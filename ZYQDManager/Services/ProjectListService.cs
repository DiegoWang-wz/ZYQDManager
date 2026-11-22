using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

public class ProjectListService
{
    private readonly SqlServerDbContext _db;

    public ProjectListService(SqlServerDbContext db)
    {
        _db = db;
    }

    public Task<List<ProjectListItemDto>> QueryAsync(ProjectListQueryDto query, CancellationToken ct = default)
        => QueryFromDbAsync(query, ct);

    private async Task<List<ProjectListItemDto>> QueryFromDbAsync(ProjectListQueryDto query, CancellationToken ct)
    {
        var q = _db.MotorLx.AsNoTracking()
            .Where(x => x.DeleteFlag == null || x.DeleteFlag == "");

        if (query.DateStart.HasValue)
        {
            var s = query.DateStart.Value.Date;
            q = q.Where(x => (x.TimeToRequest ?? x.CreateTime) >= s);
        }

        if (query.DateEnd.HasValue)
        {
            var e = query.DateEnd.Value.Date.AddDays(1);
            q = q.Where(x => (x.TimeToRequest ?? x.CreateTime) < e);
        }

        if (!query.IncludeDraft && !query.IncludeForwarded)
            return new List<ProjectListItemDto>();

        if (query.IncludeDraft && !query.IncludeForwarded)
            q = q.Where(x => x.IsDraft);
        else if (!query.IncludeDraft && query.IncludeForwarded)
            q = q.Where(x => !x.IsDraft);

        if (!query.ViewAllCustomers)
        {
            var empNo = (query.ViewerEmpNo ?? "").Trim();
            var codes = query.AssignedCustomerCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            q = q.Where(x =>
                (x.Customer != null && codes.Contains(x.Customer))
                || (empNo.Length > 0 && x.CreatePersonCode == empNo));
        }

        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            q = query.FilterColumn switch
            {
                "DemandPerson" => q.Where(x => x.DemandPerson != null && x.DemandPerson.Contains(keyword)),
                "CustomerPartCode" => q.Where(x => x.CustomerPartCode != null && x.CustomerPartCode.Contains(keyword)),
                "MotorModelSpec" => q.Where(x => x.MotorModelSpec != null && x.MotorModelSpec.Contains(keyword)),
                "Description" => q.Where(x => x.Description != null && x.Description.Contains(keyword)),
                _ => q.Where(x => x.Customer != null && x.Customer.Contains(keyword))
            };
        }

        var rows = await q
            .OrderByDescending(x => x.UpdateTime ?? x.CreateTime)
            .ThenByDescending(x => x.Id)
            .Take(2000)
            .ToListAsync(ct);

        var projectGuids = rows.Select(x => x.ProjectGuid).Distinct().ToList();

        // 待办一律读版本表：PendingEmpNo（指定人）/ PendingRole（角色节点）+ CurrentNode。
        // 不再从 ApprovalFlow 的 Pending 流水里取 —— 那张表现在只记退回原因、审批备注这类特殊事项。
        var versions = projectGuids.Count == 0
            ? new List<ProjectVersionModel>()
            : await _db.ProjectVersions.AsNoTracking()
                .Where(v => projectGuids.Contains(v.ProjectGuid))
                .OrderBy(v => v.Id)
                .ToListAsync(ct);

        var latestVersion = versions
            .GroupBy(v => v.ProjectGuid)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(v => v.Id).First());

        // 当前待办人姓名：业务表只存工号，姓名从员工表批量取
        var assigneeNames = await LoadEmpNamesAsync(
            latestVersion.Values.Select(v => v.PendingEmpNo), ct);

        return rows.Select(x =>
        {
            latestVersion.TryGetValue(x.ProjectGuid, out var ver);
            var node = MotorNode.Normalize(ver?.CurrentNode ?? MotorNode.None);
            var archived = ver is null
                           || MotorFlow.Same(ver.Status, MotorFlow.VersionArchived)
                           || node == MotorNode.Archived;

            return new ProjectListItemDto
            {
                Id = x.Id,
                ProjectGuid = x.ProjectGuid,
                DocGuid = x.DocGuid,
                ProductType = "电机",
                VersionNo = x.VersionNo,
                IsDraft = x.IsDraft,
                Customer = x.Customer,
                DemandPerson = x.DemandPerson,
                CustomerPartCode = x.CustomerPartCode,
                ProductModel = x.MotorModelSpec,
                Description = x.Description,
                TimeToRequest = x.TimeToRequest,
                CreateTime = x.CreateTime,
                UpdateTime = x.UpdateTime,
                CurrentAssigneeName = archived
                    ? null
                    : (string.IsNullOrWhiteSpace(ver!.PendingEmpNo)
                        ? MotorFlow.RoleName(ver.PendingRole)
                        : LookupName(assigneeNames, ver.PendingEmpNo) ?? ver.PendingEmpNo),
                CurrentStepCode = archived ? null : MotorNode.Title(node)
            };
        }).ToList();
    }

    public async Task<int> SoftDeleteAsync(IEnumerable<string> docGuids, string? empNo, CancellationToken ct = default)
    {
        var set = docGuids.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (set.Count == 0) return 0;

        var rows = await _db.MotorLx.Where(x => set.Contains(x.DocGuid) && x.DeleteFlag != "X").ToListAsync(ct);
        var now = DateTime.Now;
        foreach (var row in rows)
        {
            row.DeleteFlag = "X";
            row.DeleteTime = now;
            row.DeletePersonCode = empNo;
            row.UpdateTime = now;
            row.UpdatePersonCode = empNo;
        }

        return await _db.SaveChangesAsync(ct);
    }

    /// <summary>批量取「工号 → 姓名」，业务表只存工号，姓名统一从员工表取。</summary>
    private async Task<Dictionary<string, string>> LoadEmpNamesAsync(
        IEnumerable<string?> empNos, CancellationToken ct)
    {
        var keys = empNos
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (keys.Count == 0) return dict;

        var rows = await _db.Employees.AsNoTracking()
            .Where(x => keys.Contains(x.EmpNo))
            .Select(x => new { x.EmpNo, x.EmpName })
            .ToListAsync(ct);

        foreach (var r in rows)
        {
            if (!string.IsNullOrWhiteSpace(r.EmpName))
                dict[r.EmpNo] = r.EmpName!.Trim();
        }
        return dict;
    }

    private static string? LookupName(Dictionary<string, string> names, string? empNo)
        => !string.IsNullOrWhiteSpace(empNo) && names.TryGetValue(empNo.Trim(), out var n) ? n : null;
}
