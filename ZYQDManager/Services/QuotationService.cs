using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.DataModels.Quotation;

namespace ZYQDManager.Services;

public class QuotationService
{
    private readonly SqlServerDbContext _db;
    private readonly QuotationBaseService _base;

    public QuotationService(SqlServerDbContext db, QuotationBaseService quotationBase)
    {
        _db = db;
        _base = quotationBase;
    }

    public async Task<(List<QuotationListItemDto> Rows, int Total)> QueryPageAsync(
        QuotationQueryDto query, int skip, int take, CancellationToken ct = default)
    {
        if (take <= 0) take = 20;
        if (skip < 0) skip = 0;

        IQueryable<ZyqdQuotationMainModel> q = _db.QuotationMains.AsNoTracking();

        var kw = (query.Keyword ?? "").Trim();
        if (kw.Length > 0)
        {
            var col = (query.FilterColumn ?? "Quota_No").Trim();
            q = col switch
            {
                "ClientCode" => q.Where(x => x.ClientCode != null && x.ClientCode.Contains(kw)),
                "ClientName" => q.Where(x => x.ClientName != null && x.ClientName.Contains(kw)),
                "Attn" => q.Where(x => x.Attn != null && x.Attn.Contains(kw)),
                "personId" => q.Where(x => x.personId != null && x.personId.Contains(kw)),
                _ => q.Where(x => x.Quota_No != null && x.Quota_No.Contains(kw)),
            };
        }

        if (query.DateStart is { } ds)
            q = q.Where(x => x.createTime == null || x.createTime >= ds.Date);
        if (query.DateEnd is { } de)
        {
            var end = de.Date.AddDays(1);
            q = q.Where(x => x.createTime == null || x.createTime < end);
        }

        var label = (query.SortLabel ?? "createTime").Trim();
        var desc = query.SortDescending;
        q = label switch
        {
            "Quota_No" => desc ? q.OrderByDescending(x => x.Quota_No) : q.OrderBy(x => x.Quota_No),
            "ClientCode" => desc ? q.OrderByDescending(x => x.ClientCode) : q.OrderBy(x => x.ClientCode),
            "ClientName" => desc ? q.OrderByDescending(x => x.ClientName) : q.OrderBy(x => x.ClientName),
            "DATE" => desc ? q.OrderByDescending(x => x.DATE) : q.OrderBy(x => x.DATE),
            "personId" => desc ? q.OrderByDescending(x => x.personId) : q.OrderBy(x => x.personId),
            _ => desc ? q.OrderByDescending(x => x.createTime) : q.OrderBy(x => x.createTime),
        };

        var total = await q.CountAsync(ct);
        var rows = await q.Skip(skip).Take(take)
            .Select(x => new QuotationListItemDto
            {
                Id = x.Id,
                Guid = x.guid,
                Quota_No = x.Quota_No,
                ClientCode = x.ClientCode,
                ClientName = x.ClientName,
                DATE = x.DATE,
                personId = x.personId,
                createTime = x.createTime,
                currencyUnit = x.currencyUnit,
                signature = x.signature,
            })
            .ToListAsync(ct);

        return (rows, total);
    }

    public async Task<QuotationDocumentDto?> GetAsync(string? guid, CancellationToken ct = default)
    {
        guid = (guid ?? "").Trim();
        if (guid.Length == 0) return null;

        var main = await _db.QuotationMains.AsNoTracking()
            .FirstOrDefaultAsync(x => x.guid == guid, ct);
        if (main is null) return null;

        var details = await _db.QuotationDetails.AsNoTracking()
            .Where(x => x.main_guid == guid)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);

        var doc = new QuotationDocumentDto { Main = main };
        foreach (var d in details)
        {
            var line = MapDetail(d);
            if (d.BaseId is > 0)
            {
                var b = await _base.GetAsync(d.ItemType, d.BaseId.Value, ct);
                if (b is not null)
                    line.ApplyBase(b);
            }

            doc.Details.Add(line);
        }

        return doc;
    }

    public QuotationDocumentDto CreateBlank(string? empNo)
    {
        return new QuotationDocumentDto
        {
            Main = new ZyqdQuotationMainModel
            {
                guid = Guid.NewGuid().ToString("N"),
                DATE = DateTime.Now.ToString("yyyy-MM-dd"),
                personId = (empNo ?? "").Trim(),
                currencyUnit = "$",
                rate = "",
                createTime = DateTime.Now,
            },
            Details = [],
        };
    }

    public async Task<(bool Ok, string? Message, string Guid)> SaveAsync(
        QuotationDocumentDto doc,
        string? empNo,
        bool isCreate,
        CancellationToken ct = default)
    {
        if (doc.Main is null)
            return (false, "抬头为空", "");

        var main = doc.Main;
        var guid = (main.guid ?? "").Trim();
        if (guid.Length == 0)
            guid = Guid.NewGuid().ToString("N");
        main.guid = guid;

        var quotaNo = (main.Quota_No ?? "").Trim();
        if (quotaNo.Length == 0)
            return (false, "请填写报价单号 Quota_No", "");
        main.Quota_No = quotaNo;

        empNo = (empNo ?? "").Trim();
        if (empNo.Length > 0)
            main.personId = empNo;

        if (string.IsNullOrWhiteSpace(main.currencyUnit))
            main.currencyUnit = "$";

        foreach (var line in doc.Details)
        {
            line.ItemType = QuotationBaseTypes.Normalize(line.ItemType);
            line.RecalcAmount();
        }

        var validLines = doc.Details
            .Where(x => !string.IsNullOrWhiteSpace(x.Part_No) || x.BaseId is > 0)
            .ToList();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            ZyqdQuotationMainModel row;
            if (isCreate)
            {
                if (await _db.QuotationMains.AnyAsync(x => x.guid == guid, ct))
                    return (false, "单据编号冲突，请刷新后重试", "");

                row = new ZyqdQuotationMainModel
                {
                    guid = guid,
                    createTime = DateTime.Now,
                };
                _db.QuotationMains.Add(row);
            }
            else
            {
                row = await _db.QuotationMains.FirstOrDefaultAsync(x => x.guid == guid, ct)
                      ?? throw new InvalidOperationException("报价单不存在");
            }

            CopyMain(main, row);
            if (isCreate && row.createTime is null)
                row.createTime = DateTime.Now;

            var old = await _db.QuotationDetails.Where(x => x.main_guid == guid).ToListAsync(ct);
            _db.QuotationDetails.RemoveRange(old);

            var order = 0;
            foreach (var line in validLines)
            {
                order++;
                _db.QuotationDetails.Add(new ZyqdQuotationDetailModel
                {
                    main_guid = guid,
                    ItemType = QuotationBaseTypes.Normalize(line.ItemType),
                    BaseId = line.BaseId,
                    Part_No = (line.Part_No ?? "").Trim(),
                    Quotation_logo = line.Quotation_logo,
                    Qty = line.Qty,
                    Price = line.Price,
                    Discount = line.Discount ?? 0m,
                    Amount = line.Amount,
                    Note = line.Note,
                    SortOrder = order,
                    createTime = DateTime.Now,
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (true, null, guid);
        }
        catch (Exception)
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<(bool Ok, string? Message)> DeleteAsync(string? guid, CancellationToken ct = default)
    {
        guid = (guid ?? "").Trim();
        if (guid.Length == 0)
            return (false, "参数无效");

        var main = await _db.QuotationMains.FirstOrDefaultAsync(x => x.guid == guid, ct);
        if (main is null)
            return (false, "记录不存在");

        var details = await _db.QuotationDetails.Where(x => x.main_guid == guid).ToListAsync(ct);
        _db.QuotationDetails.RemoveRange(details);
        _db.QuotationMains.Remove(main);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Message)> DeleteManyAsync(
        IEnumerable<string> guids, CancellationToken ct = default)
    {
        var list = guids.Select(x => (x ?? "").Trim()).Where(x => x.Length > 0).Distinct().ToList();
        if (list.Count == 0)
            return (false, "未选择记录");

        var fail = 0;
        foreach (var g in list)
        {
            var (ok, _) = await DeleteAsync(g, ct);
            if (!ok) fail++;
        }

        return fail == 0 ? (true, null) : (false, $"有 {fail} 条删除失败");
    }

    private static void CopyMain(ZyqdQuotationMainModel src, ZyqdQuotationMainModel dst)
    {
        dst.ClientCode = src.ClientCode;
        dst.ClientName = src.ClientName;
        dst.Address = src.Address;
        dst.Tel = src.Tel;
        dst.Mail = src.Mail;
        dst.Attn = src.Attn;
        dst.Delivery_Term = src.Delivery_Term;
        dst.Payment_Term = src.Payment_Term;
        dst.Quotation_validity = src.Quotation_validity;
        dst.Note = src.Note;
        dst.signature = src.signature;
        dst.signature_url = src.signature_url;
        dst.Quota_No = src.Quota_No;
        dst.DATE = src.DATE;
        dst.personId = src.personId;
        dst.rate = src.rate;
        dst.currencyUnit = src.currencyUnit;
    }

    private static QuotationDetailLineDto MapDetail(ZyqdQuotationDetailModel d) => new()
    {
        Id = d.Id,
        ItemType = QuotationBaseTypes.Normalize(d.ItemType),
        BaseId = d.BaseId,
        Part_No = d.Part_No,
        Quotation_logo = d.Quotation_logo,
        Qty = d.Qty,
        Price = d.Price,
        Discount = d.Discount,
        Amount = d.Amount,
        Note = d.Note,
        SortOrder = d.SortOrder,
    };
}
