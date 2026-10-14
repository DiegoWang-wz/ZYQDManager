using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;

namespace ZYQDManager.Services;

public class CustomsAndPackingService
{
    private readonly SqlServerDbContext _db;

    public CustomsAndPackingService(SqlServerDbContext db)
    {
        _db = db;
    }

    public async Task<(List<ZyqdCustomsAndPackingModel> Rows, int Total)> QueryPageAsync(
        CustomsAndPackingQueryDto query, int skip, int take, CancellationToken ct = default)
    {
        var q = ApplyFilter(_db.CustomsAndPackings.AsNoTracking(), query);
        var idQuery = q.GroupBy(x => x.DocumentGuid).Select(g => g.Min(x => x.Id));
        var docs = _db.CustomsAndPackings.AsNoTracking().Where(x => idQuery.Contains(x.Id));
        docs = ApplySort(docs, query);

        var total = await docs.CountAsync(ct);
        if (take <= 0) take = 20;
        if (skip < 0) skip = 0;

        var headers = await docs.Skip(skip).Take(take).ToListAsync(ct);
        var guids = headers
            .Select(x => x.DocumentGuid ?? "")
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sapMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (guids.Count > 0)
        {
            var sapRows = await _db.CustomsAndPackings.AsNoTracking()
                .Where(x => x.DocumentGuid != null && guids.Contains(x.DocumentGuid) && x.deleteSigh != "X")
                .Select(x => new { x.DocumentGuid, x.SAP_order_NO })
                .ToListAsync(ct);

            foreach (var g in sapRows.GroupBy(x => x.DocumentGuid ?? "", StringComparer.OrdinalIgnoreCase))
            {
                var sap = string.Join("+", g
                    .Select(x => (x.SAP_order_NO ?? "").Trim())
                    .Where(x => x.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase));
                sapMap[g.Key] = sap;
            }
        }

        foreach (var row in headers)
        {
            var g = row.DocumentGuid ?? "";
            if (sapMap.TryGetValue(g, out var sap) && sap.Length > 0)
                row.SAP_order_NO = sap;
        }

        return (headers, total);
    }

    public async Task<(bool Ok, string? Message)> DeleteByGuidAsync(string? guid, CancellationToken ct = default)
    {
        guid = (guid ?? "").Trim();
        if (guid.Length == 0)
            return (false, "单据编号为空");

        var rows = await _db.CustomsAndPackings.Where(x => x.DocumentGuid == guid).ToListAsync(ct);
        if (rows.Count == 0)
            return (false, "记录不存在");

        _db.CustomsAndPackings.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<List<ZyqdCustomsAndPackingModel>> GetByGuidAsync(string? guid, CancellationToken ct = default)
    {
        guid = (guid ?? "").Trim();
        if (guid.Length == 0)
            return [];

        return await _db.CustomsAndPackings.AsNoTracking()
            .Where(x => x.DocumentGuid == guid && x.deleteSigh != "X")
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
    }

    /// <summary>从报关 PI 生成装箱单草稿（不落库，保存时再写）。</summary>
    public async Task<List<ZyqdCustomsAndPackingModel>> LoadDraftFromCustomsPiAsync(
        string? piGuid, CancellationToken ct = default)
    {
        piGuid = (piGuid ?? "").Trim();
        if (piGuid.Length == 0)
            return [];

        var piRows = await _db.CotractPis.AsNoTracking()
            .Where(x => x.DocumentGuid == piGuid && x.deleteSigh != "X")
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        if (piRows.Count == 0)
            return [];

        var packingGuid = Guid.NewGuid().ToString("N");
        var now = DateTime.Now;
        var list = new List<ZyqdCustomsAndPackingModel>(piRows.Count);

        foreach (var item in piRows)
        {
            var model = new ZyqdCustomsAndPackingModel
            {
                DocumentGuid = packingGuid,
                Person = item.Person,
                materialNo = item.materialNo,
                materialName = item.materialName,
                ClientNo = item.ClientNo,
                ClientName = item.ClientName,
                materialType = item.materialType,
                materialModel = item.materiaCust,
                materiaSpecs = item.materiaSpecs,
                materiaPowered = item.materiaPowered,
                materiaColor = item.materiaColor,
                materiaCust = item.materiaCust,
                materiaCustLogo = item.materiaCustLogo,
                RadFreq = item.RadFreq,
                inBox = item.inBox,
                MstCtn = item.MstCtn,
                AI = item.AI,
                price = item.price,
                currency_unit = item.currency_unit,
                CreateTime = now,
                UpdateTime = now,
                qty = item.qty,
                PIcotract_code = item.INVOICE_NO,
                PI_Material_Name = item.materialName,
                Shipping_heads_up = string.Equals(item.Cotract_PI_type, "JCK", StringComparison.OrdinalIgnoreCase)
                    ? "新昌县捷昌进出口有限公司"
                    : "浙江捷昌线性驱动科技股份有限公司",
                var6 = "报关PI",
                var7 = "原始行",
                UNIT = item.unit.ToString("0.##"),
                deleteSigh = "N"
            };

            var clientNo = (item.ClientNo ?? "").Trim();
            var materialNo = (item.materialNo ?? "").Trim();
            if (clientNo.Length > 0 && materialNo.Length > 0)
            {
                var bas = await _db.CotractBases.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ClientNo == clientNo && x.materialNo == materialNo, ct);
                if (bas is not null)
                {
                    model.ClientNo = bas.ClientNo;
                    model.ClientName = bas.ClientName;
                    model.Customer_destination_country = bas.Customer_destination_country;
                    model.materiaSpecs = bas.materiaSpecs;
                    model.Tuo_Mark = bas.Tuo_Mark;
                    model.Box_label = bas.Box_label;
                    model.Box_label2 = bas.Box_label2;
                    model.Client_poNo = bas.Client_poNo;
                    model.materiaCust = bas.materiaCust;
                    model.materialModel = bas.materiaCust;
                    model.SAP_order_NO = bas.SAP_order_NO;
                    model.materialNo = bas.materialNo;
                    model.materialName = bas.materialName;
                    model.Packing_method = bas.Packing_method;
                    model.palletizing_method = bas.palletizing_method;
                    model.Packing_method_unit = bas.Packing_method_unit;
                    model.Carton_Size = bas.Carton_Size;
                    model.Pallet_Size = bas.Pallet_Size;
                    model.Net_weight = bas.Net_weight;
                    model.gross_weight = bas.gross_weight;
                    model.var4 = bas.var4;
                    model.palletizing_method_unit = bas.palletizing_method_unit;
                    model.Customer_outer_box_logo = bas.Customer_outer_box_logo;
                    model.Tuo_Mark_url = bas.Tuo_Mark_url;
                    model.Box_label_url = bas.Box_label_url;
                    model.nameplate_url = bas.nameplate_url;
                    model.Box_label_url2 = bas.Box_label_url2;
                    model.nameplate_url2 = bas.nameplate_url2;
                }
            }

            if (string.IsNullOrWhiteSpace(model.packing_list_type))
                model.packing_list_type = "总箱单";

            CustomsAndPackingCalc.RecalcLine(model);
            list.Add(model);
        }

        SyncHeaderTotals(list);
        return list;
    }

    public async Task<(bool Ok, string? Message, string? Guid)> SaveAsync(
        ZyqdCustomsAndPackingModel header,
        List<CustomsAndPackingLineDto> lines,
        string? empNo,
        bool isCreate,
        CancellationToken ct = default)
    {
        empNo = (empNo ?? "").Trim();
        if (empNo.Length == 0)
            return (false, "无法获取当前登录工号，请重新登录后再保存", null);
        if (lines.Count == 0)
            return (false, "请至少保留一行明细", null);

        var now = DateTime.Now;
        var guid = (header.DocumentGuid ?? "").Trim();
        List<ZyqdCustomsAndPackingModel> old = [];
        if (!isCreate && guid.Length > 0)
            old = await _db.CustomsAndPackings.Where(x => x.DocumentGuid == guid).ToListAsync(ct);

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
            if (guid.Length == 0)
                guid = Guid.NewGuid().ToString("N");
            createPerson = empNo;
            createTime = now;
            person = string.IsNullOrWhiteSpace(header.Person) ? empNo : header.Person.Trim();
        }

        var rows = new List<ZyqdCustomsAndPackingModel>(lines.Count);
        foreach (var line in lines)
        {
            rows.Add(new ZyqdCustomsAndPackingModel
            {
                DocumentGuid = guid,
                Person = person,
                ClientNo = header.ClientNo,
                ClientName = header.ClientName,
                Customer_destination_country = header.Customer_destination_country ?? line.Customer_destination_country,
                PIcotract_code = header.PIcotract_code,
                Shipping_heads_up = header.Shipping_heads_up,
                delivery_time = header.delivery_time,
                Trade_Term = header.Trade_Term,
                encasement_method = header.encasement_method,
                Box_type_quantity = header.Box_type_quantity,
                packing_list_type = string.IsNullOrWhiteSpace(header.packing_list_type) ? "总箱单" : header.packing_list_type,
                Shipping_method = header.Shipping_method,
                var6 = string.IsNullOrWhiteSpace(header.var6) ? "报关PI" : header.var6,
                materialNo = line.materialNo,
                materialName = line.materialName,
                materialType = line.materialType,
                materialModel = line.materialModel,
                materiaSpecs = line.materiaSpecs,
                materiaPowered = line.materiaPowered,
                materiaColor = line.materiaColor,
                materiaCust = line.materiaCust,
                UNIT = line.UNIT,
                price = line.price,
                qty = line.qty,
                currency_unit = line.currency_unit,
                Packing_method = line.Packing_method,
                Packing_method_unit = line.Packing_method_unit,
                var1 = line.var1,
                box_qty = line.box_qty,
                Pallet_qty = line.Pallet_qty,
                var3 = line.var3,
                var2 = line.var2,
                var4 = line.var4,
                Carton_Size = line.Carton_Size,
                Pallet_Size = line.Pallet_Size,
                palletizing_method = line.palletizing_method,
                palletizing_method_unit = line.palletizing_method_unit,
                Pallet_weight = line.Pallet_weight,
                Net_weight = line.Net_weight,
                gross_weight = line.gross_weight,
                total_Net_weight = line.total_Net_weight,
                total_gross_weight = line.total_gross_weight,
                volume = line.volume,
                Pallet_code = line.Pallet_code,
                emit = line.emit,
                packing_unit = line.packing_unit,
                sample_type = line.sample_type,
                SAP_order_NO = line.SAP_order_NO,
                Client_poNo = line.Client_poNo,
                PI_Material_Name = line.PI_Material_Name,
                var7 = string.IsNullOrWhiteSpace(line.var7) ? "原始行" : line.var7,
                var8 = line.var8,
                Customer_outer_box_logo = line.Customer_outer_box_logo,
                Tuo_Mark = line.Tuo_Mark,
                Tuo_Mark_url = line.Tuo_Mark_url,
                Box_label = line.Box_label,
                Box_label_url = line.Box_label_url,
                Box_label2 = line.Box_label2,
                Box_label_url2 = line.Box_label_url2,
                nameplate_url = line.nameplate_url,
                nameplate_url2 = line.nameplate_url2,
                CreatePerson = createPerson,
                CreateTime = createTime,
                UpdatePerson = empNo,
                UpdateTime = now,
                deleteSigh = "N"
            });
        }

        foreach (var r in rows)
            CustomsAndPackingCalc.RecalcLine(r);
        SyncHeaderTotals(rows);

        if (old.Count > 0)
            _db.CustomsAndPackings.RemoveRange(old);
        _db.CustomsAndPackings.AddRange(rows);
        await _db.SaveChangesAsync(ct);
        return (true, null, guid);
    }

    public static void SyncHeaderTotals(List<ZyqdCustomsAndPackingModel> rows)
    {
        if (rows.Count == 0) return;
        var lines = rows.Select((x, i) => ToLineDto(x, i + 1)).ToList();
        CustomsAndPackingCalc.ApplySummary(rows[0], lines);
        var h = rows[0];
        foreach (var r in rows)
        {
            r.all_qty = h.all_qty;
            r.all_box_qty = h.all_box_qty;
            r.all_Pallet_qty = h.all_Pallet_qty;
            r.all_Net_weight = h.all_Net_weight;
            r.all_gross_weight = h.all_gross_weight;
            r.all_total_Net_weight = h.all_total_Net_weight;
            r.all_total_gross_weight = h.all_total_gross_weight;
            r.all_volume = h.all_volume;
            r.all_emit = h.all_emit;
            r.all_Pallet_weight = h.all_Pallet_weight;
            r.var5 = h.var5;
        }
    }

    public static CustomsAndPackingLineDto ToLineDto(ZyqdCustomsAndPackingModel x, int tempId)
        => new()
        {
            TempId = tempId,
            materialNo = x.materialNo,
            materialName = x.materialName,
            materialType = x.materialType,
            materialModel = x.materialModel,
            materiaSpecs = x.materiaSpecs,
            materiaPowered = x.materiaPowered,
            materiaColor = x.materiaColor,
            materiaCust = x.materiaCust,
            UNIT = x.UNIT,
            price = x.price,
            qty = x.qty,
            currency_unit = x.currency_unit,
            Packing_method = x.Packing_method,
            Packing_method_unit = x.Packing_method_unit,
            var1 = x.var1,
            box_qty = x.box_qty,
            Pallet_qty = x.Pallet_qty,
            var3 = x.var3,
            var2 = x.var2,
            var4 = x.var4,
            Carton_Size = x.Carton_Size,
            Pallet_Size = x.Pallet_Size,
            palletizing_method = x.palletizing_method,
            palletizing_method_unit = x.palletizing_method_unit,
            Pallet_weight = x.Pallet_weight,
            Net_weight = x.Net_weight,
            gross_weight = x.gross_weight,
            total_Net_weight = x.total_Net_weight,
            total_gross_weight = x.total_gross_weight,
            volume = x.volume,
            Pallet_code = x.Pallet_code,
            emit = x.emit,
            packing_unit = x.packing_unit,
            sample_type = x.sample_type,
            SAP_order_NO = x.SAP_order_NO,
            Client_poNo = x.Client_poNo,
            PI_Material_Name = x.PI_Material_Name,
            var7 = x.var7,
            var8 = x.var8,
            Customer_outer_box_logo = x.Customer_outer_box_logo,
            Tuo_Mark = x.Tuo_Mark,
            Tuo_Mark_url = x.Tuo_Mark_url,
            Box_label = x.Box_label,
            Box_label_url = x.Box_label_url,
            Box_label2 = x.Box_label2,
            Box_label_url2 = x.Box_label_url2,
            nameplate_url = x.nameplate_url,
            nameplate_url2 = x.nameplate_url2,
            Customer_destination_country = x.Customer_destination_country,
        };

    private static IQueryable<ZyqdCustomsAndPackingModel> ApplyFilter(
        IQueryable<ZyqdCustomsAndPackingModel> q, CustomsAndPackingQueryDto query)
    {
        q = q.Where(x => x.deleteSigh != "X");

        if (!query.ViewAll)
        {
            var emp = (query.ViewerEmpNo ?? "").Trim();
            if (emp.Length > 0)
                q = q.Where(x => x.CreatePerson == emp || x.Person == emp);
            else
                q = q.Where(_ => false);
        }

        if (query.DateStart is { } ds)
        {
            var start = ds.Date;
            q = q.Where(x => x.CreateTime != null && x.CreateTime.Value >= start);
        }
        if (query.DateEnd is { } de)
        {
            var end = de.Date.AddDays(1);
            q = q.Where(x => x.CreateTime != null && x.CreateTime.Value < end);
        }

        var kw = (query.Keyword ?? "").Trim();
        if (kw.Length == 0)
            return q;

        return (query.FilterColumn ?? "").Trim() switch
        {
            "客户名称" => q.Where(x => x.ClientName != null && x.ClientName.Contains(kw)),
            "PI合同" => q.Where(x => x.PIcotract_code != null && x.PIcotract_code.Contains(kw)),
            "创建人" => q.Where(x => x.CreatePerson != null && x.CreatePerson.Contains(kw)),
            "SAP订单号" => q.Where(x =>
                (x.SAP_order_NO != null && x.SAP_order_NO.Contains(kw))
                || (x.Customer_destination_country != null && x.Customer_destination_country.Contains(kw))
                || (x.ClientName != null && x.ClientName.Contains(kw))
                || (x.packing_list_type != null && x.packing_list_type.Contains(kw))),
            _ => q.Where(x => x.ClientNo != null && x.ClientNo.Contains(kw))
        };
    }

    private static IQueryable<ZyqdCustomsAndPackingModel> ApplySort(
        IQueryable<ZyqdCustomsAndPackingModel> q, CustomsAndPackingQueryDto query)
    {
        var desc = query.SortDescending;
        return (query.SortLabel ?? "").Trim() switch
        {
            "ClientNo" => desc ? q.OrderByDescending(x => x.ClientNo) : q.OrderBy(x => x.ClientNo),
            "ClientName" => desc ? q.OrderByDescending(x => x.ClientName) : q.OrderBy(x => x.ClientName),
            "PIcotract_code" => desc ? q.OrderByDescending(x => x.PIcotract_code) : q.OrderBy(x => x.PIcotract_code),
            "Person" => desc ? q.OrderByDescending(x => x.Person) : q.OrderBy(x => x.Person),
            "CreatePerson" => desc ? q.OrderByDescending(x => x.CreatePerson) : q.OrderBy(x => x.CreatePerson),
            _ => desc ? q.OrderByDescending(x => x.CreateTime) : q.OrderBy(x => x.CreateTime)
        };
    }
}
