using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

public class UserCustomerAssignService
{
    private readonly SqlServerDbContext _db;

    public UserCustomerAssignService(SqlServerDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<string, List<CustomerOptionDto>>> GetAllGroupedAsync(CancellationToken ct = default)
    {
        var rows = await _db.UserCustomerAssigns.AsNoTracking()
            .OrderBy(x => x.CustomerCode)
            .ToListAsync(ct);

        var grouped = rows
            .GroupBy(x => x.EmpNo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(ToOption).ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var list in grouped.Values)
            await FillCustomerNamesAsync(list, ct);
        return grouped;
    }

    public async Task<List<CustomerOptionDto>> GetByEmpNoAsync(string empNo, CancellationToken ct = default)
    {
        empNo = (empNo ?? "").Trim();
        if (string.IsNullOrEmpty(empNo)) return [];

        var list = await _db.UserCustomerAssigns.AsNoTracking()
            .Where(x => x.EmpNo == empNo)
            .OrderBy(x => x.CustomerCode)
            .Select(x => new CustomerOptionDto
            {
                Code = x.CustomerCode,
                Name = x.CustomerName
            })
            .ToListAsync(ct);

        await FillCustomerNamesAsync(list, ct);
        return list;
    }

    public async Task<List<string>> GetCodesByEmpNoAsync(string empNo, CancellationToken ct = default)
    {
        empNo = (empNo ?? "").Trim();
        if (string.IsNullOrEmpty(empNo)) return [];

        return await _db.UserCustomerAssigns.AsNoTracking()
            .Where(x => x.EmpNo == empNo)
            .Select(x => x.CustomerCode)
            .ToListAsync(ct);
    }

    public async Task<List<CustomerOptionDto>> GetCatalogAsync(CancellationToken ct = default)
    {
        var fromLx = await _db.MotorLx.AsNoTracking()
            .Where(x => x.Customer != null && x.Customer != "")
            .Select(x => x.Customer!)
            .Distinct()
            .ToListAsync(ct);
        var fromGg = await _db.MotorGg.AsNoTracking()
            .Where(x => x.Customer != null && x.Customer != "")
            .Select(x => x.Customer!)
            .Distinct()
            .ToListAsync(ct);
        var fromCr = await _db.MotorCr.AsNoTracking()
            .Where(x => x.Customer != null && x.Customer != "")
            .Select(x => x.Customer!)
            .Distinct()
            .ToListAsync(ct);
        var assigned = await _db.UserCustomerAssigns.AsNoTracking()
            .Select(x => new { x.CustomerCode, x.CustomerName })
            .ToListAsync(ct);

        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in fromLx.Concat(fromGg).Concat(fromCr))
        {
            var c = code.Trim();
            if (c.Length == 0) continue;
            map.TryAdd(c, null);
        }

        foreach (var a in assigned)
        {
            var c = (a.CustomerCode ?? "").Trim();
            if (c.Length == 0) continue;
            if (!map.TryGetValue(c, out var name) || string.IsNullOrWhiteSpace(name))
                map[c] = string.IsNullOrWhiteSpace(a.CustomerName) ? name : a.CustomerName;
        }

        return map
            .OrderBy(x => x.Key)
            .Select(x => new CustomerOptionDto { Code = x.Key, Name = x.Value })
            .ToList();
    }

    public async Task<ApiResponse<bool>> ReplaceAsync(
        string empNo, IEnumerable<CustomerOptionDto> customers, CancellationToken ct = default)
    {
        empNo = (empNo ?? "").Trim();
        if (string.IsNullOrEmpty(empNo) || empNo.Length > 20)
            return ApiResponse<bool>.BadRequest("工号无效");

        var incoming = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in customers)
        {
            var code = (item.Code ?? "").Trim();
            if (string.IsNullOrEmpty(code) || code.Length > 50) continue;
            incoming[code] = string.IsNullOrWhiteSpace(item.Name) ? null : item.Name.Trim();
        }

        var existing = await _db.UserCustomerAssigns.Where(x => x.EmpNo == empNo).ToListAsync(ct);
        var now = DateTime.Now;

        var oldCodes = existing
            .Select(x => (x.CustomerCode ?? "").Trim())
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var addedCodes = incoming.Keys.Where(c => !oldCodes.Contains(c)).ToList();
        var removedCodes = oldCodes.Where(c => !incoming.ContainsKey(c)).ToList();

        foreach (var row in existing.Where(x => !incoming.ContainsKey(x.CustomerCode)).ToList())
            _db.UserCustomerAssigns.Remove(row);

        foreach (var (code, name) in incoming)
        {
            var row = existing.FirstOrDefault(x =>
                string.Equals(x.CustomerCode, code, StringComparison.OrdinalIgnoreCase));
            if (row is null)
            {
                _db.UserCustomerAssigns.Add(new ZyqdUserCustomerAssignModel
                {
                    EmpNo = empNo,
                    CustomerCode = code,
                    CustomerName = name,
                    CreateTime = now
                });
            }
            else if (!string.IsNullOrWhiteSpace(name) && row.CustomerName != name)
            {
                row.CustomerName = name;
            }
        }

        await SyncCotractBaseManagersAsync(empNo, addedCodes, removedCodes, ct);
        await _db.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "客户分配已保存");
    }

    /// <summary>
    /// 客户分配变更时，同步合同基础数据负责人：新增客户追加工号，取消客户删除工号。
    /// </summary>
    private async Task SyncCotractBaseManagersAsync(
        string empNo,
        IReadOnlyList<string> addedClientNos,
        IReadOnlyList<string> removedClientNos,
        CancellationToken ct)
    {
        if (addedClientNos.Count == 0 && removedClientNos.Count == 0)
            return;

        var touchCodes = addedClientNos.Concat(removedClientNos)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = await _db.CotractBases
            .Where(x => x.ClientNo != null
                        && touchCodes.Contains(x.ClientNo)
                        && (x.deleteSigh == null || x.deleteSigh != "X"))
            .ToListAsync(ct);

        var addSet = addedClientNos.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removeSet = removedClientNos.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var client = (row.ClientNo ?? "").Trim();
            if (addSet.Contains(client))
                row.Customer_Manager = CustomerManagerText.AddEmpNo(row.Customer_Manager, empNo);
            if (removeSet.Contains(client))
                row.Customer_Manager = CustomerManagerText.RemoveEmpNo(row.Customer_Manager, empNo);
            row.UpdateTime = DateTime.Now;
        }
    }

    private static CustomerOptionDto ToOption(ZyqdUserCustomerAssignModel x) => new()
    {
        Code = x.CustomerCode,
        Name = x.CustomerName
    };

    /// <summary>分配表缺名称时，从合同基础/PI 按客户编号补全。</summary>
    private async Task FillCustomerNamesAsync(List<CustomerOptionDto> list, CancellationToken ct)
    {
        var missing = list
            .Where(x => !string.IsNullOrWhiteSpace(x.Code) && string.IsNullOrWhiteSpace(x.Name))
            .Select(x => x.Code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missing.Count == 0) return;

        var fromBase = await _db.CotractBases.AsNoTracking()
            .Where(x => x.ClientNo != null
                        && x.ClientName != null
                        && x.ClientName != ""
                        && missing.Contains(x.ClientNo))
            .Select(x => new { x.ClientNo, x.ClientName })
            .ToListAsync(ct);

        var fromPi = await _db.CotractPis.AsNoTracking()
            .Where(x => x.ClientNo != null
                        && x.ClientName != null
                        && x.ClientName != ""
                        && missing.Contains(x.ClientNo))
            .Select(x => new { x.ClientNo, x.ClientName })
            .ToListAsync(ct);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in fromBase.Concat(fromPi))
        {
            var code = (row.ClientNo ?? "").Trim();
            var name = (row.ClientName ?? "").Trim();
            if (code.Length == 0 || name.Length == 0) continue;
            map.TryAdd(code, name);
        }

        foreach (var item in list)
        {
            if (!string.IsNullOrWhiteSpace(item.Name)) continue;
            var code = (item.Code ?? "").Trim();
            if (code.Length > 0 && map.TryGetValue(code, out var n))
                item.Name = n;
        }
    }
}
