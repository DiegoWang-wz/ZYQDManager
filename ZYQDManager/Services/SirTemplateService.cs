using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;

namespace ZYQDManager.Services;

/// <summary>检测项模板维护：按报告类型 + 产品类型增删改，并让已填实测跟着编号走。</summary>
public class SirTemplateService
{
    private static readonly Regex MotorCode = new(@"^M\.\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly SqlServerDbContext _db;

    public SirTemplateService(SqlServerDbContext db)
    {
        _db = db;
    }

    public async Task<List<string>> ProductTypesAsync(string? reportType, CancellationToken ct = default)
    {
        reportType = (reportType ?? "").Trim();
        var known = SirReportTypes.ProductsFor(reportType).ToList();
        if (reportType.Length == 0)
            return known;

        var fromDb = await _db.SirItemTemplates.AsNoTracking()
            .Where(x => x.Report_type == reportType && x.product_type != "")
            .Select(x => x.product_type)
            .Distinct()
            .ToListAsync(ct);

        foreach (var name in fromDb)
        {
            if (known.Any(x => string.Equals(x, name, StringComparison.Ordinal)))
                continue;
            known.Add(name);
        }

        return known;
    }

    public async Task<List<ZyqdSirItemTemplateModel>> ListAsync(
        string? reportType, string? productType, CancellationToken ct = default)
    {
        reportType = (reportType ?? "").Trim();
        productType = (productType ?? "").Trim();
        return await _db.SirItemTemplates.AsNoTracking()
            .Where(x => x.Report_type == reportType && x.product_type == productType)
            .OrderBy(x => x.SortNo)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public static bool UsesSequentialCodes(string? reportType, IEnumerable<ZyqdSirItemTemplateModel> rows)
    {
        var list = rows.ToList();
        if (list.Count == 0)
        {
            var type = (reportType ?? "").Trim();
            return type is "motor" or "remote_new" or "putter";
        }

        return list.All(x => MotorCode.IsMatch((x.Specification_type ?? "").Trim()));
    }

    public async Task<(bool Ok, string? Message)> SaveAsync(
        string? reportType,
        string? productType,
        IReadOnlyList<ZyqdSirItemTemplateModel> items,
        CancellationToken ct = default)
    {
        reportType = (reportType ?? "").Trim();
        productType = (productType ?? "").Trim();
        if (reportType.Length == 0 || productType.Length == 0)
            return (false, "请选择测试类型和产品类型");

        var rows = items.ToList();
        for (var i = 0; i < rows.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(rows[i].Specification))
                return (false, $"第 {i + 1} 行请填写检测项中文名称");
        }

        var existing = await _db.SirItemTemplates
            .Where(x => x.Report_type == reportType && x.product_type == productType)
            .ToListAsync(ct);
        var existingById = existing.ToDictionary(x => x.Id);

        if (rows.Any(x => x.Id > 0 && !existingById.ContainsKey(x.Id)))
            return (false, "检测项已变化，请刷新后再保存");

        var sequential = UsesSequentialCodes(reportType, existing);
        if (sequential)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Specification_type = $"M.{i + 1}";
                rows[i].SortNo = (i + 1) * 10;
            }
        }
        else
        {
            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Specification_type = (rows[i].Specification_type ?? "").Trim();
                rows[i].SortNo = (i + 1) * 10;
                if (rows[i].Specification_type.Length == 0)
                    return (false, $"第 {i + 1} 行请填写检测项编号");
            }
        }

        var codes = rows.Select(x => x.Specification_type.Trim()).ToList();
        if (codes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codes.Count)
            return (false, "检测项编号重复");

        var incomingIds = rows.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
        var removed = existing.Where(x => !incomingIds.Contains(x.Id)).ToList();
        var renames = new List<(string OldCode, string TempCode, string NewCode)>();
        foreach (var row in rows.Where(x => x.Id > 0))
        {
            var oldCode = (existingById[row.Id].Specification_type ?? "").Trim();
            var newCode = row.Specification_type.Trim();
            if (string.Equals(oldCode, newCode, StringComparison.OrdinalIgnoreCase))
                continue;
            renames.Add((oldCode, $"TMP#{row.Id}", newCode));
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var row in removed)
        {
            var code = (row.Specification_type ?? "").Trim();
            if (code.Length > 0)
                await RetireDetailsAsync(reportType, productType, code, ct);
        }

        if (removed.Count > 0)
            _db.SirItemTemplates.RemoveRange(removed);

        foreach (var rename in renames)
            await RenameDetailsAsync(reportType, productType, rename.OldCode, rename.TempCode, ct);
        foreach (var rename in renames)
            await RenameDetailsAsync(reportType, productType, rename.TempCode, rename.NewCode, ct);

        foreach (var row in rows)
        {
            var filling = string.IsNullOrWhiteSpace(row.filling_type) ? "0" : row.filling_type.Trim();
            if (row.Id <= 0)
            {
                _db.SirItemTemplates.Add(new ZyqdSirItemTemplateModel
                {
                    Report_type = reportType,
                    product_type = productType,
                    Specification_type = row.Specification_type.Trim(),
                    SortNo = row.SortNo,
                    GroupTitle = BlankToNull(row.GroupTitle),
                    Specification = row.Specification?.Trim(),
                    Specification_EN = BlankToNull(row.Specification_EN),
                    Criteria = BlankToNull(row.Criteria),
                    Criteria_EN = BlankToNull(row.Criteria_EN),
                    filling_type = filling,
                    Enabled = row.Enabled,
                    createTime = DateTime.Now,
                });
                continue;
            }

            var entity = existingById[row.Id];
            entity.Specification_type = row.Specification_type.Trim();
            entity.SortNo = row.SortNo;
            entity.GroupTitle = BlankToNull(row.GroupTitle);
            entity.Specification = row.Specification?.Trim();
            entity.Specification_EN = BlankToNull(row.Specification_EN);
            entity.Criteria = BlankToNull(row.Criteria);
            entity.Criteria_EN = BlankToNull(row.Criteria_EN);
            entity.filling_type = filling;
            entity.Enabled = row.Enabled;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (true, null);
    }

    private async Task RenameDetailsAsync(
        string reportType, string productType, string fromCode, string toCode, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE d
            SET d.Specification_type = {toCode}
            FROM dbo.T_ZYQD_SIR_detail d
            INNER JOIN dbo.T_ZYQD_SIR_main m ON m.guid = d.main_guid
            WHERE m.Report_type = {reportType}
              AND m.product_type = {productType}
              AND ISNULL(d.deleteSigh, N'') <> N'X'
              AND d.Specification_type = {fromCode}
            """, ct);
    }

    private async Task RetireDetailsAsync(
        string reportType, string productType, string code, CancellationToken ct)
    {
        var now = DateTime.Now;
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE d
            SET d.deleteSigh = N'X',
                d.deleteTime = {now},
                d.deletePerson = N'template'
            FROM dbo.T_ZYQD_SIR_detail d
            INNER JOIN dbo.T_ZYQD_SIR_main m ON m.guid = d.main_guid
            WHERE m.Report_type = {reportType}
              AND m.product_type = {productType}
              AND ISNULL(d.deleteSigh, N'') <> N'X'
              AND d.Specification_type = {code}
            """, ct);
    }

    private static string? BlankToNull(string? value)
    {
        value = (value ?? "").Trim();
        return value.Length == 0 ? null : value;
    }
}
