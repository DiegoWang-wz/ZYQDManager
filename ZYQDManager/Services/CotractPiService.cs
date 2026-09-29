using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;

namespace ZYQDManager.Services;

public class CotractPiService
{
    private readonly SqlServerDbContext _db;

    public CotractPiService(SqlServerDbContext db)
    {
        _db = db;
    }

    public async Task<(List<ZyqdCotractPiModel> Rows, int Total)> QueryPageAsync(
        CotractPiQueryDto query, int skip, int take, CancellationToken ct = default)
    {
        if (query.RequireClientNo && string.IsNullOrWhiteSpace(query.ClientNo))
            return ([], 0);

        var q = ApplyFilter(_db.CotractPis.AsNoTracking(), query);
        var idQuery = q.GroupBy(x => x.DocumentGuid).Select(g => g.Min(x => x.Id));
        var docs = _db.CotractPis.AsNoTracking().Where(x => idQuery.Contains(x.Id));
        docs = ApplySort(docs, query);

        var total = await docs.CountAsync(ct);
        if (take <= 0) take = 20;
        if (skip < 0) skip = 0;

        var rows = await docs
            .Skip(skip)
            .Take(take)
            .Select(x => new ZyqdCotractPiModel
            {
                Id = x.Id,
                DocumentGuid = x.DocumentGuid,
                ClientNo = x.ClientNo,
                ClientName = x.ClientName,
                var6 = x.var6,
                Person = x.Person,
                INVOICE_NO = x.INVOICE_NO,
                BILL_TO_DATE = x.BILL_TO_DATE,
                CreateTime = x.CreateTime,
                UpdateTime = x.UpdateTime,
                BILL_TO_PO_REF = x.BILL_TO_PO_REF
            })
            .ToListAsync(ct);

        return (rows, total);
    }

    public async Task<(bool Ok, string? Message)> DeleteByGuidAsync(string? guid, CancellationToken ct = default)
    {
        guid = (guid ?? "").Trim();
        if (guid.Length == 0)
            return (false, "单据编号为空");

        var rows = await _db.CotractPis.Where(x => x.DocumentGuid == guid).ToListAsync(ct);
        if (rows.Count == 0)
            return (false, "记录不存在");

        _db.CotractPis.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<List<ZyqdCotractPiModel>> GetByGuidAsync(string? guid, CancellationToken ct = default)
    {
        guid = (guid ?? "").Trim();
        if (guid.Length == 0)
            return [];

        return await _db.CotractPis.AsNoTracking()
            .Where(x => x.DocumentGuid == guid)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<List<ZyqdCotractPiModel>> GetByGuidsAsync(string? guids, CancellationToken ct = default)
    {
        var ids = SplitGuids(guids);
        if (ids.Count == 0)
            return [];

        var rows = await _db.CotractPis.AsNoTracking()
            .Where(x => x.DocumentGuid != null && ids.Contains(x.DocumentGuid))
            .ToListAsync(ct);

        var map = rows
            .GroupBy(x => x.DocumentGuid ?? "", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList(), StringComparer.OrdinalIgnoreCase);

        var list = new List<ZyqdCotractPiModel>();
        foreach (var id in ids)
        {
            if (map.TryGetValue(id, out var group))
                list.AddRange(group);
        }

        return list
            .OrderBy(x => x.INVOICE_NO)
            .ThenBy(x => x.materiaCust)
            .ThenBy(x => x.Id)
            .ToList();
    }

    public async Task<List<ZyqdCotractPiModel>> LoadCustomerCiDraftAsync(
        string? sourceGuids, string? orderType, CancellationToken ct = default)
    {
        var rows = await GetByGuidsAsync(sourceGuids, ct);
        if (rows.Count == 0)
            return [];

        var today = CotractPiTypes.FormatDate(DateTime.Today);
        var ciType = CotractPiTypes.ToCustomerCiType(
            string.IsNullOrWhiteSpace(orderType) ? rows[0].var6 : orderType);

        var unique = rows
            .GroupBy(x => x.DocumentGuid ?? "", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        var ship = unique.Sum(ParseFee);
        var insure = unique.Sum(x => x.Insurance_premium);
        var ratioExt = unique.Sum(x => x.Payment_ratio_Ext);
        var balance = unique.Sum(x => x.balance_before_delivery);

        foreach (var row in rows)
        {
            row.Id = 0;
            row.DocumentGuid = null;
            row.var6 = ciType;
            row.var11 = "客户CI";
            if (string.IsNullOrWhiteSpace(row.var13))
                row.var13 = row.INVOICE_NO;
            row.BILL_TO_DATE = today;
            row.var5 = today;
            row.var10 = ship.ToString("0.##");
            row.Insurance_premium = insure;
            row.Payment_ratio_Ext = ratioExt;
            row.balance_before_delivery = balance;
            row.deleteSigh = "N";
        }

        return rows;
    }

    private static double ParseFee(ZyqdCotractPiModel x)
        => double.TryParse(x.var10, out var n) ? n : 0;

    private static List<string> SplitGuids(string? raw)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
            return list;
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!list.Contains(part, StringComparer.OrdinalIgnoreCase))
                list.Add(part);
        }
        return list;
    }

    public bool CanAccess(ZyqdCotractPiModel row, CotractPiQueryDto query)
    {
        if (query.ViewAll) return true;
        var empNo = (query.ViewerEmpNo ?? "").Trim();
        return empNo.Length > 0
               && !string.IsNullOrWhiteSpace(row.Person)
               && string.Equals(row.Person.Trim(), empNo, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> BuildInvoiceNoAsync(
        string? clientNo, string? clientNameOld, string? orderType, CancellationToken ct = default)
    {
        clientNo = CotractPiTypes.NormalizeClientNo(clientNo);
        var name = (clientNameOld ?? "").Trim();
        var template = CotractPiTypes.ResolveTemplateType(orderType, clientNo);
        var prefix = CotractPiTypes.InvoicePrefix(template);
        var var12 = CotractPiTypes.ResolveVar12(template);
        var var11 = CotractPiTypes.DocumentKind(orderType);
        var today = DateTime.Today;

        var count = await _db.CotractPis.AsNoTracking()
            .Where(x => x.deleteSigh != "X"
                        && x.ClientNo == clientNo
                        && x.var12 == var12
                        && x.var11 == var11
                        && x.UpdateTime != null
                        && x.UpdateTime.Value.Date == today)
            .Select(x => x.DocumentGuid)
            .Distinct()
            .CountAsync(ct);

        var year = today.ToString("yy");
        var md = today.ToString("MMdd");
        var hz = count == 0 ? "" : count == 1 ? "-1" : "-" + count;
        return $"{year}{prefix}{md}-{name}{hz}";
    }

    public async Task<(bool Ok, string? Message, string? Guid)> SaveAsync(
        ZyqdCotractPiModel header,
        List<CotractPiLineDto> lines,
        string? empNo,
        bool isCreate,
        CancellationToken ct = default)
    {
        empNo = (empNo ?? "").Trim();
        if (empNo.Length == 0)
            return (false, "无法获取当前登录工号，请重新登录后再保存", null);

        var err = Validate(header, lines);
        if (err is not null)
            return (false, err, header.DocumentGuid);

        header.ClientNo = CotractPiTypes.NormalizeClientNo(header.ClientNo);
        header.Cotract_PI_type = CotractPiTypes.ResolveTemplateType(header.var6, header.ClientNo);
        header.var11 = CotractPiTypes.DocumentKind(header.var6);
        header.var12 = CotractPiTypes.ResolveVar12(header.Cotract_PI_type);
        if (header.var11 == "PI")
            header.var13 = header.INVOICE_NO;
        header.percent = header.percent == 0 ? 1 : header.percent;
        header.deleteSigh = "N";
        header.BILL_TO_DATE = CotractPiTypes.FormatDate(header.BILL_TO_DATE);
        header.var2 = CotractPiTypes.FormatDate(header.var2);
        header.var5 = CotractPiTypes.FormatDate(header.var5);
        if (!string.IsNullOrWhiteSpace(header.SignDate))
            header.SignDate = CotractPiTypes.FormatDate(header.SignDate);

        var now = DateTime.Now;
        var guid = (header.DocumentGuid ?? "").Trim();
        List<ZyqdCotractPiModel> old = [];
        if (!isCreate && guid.Length > 0)
            old = await _db.CotractPis.Where(x => x.DocumentGuid == guid).ToListAsync(ct);

        string? createPerson;
        DateTime? createTime;
        string person;
        if (old.Count > 0)
        {
            createPerson = old[0].CreatePerson;
            createTime = old[0].CreateTime;
            person = string.IsNullOrWhiteSpace(old[0].Person) ? empNo : old[0].Person!.Trim();
        }
        else
        {
            guid = Guid.NewGuid().ToString("N");
            createPerson = empNo;
            createTime = now;
            person = string.IsNullOrWhiteSpace(header.Person) ? empNo : header.Person.Trim();
        }

        var currency = lines.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.currency_unit))?.currency_unit
                       ?? header.currency_unit;

        var rows = new List<ZyqdCotractPiModel>(lines.Count);
        foreach (var line in lines)
        {
            var origin = line.origin_price != 0 ? line.origin_price : line.price;
            rows.Add(new ZyqdCotractPiModel
            {
                DocumentGuid = guid,
                Status = header.Status,
                Person = person,
                Cotract_PI_type = header.Cotract_PI_type,
                BILL_TO_info1 = header.BILL_TO_info1,
                BILL_TO_info2 = header.BILL_TO_info2,
                BILL_TO_info3 = header.BILL_TO_info3,
                BILL_TO_info4 = header.BILL_TO_info4,
                SHIP_TO_info1 = header.SHIP_TO_info1,
                SHIP_TO_info2 = header.SHIP_TO_info2,
                SHIP_TO_info3 = header.SHIP_TO_info3,
                SHIP_TO_info4 = header.SHIP_TO_info4,
                materialNo = line.materialNo,
                materialName = line.materialName,
                ClientNo = header.ClientNo,
                ClientName = header.ClientName,
                INVOICE_NO = header.INVOICE_NO,
                BILL_TO_DATE = header.BILL_TO_DATE,
                BILL_TO_PO_REF = header.BILL_TO_PO_REF,
                BILL_TO_FOB = header.BILL_TO_FOB,
                materialType = header.materialType,
                materialModel = line.materialModel,
                materiaSpecs = line.materiaSpecs,
                materiaPowered = line.materiaPowered,
                materiaCust = line.materiaCust,
                qty = line.qty,
                price = origin,
                ext = line.ext,
                TOTAL = header.TOTAL,
                currency_unit = line.currency_unit ?? currency,
                Payment_ratio = header.Payment_ratio,
                Payment_ratio_Ext = header.Payment_ratio_Ext,
                balance_before_delivery = header.balance_before_delivery,
                payment_method = header.payment_method,
                email = header.email,
                SignDate = header.SignDate,
                Notes = line.Notes,
                Estimated_TimeofCompletion = header.Estimated_TimeofCompletion,
                var1 = header.var1,
                var2 = header.var2,
                var3 = header.var3,
                var4 = header.var4,
                var5 = header.var5,
                var6 = header.var6,
                var7 = line.var7,
                var8 = line.var8,
                var9 = origin.ToString("0.####"),
                var10 = header.var10,
                var11 = header.var11,
                var12 = header.var12,
                var13 = header.var13,
                ClientName_old = header.ClientName_old,
                percent = header.percent,
                Insurance_premium = header.Insurance_premium,
                Shipping_Fee = header.Shipping_Fee,
                CreateTime = createTime,
                UpdateTime = now,
                CreatePerson = createPerson,
                UpdatePerson = empNo,
                deleteSigh = "N"
            });
        }

        if (old.Count > 0)
            _db.CotractPis.RemoveRange(old);
        _db.CotractPis.AddRange(rows);
        await _db.SaveChangesAsync(ct);
        return (true, null, guid);
    }

    private static string? Validate(ZyqdCotractPiModel header, List<CotractPiLineDto> lines)
    {
        if (string.IsNullOrWhiteSpace(header.email))
            return "邮箱为必填项";
        if (string.IsNullOrWhiteSpace(header.BILL_TO_FOB))
            return "贸易方式为必填项";
        if (string.IsNullOrWhiteSpace(header.var3))
            return "付款条件为必填项";
        if (string.IsNullOrWhiteSpace(header.ClientNo))
            return "请填写付款客户编号";
        if (string.IsNullOrWhiteSpace(header.var6))
            return "请选择订单类型";
        if (lines.Count == 0)
            return "请至少添加一行产品";
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].qty <= 0)
                return $"第{i + 1}行数量不能小于等于0";
        }
        return null;
    }

    private static IQueryable<ZyqdCotractPiModel> ApplyFilter(IQueryable<ZyqdCotractPiModel> q, CotractPiQueryDto query)
    {
        q = q.Where(x => x.deleteSigh != "X"
                         && x.DocumentGuid != null
                         && x.DocumentGuid != "");

        var types = OrderTypes(query.DocumentType);
        if (types.Count > 0)
            q = q.Where(x => x.var6 != null && types.Contains(x.var6));

        if (!query.ViewAll)
        {
            var empNo = (query.ViewerEmpNo ?? "").Trim();
            if (empNo.Length == 0)
                return q.Where(x => false);
            q = q.Where(x => x.Person != null && x.Person == empNo);
        }

        var clientNo = CotractPiTypes.NormalizeClientNo(query.ClientNo);
        if (clientNo.Length > 0)
            q = q.Where(x => x.ClientNo != null && (x.ClientNo == clientNo || x.ClientNo.EndsWith(clientNo)));
        else if (query.RequireClientNo)
            return q.Where(x => false);

        if (query.DateStart is { } from)
        {
            var start = from.Date;
            q = q.Where(x => x.CreateTime != null && x.CreateTime >= start);
        }

        if (query.DateEnd is { } to)
        {
            var end = to.Date.AddDays(1);
            q = q.Where(x => x.CreateTime != null && x.CreateTime < end);
        }

        var keyword = (query.Keyword ?? "").Trim();
        if (keyword.Length == 0)
            return q;

        return query.FilterColumn switch
        {
            "INVOICE_NO" => q.Where(x => x.INVOICE_NO != null && x.INVOICE_NO.Contains(keyword)),
            "Person" => q.Where(x => x.Person != null && x.Person.Contains(keyword)),
            "var6" => q.Where(x => x.var6 != null && x.var6.Contains(keyword)),
            "BILL_TO_PO_REF" => q.Where(x => x.BILL_TO_PO_REF != null && x.BILL_TO_PO_REF.Contains(keyword)),
            _ => q.Where(x =>
                (x.ClientName != null && x.ClientName.Contains(keyword))
                || (x.ClientNo != null && (x.ClientNo.Contains(keyword) || x.ClientNo.EndsWith(keyword))))
        };
    }

    private static IQueryable<ZyqdCotractPiModel> ApplySort(IQueryable<ZyqdCotractPiModel> q, CotractPiQueryDto query)
    {
        var desc = query.SortDescending;
        return query.SortLabel switch
        {
            "ClientNo" => desc ? q.OrderByDescending(x => x.ClientNo) : q.OrderBy(x => x.ClientNo),
            "ClientName" => desc ? q.OrderByDescending(x => x.ClientName) : q.OrderBy(x => x.ClientName),
            "var6" => desc ? q.OrderByDescending(x => x.var6) : q.OrderBy(x => x.var6),
            "Person" => desc ? q.OrderByDescending(x => x.Person) : q.OrderBy(x => x.Person),
            "INVOICE_NO" => desc ? q.OrderByDescending(x => x.INVOICE_NO) : q.OrderBy(x => x.INVOICE_NO),
            "BILL_TO_DATE" => desc ? q.OrderByDescending(x => x.BILL_TO_DATE) : q.OrderBy(x => x.BILL_TO_DATE),
            "CreateTime" => desc ? q.OrderByDescending(x => x.CreateTime) : q.OrderBy(x => x.CreateTime),
            "BILL_TO_PO_REF" => desc ? q.OrderByDescending(x => x.BILL_TO_PO_REF) : q.OrderBy(x => x.BILL_TO_PO_REF),
            _ => desc ? q.OrderByDescending(x => x.UpdateTime) : q.OrderBy(x => x.UpdateTime)
        };
    }

    private static IReadOnlyList<string> OrderTypes(string? documentType)
    {
        var t = (documentType ?? "").Trim();
        if (t.Length == 0 || t == "客户PI")
            return CotractPiTypes.CustomerPi;
        if (t == "客户CI")
            return CotractPiTypes.CustomerCi;
        if (CotractPiTypes.CustomerPi.Contains(t))
            return [t];
        if (CotractPiTypes.CustomerCi.Contains(t))
            return [t];
        return CotractPiTypes.CustomerPi;
    }
}
