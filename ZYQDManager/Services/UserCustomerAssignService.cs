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

        return rows
            .GroupBy(x => x.EmpNo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(ToOption).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<List<CustomerOptionDto>> GetByEmpNoAsync(string empNo, CancellationToken ct = default)
    {
        empNo = (empNo ?? "").Trim();
        if (string.IsNullOrEmpty(empNo)) return [];

        return await _db.UserCustomerAssigns.AsNoTracking()
            .Where(x => x.EmpNo == empNo)
            .OrderBy(x => x.CustomerCode)
            .Select(x => new CustomerOptionDto
            {
                Code = x.CustomerCode,
                Name = x.CustomerName
            })
            .ToListAsync(ct);
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

        await _db.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "客户分配已保存");
    }

    private static CustomerOptionDto ToOption(ZyqdUserCustomerAssignModel x) => new()
    {
        Code = x.CustomerCode,
        Name = x.CustomerName
    };
}
