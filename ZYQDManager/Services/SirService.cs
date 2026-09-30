using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Services.Sir;

namespace ZYQDManager.Services;

public class SirService
{
    private readonly SqlServerDbContext _db;

    public SirService(SqlServerDbContext db)
    {
        _db = db;
    }

    public async Task<(List<ZyqdSirMainModel> Rows, int Total)> QueryPageAsync(
        SirQueryDto query, int skip, int take, CancellationToken ct = default)
    {
        var q = ApplyFilter(_db.SirMains.AsNoTracking(), query);
        q = ApplySort(q, query);

        var total = await q.CountAsync(ct);
        if (take <= 0) take = 20;
        if (skip < 0) skip = 0;

        var rows = await q.Skip(skip).Take(take).ToListAsync(ct);
        return (rows, total);
    }

    public async Task<(bool Ok, string? Message)> DeleteByGuidAsync(string? guid, CancellationToken ct = default)
    {
        guid = (guid ?? "").Trim();
        if (guid.Length == 0)
            return (false, "单据编号为空");

        var mains = await _db.SirMains.Where(x => x.guid == guid).ToListAsync(ct);
        var details = await _db.SirDetails.Where(x => x.main_guid == guid).ToListAsync(ct);
        var samples = await _db.SirSamples.Where(x => x.main_guid == guid).ToListAsync(ct);
        if (mains.Count == 0 && details.Count == 0 && samples.Count == 0)
            return (false, "记录不存在");

        _db.SirMains.RemoveRange(mains);
        _db.SirDetails.RemoveRange(details);
        _db.SirSamples.RemoveRange(samples);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<SirDocumentDto> GetDocumentAsync(
        string? guid, string? reportType, string? productType, CancellationToken ct = default)
    {
        guid = (guid ?? "").Trim();
        if (guid.Length == 0)
            guid = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(reportType))
        {
            reportType = "remote";
            productType = "基础版遥控器";
        }

        if (string.IsNullOrWhiteSpace(productType))
            productType = SirReportTypes.DefaultProduct(reportType);

        var main = await _db.SirMains.AsNoTracking().FirstOrDefaultAsync(x => x.guid == guid, ct);
        if (main is null)
        {
            main = new ZyqdSirMainModel
            {
                guid = guid,
                Report_type = reportType,
                product_type = productType,
                IssuanceDate = DateTime.Today,
                createTime = DateTime.Now,
            };
        }
        else
        {
            // 切换类型时前端仍带同一 guid，对齐当前选择
            main.Report_type = reportType;
            main.product_type = productType;
        }

        var details = await _db.SirDetails.AsNoTracking()
            .Where(x => x.main_guid == guid && x.Report_type == reportType)
            .ToListAsync(ct);
        if (details.Count == 0)
            details = BuildDefaultDetails(guid, reportType!, productType!);

        var samples = await _db.SirSamples.AsNoTracking()
            .Where(x => x.main_guid == guid && x.Report_type == reportType)
            .OrderBy(x => x.code)
            .ToListAsync(ct);
        // 遥控器测试(remote)：不留样，只上传附图
        // 电机/推杆/遥控器(新)：按产品类型自动生成留样部件行
        if (IsRemoteWithoutSample(reportType))
        {
            samples = [];
        }
        else if (samples.Count == 0)
        {
            samples = BuildDefaultSamples(guid, reportType!, productType!);
        }

        return new SirDocumentDto
        {
            Main = main,
            Details = details,
            Samples = samples,
        };
    }

    /// <summary>仅基础遥控器测试：无留样表，走主控板附图。</summary>
    public static bool IsRemoteWithoutSample(string? reportType)
        => string.Equals(reportType?.Trim(), "remote", StringComparison.OrdinalIgnoreCase);

    /// <summary>电机 / 推杆 / 遥控器(新)：需要样品留样记录。</summary>
    public static bool NeedsSampleFiling(string? reportType)
    {
        var t = (reportType ?? "").Trim();
        return t is "motor" or "putter" or "remote_new";
    }

    public async Task<(bool Ok, string? Message)> SaveAsync(SirDocumentDto doc, CancellationToken ct = default)
    {
        if (doc.Main is null)
            return (false, "主表为空");

        var guid = (doc.Main.guid ?? "").Trim();
        if (guid.Length == 0)
            return (false, "guid 为空");

        doc.Main.guid = guid;
        var reportType = (doc.Main.Report_type ?? "").Trim();
        if (reportType.Length == 0)
            return (false, "报告类型为空");

        if (doc.Main.DeliveryTimes == 0)
            return (false, "送样次数不可为0，请修改");

        if (!doc.Main.UL && !doc.Main.FCC && !doc.Main.CE && !doc.Main.RoHS && !doc.Main.REACH && !doc.Main.Certification_None)
            return (false, "认证测试要求必选");

        var existing = await _db.SirMains.FirstOrDefaultAsync(x => x.guid == guid, ct);
        if (existing is null)
        {
            doc.Main.createTime ??= DateTime.Now;
            doc.Main.Id = 0;
            _db.SirMains.Add(doc.Main);
        }
        else
        {
            doc.Main.Id = existing.Id;
            doc.Main.createTime ??= existing.createTime;
            _db.Entry(existing).CurrentValues.SetValues(doc.Main);
        }

        // 明细按 main_guid + Report_type 覆盖保存
        var oldDetails = await _db.SirDetails
            .Where(x => x.main_guid == guid && x.Report_type == reportType)
            .ToListAsync(ct);
        _db.SirDetails.RemoveRange(oldDetails);

        foreach (var d in doc.Details ?? [])
        {
            d.Id = 0;
            d.main_guid = guid;
            d.Report_type = reportType;
            d.createTime ??= DateTime.Now;
            _db.SirDetails.Add(d);
        }

        var oldSamples = await _db.SirSamples
            .Where(x => x.main_guid == guid && x.Report_type == reportType)
            .ToListAsync(ct);
        _db.SirSamples.RemoveRange(oldSamples);

        foreach (var s in doc.Samples ?? [])
        {
            s.Id = 0;
            s.main_guid = guid;
            s.Report_type = reportType;
            s.createTime ??= DateTime.Now;
            _db.SirSamples.Add(s);
        }

        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    private static IQueryable<ZyqdSirMainModel> ApplyFilter(IQueryable<ZyqdSirMainModel> q, SirQueryDto query)
    {
        if (query.DateStart is { } ds)
        {
            var start = ds.Date;
            q = q.Where(x => x.IssuanceDate >= start || (x.createTime != null && x.createTime >= start));
        }

        if (query.DateEnd is { } de)
        {
            var end = de.Date.AddDays(1);
            q = q.Where(x => x.IssuanceDate < end || (x.createTime != null && x.createTime < end));
        }

        var kw = (query.Keyword ?? "").Trim();
        if (kw.Length == 0)
            return q;

        return (query.FilterColumn ?? "客户") switch
        {
            "产品型号" => q.Where(x => x.Product_partNo != null && x.Product_partNo.Contains(kw)),
            "产品名称" => q.Where(x => x.ProductName != null && x.ProductName.Contains(kw)),
            "业务员" => q.Where(x => x.SalesmanName != null && x.SalesmanName.Contains(kw)),
            "报告类型" => q.Where(x => x.Report_type != null && x.Report_type.Contains(kw)),
            _ => q.Where(x => x.Client != null && x.Client.Contains(kw)),
        };
    }

    private static IQueryable<ZyqdSirMainModel> ApplySort(IQueryable<ZyqdSirMainModel> q, SirQueryDto query)
    {
        var label = (query.SortLabel ?? "").Trim();
        var desc = query.SortDesc;
        return label switch
        {
            "Department" => desc ? q.OrderByDescending(x => x.Department) : q.OrderBy(x => x.Department),
            "SalesmanName" => desc ? q.OrderByDescending(x => x.SalesmanName) : q.OrderBy(x => x.SalesmanName),
            "Client" => desc ? q.OrderByDescending(x => x.Client) : q.OrderBy(x => x.Client),
            "Report_type" => desc ? q.OrderByDescending(x => x.Report_type) : q.OrderBy(x => x.Report_type),
            "product_type" => desc ? q.OrderByDescending(x => x.product_type) : q.OrderBy(x => x.product_type),
            "Product_partNo" => desc ? q.OrderByDescending(x => x.Product_partNo) : q.OrderBy(x => x.Product_partNo),
            "ProductName" => desc ? q.OrderByDescending(x => x.ProductName) : q.OrderBy(x => x.ProductName),
            "SoftwareVersion" => desc ? q.OrderByDescending(x => x.SoftwareVersion) : q.OrderBy(x => x.SoftwareVersion),
            "IssuanceDate" => desc ? q.OrderByDescending(x => x.IssuanceDate) : q.OrderBy(x => x.IssuanceDate),
            "Test_Approved_By" => desc ? q.OrderByDescending(x => x.Test_Approved_By) : q.OrderBy(x => x.Test_Approved_By),
            _ => desc ? q.OrderByDescending(x => x.Id) : q.OrderBy(x => x.Id),
        };
    }

    private static List<ZyqdSirDetailModel> BuildDefaultDetails(string mainGuid, string reportType, string productType)
    {
        var dict = new SirDictionary();
        var specificationDescMap = dict._specificationDescMap_remote;
        var criteriaMap = dict._criteriaMap_remote;
        var fillingtypeMap = dict._fillingtype_remote;

        switch (productType)
        {
            case "JCA-内置RF控制机械行程交流管状电机":
                specificationDescMap = dict._specificationDescMap_JCA_in_RF_Mech;
                criteriaMap = dict._criteriaMap_JCA_in_RF_Mech;
                fillingtypeMap = dict._fillingtype_JCA_in_RF_Mech;
                break;
            case "JCA-内置RF控制电子行程交流管状电机":
                specificationDescMap = dict._specificationDescMap_JCA_in_RF_Elec;
                criteriaMap = dict._criteriaMap_JCA_in_RF_Elec;
                fillingtypeMap = dict._fillingtype_JCA_in_RF_Elec;
                break;
            case "JCA-无RF电子行程交流管状电机":
                specificationDescMap = dict._specificationDescMap_JCA_no_RF_Elec;
                criteriaMap = dict._criteriaMap_JCA_no_RF_Elec;
                fillingtypeMap = dict._fillingtype_JCA_no_RF_Elec;
                break;
            case "JCA-无RF机械行程交流管状电机":
                specificationDescMap = dict._specificationDescMap_JCA_no_RF_Mech;
                criteriaMap = dict._criteriaMap_JCA_no_RF_Mech;
                fillingtypeMap = dict._fillingtype_JCA_no_RF_Mech;
                break;
            case "JCC-锂电池款开合帘电机":
                specificationDescMap = dict._specificationDescMap_JCC_Li_Batt;
                criteriaMap = dict._criteriaMap_JCC_Li_Batt;
                fillingtypeMap = dict._fillingtype_JCC_Li_Batt;
                break;
            case "JCC-内置开关电源款开合帘电机":
                specificationDescMap = dict._specificationDescMap_JCC_Built_in_Sw_PS;
                criteriaMap = dict._criteriaMap_JCC_Built_in_Sw_PS;
                fillingtypeMap = dict._fillingtype_JCC_Built_in_Sw_PS;
                break;
            case "JCC-AE款开合帘电机":
                specificationDescMap = dict._specificationDescMap_JCC_AE_Type_Curtain;
                criteriaMap = dict._criteriaMap_JCC_AE_Type_Curtain;
                fillingtypeMap = dict._fillingtype_JCC_AE_Type_Curtain;
                break;
            case "JCD-LE款直流管状电机":
                specificationDescMap = dict._specificationDescMap_JCD_LE_Type_DC;
                criteriaMap = dict._criteriaMap_JCD_LE_Type_DC;
                fillingtypeMap = dict._fillingtype_JCD_LE_Type_DC;
                break;
            case "JCD-TE款直流管状电机":
                specificationDescMap = dict._specificationDescMap_JCD_TE_Type_DC;
                criteriaMap = dict._criteriaMap_JCD_TE_Type_DC;
                fillingtypeMap = dict._fillingtype_JCD_TE_Type_DC;
                break;
            case "JCD-AE款直流管状电机":
                specificationDescMap = dict._specificationDescMap_JCD_AE_Type_DC;
                criteriaMap = dict._criteriaMap_JCD_AE_Type_DC;
                fillingtypeMap = dict._fillingtype_JCD_AE_Type_DC;
                break;
            case "常规JCV系列管状电机":
                specificationDescMap = dict._specificationDescMap_Std_JCV_Series_Tubular;
                criteriaMap = dict._criteriaMap_Std_JCV_Series_Tubular;
                fillingtypeMap = dict._fillingtype_Std_JCV_Series_Tubular;
                break;
            case "内置锂电池款JCV系列管状电机":
                specificationDescMap = dict._specificationDescMap_Built_in_Li_Batt_Type_JCV_Series;
                criteriaMap = dict._criteriaMap_Built_in_Li_Batt_Type_JCV_Series;
                fillingtypeMap = dict._fillingtype_Built_in_Li_Batt_Type_JCV_Series;
                break;
            case "内置开关电源款JCV系列管状电机":
                specificationDescMap = dict._specificationDescMap_Built_in_Sw_PS_JCV_Series_Tubular;
                criteriaMap = dict._criteriaMap_Built_in_Sw_PS_JCV_Series_Tubular;
                fillingtypeMap = dict._fillingtype_Built_in_Sw_PS_JCV_Series_Tubular;
                break;
            case "基础版电机":
                specificationDescMap = dict._specificationDescMap_motor;
                criteriaMap = dict._criteriaMap_motor;
                fillingtypeMap = dict._fillingtype_motor;
                break;
            case "基础版遥控器":
                specificationDescMap = dict._specificationDescMap_remote;
                criteriaMap = dict._criteriaMap_remote;
                fillingtypeMap = dict._fillingtype_remote;
                break;
            case "一次电池款双向遥控器":
                specificationDescMap = dict._specificationDescMap_Disposable_Bidirectional_Remote;
                criteriaMap = dict._criteriaMap_Disposable_Bidirectional_Remote;
                fillingtypeMap = dict._fillingtype_Disposable_Bidirectional_Remote;
                break;
            case "一次电池款单向遥控器":
                specificationDescMap = dict._specificationDescMap_Disposable_Unidirectional_Remote;
                criteriaMap = dict._criteriaMap_Disposable_Unidirectional_Remote;
                fillingtypeMap = dict._fillingtype_Disposable_Unidirectional_Remote;
                break;
            case "充电款双向遥控器":
                specificationDescMap = dict._specificationDescMap_Rechargeable_Bidirectional_Remote;
                criteriaMap = dict._criteriaMap_Rechargeable_Bidirectional_Remote;
                fillingtypeMap = dict._fillingtype_Rechargeable_Bidirectional_Remote;
                break;
            case "充电款单向遥控器":
                specificationDescMap = dict._specificationDescMap_Rechargeable_Unidirectional_Remote;
                criteriaMap = dict._criteriaMap_Rechargeable_Unidirectional_Remote;
                fillingtypeMap = dict._fillingtype_Rechargeable_Unidirectional_Remote;
                break;
            case "单推杆遮阳棚":
                specificationDescMap = dict._specificationDescMap_Single_PushRodAwning;
                criteriaMap = dict._criteriaMap_Single_PushRodAwning;
                fillingtypeMap = dict._fillingtype_Single_PushRodAwning;
                break;
            case "双推杆遮阳棚":
                specificationDescMap = dict._specificationDescMap_Dual_PushRodAwning;
                criteriaMap = dict._criteriaMap_Dual_PushRodAwning;
                fillingtypeMap = dict._fillingtype_Dual_PushRodAwning;
                break;
        }

        IEnumerable<string> keys = specificationDescMap.Keys;
        if (reportType == "remote")
        {
            keys = specificationDescMap.Keys.Where(k =>
                k.StartsWith("1.") || k.StartsWith("2.") || k.StartsWith("3.") || k.StartsWith("4.") || !k.Contains('.'));
        }
        else if (reportType is "motor" or "remote_new" or "putter")
        {
            keys = specificationDescMap.Keys.Where(k => k.StartsWith("M."));
        }

        var list = new List<ZyqdSirDetailModel>();
        foreach (var specificationType in keys)
        {
            specificationDescMap.TryGetValue(specificationType, out var specification);
            criteriaMap.TryGetValue(specificationType, out var criteria);
            fillingtypeMap.TryGetValue(specificationType, out var fillingtype);
            list.Add(new ZyqdSirDetailModel
            {
                main_guid = mainGuid,
                Report_type = reportType,
                createTime = DateTime.Now,
                Specification_type = specificationType,
                Specification = specification ?? "",
                Criteria = criteria ?? "",
                filling_type = fillingtype ?? "0",
            });
        }

        return list;
    }

    private static List<ZyqdSirSampleForFilingModel> BuildDefaultSamples(string guid, string reportType, string _)
    {
        // 新建默认只给一行，其余由用户「添加样品记录」自行增加
        return
        [
            new ZyqdSirSampleForFilingModel
            {
                main_guid = guid,
                Report_type = reportType,
                Specification_type = "S.1",
                Note = "部件1",
                Note1 = "",
                createTime = DateTime.Now,
                code = 1,
                ImageUrls = "[]",
            }
        ];
    }
}
