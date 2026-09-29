using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;

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
        var pendingFlows = projectGuids.Count == 0
            ? new List<ApprovalFlowModel>()
            : await _db.ApprovalFlows.AsNoTracking()
                .Where(f => projectGuids.Contains(f.ProjectGuid) && f.TodoStatus == "Pending")
                .OrderByDescending(f => f.ActionTime)
                .ToListAsync(ct);

        var latestPending = pendingFlows
            .GroupBy(f => f.ProjectGuid)
            .ToDictionary(g => g.Key, g => g.First());

        return rows.Select(x =>
        {
            latestPending.TryGetValue(x.ProjectGuid, out var flow);
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
                CurrentAssigneeName = flow?.ToUserName ?? flow?.ToUserCode,
                CurrentStepCode = flow?.StepCode
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
}
