using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 装箱单 Excel 填充（对齐老系统 SetCustomsAndPackingExcel），再交给 LibreOffice 转 PDF。
/// type：报关 / 客户 / 生产
/// </summary>
public class CustomsAndPackingExcelService
{
    private const string LegacyHttpClient = "LegacyFiles";

    /// <summary>有铭牌图但合同基础未维护类型，阻止报关/生产导出。</summary>
    public sealed record NameplateTypeExportIssue(
        string ClientNo,
        string MaterialNo,
        string MissingField,
        int? BaseId);

    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpFactory;
    private readonly SqlServerDbContext _db;
    private readonly string _imageBase;

    public CustomsAndPackingExcelService(
        IWebHostEnvironment env,
        IHttpClientFactory httpFactory,
        SqlServerDbContext db,
        IOptions<LegacyFilesOptions> legacyFiles)
    {
        _env = env;
        _httpFactory = httpFactory;
        _db = db;
        _imageBase = legacyFiles.Value.ResolvedBaseUrl();
    }

    public async Task<byte[]> BuildAsync(string? documentGuid, string type, CancellationToken ct = default)
    {
        type = NormalizeType(type);
        var lists = await _db.CustomsAndPackings.AsNoTracking()
            .Where(x => x.DocumentGuid == documentGuid && x.deleteSigh != "X")
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        if (lists.Count == 0)
            throw new InvalidOperationException("装箱单无数据，请先保存");

        if (type is "报关" or "生产")
        {
            var issues = await FindMissingNameplateTypesAsync(lists, ct);
            if (issues.Count > 0)
                throw new InvalidOperationException(FormatNameplateTypeIssues(issues));
        }

        foreach (var row in lists)
            CustomsAndPackingCalc.RecalcLine(row);
        CustomsAndPackingService.SyncHeaderTotals(lists);

        var main = lists[0];
        var images = await DownloadImagesAsync(lists, ct);
        var path = ResolveTemplatePath(type);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet(1);

        switch (type)
        {
            case "客户":
                await FillCustomerAsync(ws, main, lists, images, ct);
                break;
            case "生产":
                await FillProductAsync(ws, main, lists, images, ct);
                break;
            default:
                await FillCustomsAsync(ws, main, lists, images, ct);
                break;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        // ClosedXML 会丢掉 dxfs，若仍保留 tableStyles，Excel 会提示“内容有问题”
        return StripOrphanTableStyles(ms.ToArray());
    }

    /// <summary>
    /// ClosedXML 已知会清除 differential formats (dxfs)，但可能留下引用它们的 tableStyles，
    /// 导致 MS Excel 打开时要求修复。去掉悬空的 tableStyles/dxfs 即可。
    /// </summary>
    private static byte[] StripOrphanTableStyles(byte[] xlsx)
    {
        try
        {
            using var probe = new MemoryStream(xlsx);
            using (var zip = new ZipArchive(probe, ZipArchiveMode.Read))
            {
                var entry = zip.GetEntry("xl/styles.xml");
                if (entry is null) return xlsx;
                using var s = entry.Open();
                var doc = XDocument.Load(s);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                var root = doc.Root;
                if (root is null) return xlsx;
                var dxfCount = root.Element(ns + "dxfs")?.Elements().Count() ?? 0;
                if (root.Element(ns + "tableStyles") is null || dxfCount > 0)
                    return xlsx;
            }

            using var output = new MemoryStream();
            output.Write(xlsx);
            output.Position = 0;
            using (var zip = new ZipArchive(output, ZipArchiveMode.Update, leaveOpen: true))
            {
                var entry = zip.GetEntry("xl/styles.xml")!;
                XDocument doc;
                using (var s = entry.Open())
                    doc = XDocument.Load(s);
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                doc.Root!.Element(ns + "tableStyles")?.Remove();
                doc.Root.Element(ns + "dxfs")?.Remove();
                entry.Delete();
                var neu = zip.CreateEntry("xl/styles.xml");
                using var w = neu.Open();
                doc.Save(w);
            }
            return output.ToArray();
        }
        catch
        {
            return xlsx;
        }
    }

    public static string NormalizeType(string? type)
    {
        var t = (type ?? "").Trim();
        return t is "客户" or "生产" or "报关" ? t : "报关";
    }

    public static string DownloadExcelName(ZyqdCustomsAndPackingModel main, string type)
    {
        type = NormalizeType(type);
        var client = Sanitize(main.ClientName);
        var pi = Sanitize(main.PIcotract_code);
        var baseName = client.Length > 0 ? client : "装箱单";
        if (pi.Length > 0) baseName += "-" + pi;
        return $"{baseName}-{type}.xlsx";
    }

    public static string DownloadPdfName(ZyqdCustomsAndPackingModel main, string type)
    {
        var name = DownloadExcelName(main, type);
        return name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? name[..^5] + ".pdf"
            : name + ".pdf";
    }

    private string ResolveTemplatePath(string type)
    {
        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var dir = Path.Combine(webRoot, "Templates");
        var preferred = type switch
        {
            "客户" => "装箱单模板-客户.xlsx",
            "生产" => "装箱单模板-生产.xlsx",
            _ => "装箱单模板-报关.xlsx",
        };
        var path = Path.Combine(dir, preferred);
        if (File.Exists(path))
            return path;

        if (Directory.Exists(dir))
        {
            var hit = Directory.GetFiles(dir, "*.xlsx")
                .FirstOrDefault(f => Path.GetFileName(f).Contains("装箱", StringComparison.OrdinalIgnoreCase)
                                     && Path.GetFileName(f).Contains(type, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
                return hit;
        }

        throw new FileNotFoundException("未找到装箱单模板，请确认文件在 wwwroot/Templates/" + preferred);
    }

    private async Task FillCustomsAsync(
        IXLWorksheet ws,
        ZyqdCustomsAndPackingModel main,
        List<ZyqdCustomsAndPackingModel> lists,
        Dictionary<string, byte[]> images,
        CancellationToken ct)
    {
        const int dataNpoi = 3; // Excel row 4
        EnsureRows(ws, dataNpoi + 1, lists.Count);

        Set(ws, 1, 9, (main.delivery_time ?? "").Replace("-", "/", StringComparison.Ordinal));
        Set(ws, 1, 11, main.Trade_Term);
        Set(ws, 1, 13, main.encasement_method);
        Set(ws, 1, 17, main.Box_type_quantity);
        Set(ws, 1, 23, main.packing_list_type);
        Set(ws, 1, 29, main.Shipping_method);

        var title = BuildTitle(main, lists);
        var typeMap = await LoadNameplateTypeMapAsync(lists, ct);

        for (var i = 0; i < lists.Count; i++)
        {
            var item = lists[i];
            var r = dataNpoi + i;
            Set(ws, r, 1, " ");

            if (!TryPicture(ws, images, item.Tuo_Mark_url, r, 0))
                Set(ws, r, 0, item.Tuo_Mark);

            if (!TryPicture(ws, images, item.Box_label_url, r, 2))
                Set(ws, r, 2, await ResolveBoxLabelAsync(item, prefer2: false, ct));
            if (!TryPicture(ws, images, item.Box_label_url2, r, 3))
                Set(ws, r, 3, await ResolveBoxLabelAsync(item, prefer2: true, ct));

            SetText(ws, r, 4, item.Client_poNo);
            Set(ws, r, 5, item.materialModel);
            // G/H/I/J：铭牌1、类型1、铭牌2、类型2；其后列整体 +2
            var (npType1, npType2) = LookupNameplateTypes(typeMap, item);
            PutNameplate(ws, images, item.nameplate_url, r, 6, npType1, 7);
            PutNameplate(ws, images, item.nameplate_url2, r, 8, npType2, 9);
            Set(ws, r, 10, item.SAP_order_NO);
            Set(ws, r, 11, item.materialNo);
            Set(ws, r, 12, item.materialName);
            // 数量 / 净重 / 毛重：数值输入；箱数/托数/重量/体积：Excel 公式（改数后可重算）
            WriteCustomsCalcRow(ws, r, item);
            Set(ws, r, 14, FormatPacking(item));
            Set(ws, r, 17, item.Carton_Size);
            Set(ws, r, 18, ExportPalletSizeText(item));
            if (CustomsAndPackingCalc.IsMergeMember(item.var8))
            {
                Set(ws, r, 21, "");
            }
            else if (!string.IsNullOrWhiteSpace(item.var2))
            {
                var full = item.Pallet_qty == 0
                    ? ""
                    : $"{item.palletizing_method}{item.palletizing_method_unit} {Num(item.Pallet_qty)}托\n";
                Set(ws, r, 21, full + item.var4 + item.palletizing_method_unit + " 1托");
            }
            else
            {
                var dd = item.Pallet_qty == 0
                    ? ""
                    : $"{item.palletizing_method}{item.palletizing_method_unit} {Num(item.Pallet_qty)}托";
                if (!string.IsNullOrWhiteSpace(item.var4))
                    dd = string.IsNullOrEmpty(dd) ? item.var4 : dd + "\n" + item.var4;
                Set(ws, r, 21, dd);
            }

            Set(ws, r, 28, item.Pallet_code);
            Set(ws, r, 29, item.emit);
            Set(ws, r, 31, item.packing_unit);
            Set(ws, r, 32, item.Shipping_heads_up);
            Set(ws, r, 33, item.PIcotract_code);
            Set(ws, r, 34, item.PI_Material_Name);
            Set(ws, r, 35, item.sample_type);
        }

        var totalRow = dataNpoi + lists.Count;
        var firstExcel = dataNpoi + 1; // 数据首行 Excel 行号
        var lastExcel = dataNpoi + lists.Count;
        WriteCustomsTotalFormulas(ws, totalRow, firstExcel, lastExcel, main);
        Set(ws, totalRow, 29, main.all_emit);
        HideCustomsHelperColumns(ws);

        var desc =
            $"总计:{Num(main.all_box_qty)}箱/{Num(main.all_Pallet_qty)}托/{main.all_emit}箱散发/{Num(main.all_total_gross_weight)}KG/{Num(main.all_volume)}m³{main.Shipping_method}出运";
        Set(ws, totalRow + 1, 11, desc);
        Set(ws, 0, 0, title);

        MergeSame(ws, lists.Select(x => x.ClientNo).ToList(), [1], dataNpoi + 1);
        MergeSame(ws, lists.Select(x => x.Client_poNo).ToList(), [4], dataNpoi + 1);
        MergeSame(ws, lists.Select(x => x.packing_unit).ToList(), [31], dataNpoi + 1);
        MergeSame(ws, lists.Select(x => x.Shipping_heads_up).ToList(), [32], dataNpoi + 1);
        MergeSame(ws, lists.Select(x => x.PIcotract_code).ToList(), [33], dataNpoi + 1);

        if (HasUrl(lists[0].Customer_outer_box_logo))
            TryPicture(ws, images, lists[0].Customer_outer_box_logo, dataNpoi + lists.Count / 2, 1);
    }

    private async Task FillCustomerAsync(
        IXLWorksheet ws,
        ZyqdCustomsAndPackingModel main,
        List<ZyqdCustomsAndPackingModel> lists,
        Dictionary<string, byte[]> images,
        CancellationToken ct)
    {
        const int dataNpoi = 4;
        lists = lists.OrderByDescending(x => x.Client_poNo).ToList();
        EnsureRows(ws, dataNpoi + 1, lists.Count);

        var invoiceNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < lists.Count; i++)
        {
            var item = lists[i];
            var r = dataNpoi + i;
            if (!string.IsNullOrWhiteSpace(item.PIcotract_code))
                invoiceNos.Add(item.PIcotract_code.Trim());

            Set(ws, r, 0, (i + 1).ToString(CultureInfo.InvariantCulture));
            SetText(ws, r, 1, item.Client_poNo);
            Set(ws, r, 2, item.materialName);
            Set(ws, r, 3, item.materiaSpecs);
            Set(ws, r, 4, Num(item.qty));
            Set(ws, r, 5, (item.Packing_method ?? "") + "PCS/CTN");
            Set(ws, r, 6, item.Carton_Size);
            Set(ws, r, 7, Num(item.box_qty));
            Set(ws, r, 8, ExportPalletSizeText(item));
            Set(ws, r, 9, Num(ExportDisplayPalletQty(item)));
            Set(ws, r, 10, Num(item.total_Net_weight));
            Set(ws, r, 11, Num(item.total_gross_weight));
            Set(ws, r, 12, item.Pallet_code);
            Set(ws, r, 13, Num(item.volume));
            Set(ws, r, 14, await ResolveBoxLabelAsync(item, prefer2: false, ct));
            Set(ws, r, 15, await ResolveTuoMarkAsync(item, ct));
            _ = images; // 客户模板以文本唛头为主
        }

        var totalRow = dataNpoi + lists.Count;
        Set(ws, totalRow, 4, Num(main.all_qty));
        Set(ws, totalRow, 7, Num(main.all_box_qty));
        Set(ws, totalRow, 9, Num(main.all_Pallet_qty));
        Set(ws, totalRow, 10, Num(main.all_total_Net_weight));
        Set(ws, totalRow, 11, Num(main.all_total_gross_weight));
        Set(ws, totalRow, 13, Num(main.all_volume));

        if (lists.Count > 1)
            MergeSame(ws, lists.Select(x => x.Client_poNo).ToList(), [1], dataNpoi + 1);

        var invoice = await ResolveInvoiceNoAsync(invoiceNos, ct);
        if (!string.IsNullOrWhiteSpace(invoice))
            Set(ws, 2, 15, "INVOICE_NO:" + invoice);
    }

    private async Task FillProductAsync(
        IXLWorksheet ws,
        ZyqdCustomsAndPackingModel main,
        List<ZyqdCustomsAndPackingModel> lists,
        Dictionary<string, byte[]> images,
        CancellationToken ct)
    {
        const int dataNpoi = 2;
        EnsureRows(ws, dataNpoi + 1, lists.Count);

        var title = BuildTitle(main, lists);
        var typeMap = await LoadNameplateTypeMapAsync(lists, ct);
        for (var i = 0; i < lists.Count; i++)
        {
            var item = lists[i];
            var r = dataNpoi + i;

            if (!TryPicture(ws, images, item.Customer_outer_box_logo, r, 0))
                Set(ws, r, 0, "");
            if (!TryPicture(ws, images, item.Tuo_Mark_url, r, 1))
                Set(ws, r, 1, item.Tuo_Mark);
            if (!TryPicture(ws, images, item.Box_label_url, r, 2))
                Set(ws, r, 2, await ResolveBoxLabelAsync(item, prefer2: false, ct));
            if (!TryPicture(ws, images, item.Box_label_url2, r, 3))
                Set(ws, r, 3, await ResolveBoxLabelAsync(item, prefer2: true, ct));

            Set(ws, r, 4, item.Client_poNo);
            Set(ws, r, 5, item.materialModel);
            Set(ws, r, 6, item.SAP_order_NO);
            // H/I/J/K：铭牌1、类型1、铭牌2、类型2；其后物料列整体 +2
            var (npType1, npType2) = LookupNameplateTypes(typeMap, item);
            PutNameplate(ws, images, item.nameplate_url, r, 7, npType1, 8);
            PutNameplate(ws, images, item.nameplate_url2, r, 9, npType2, 10);
            Set(ws, r, 11, item.materialNo);
            Set(ws, r, 12, item.materialName);
            Set(ws, r, 13, Num(item.qty));
            Set(ws, r, 14, FormatPacking(item));
            Set(ws, r, 15, Num(item.box_qty));
            Set(ws, r, 17, item.Carton_Size);

            Set(ws, r, 16, Num(ExportDisplayPalletQty(item)));
            Set(ws, r, 18, ExportPalletSizeText(item));
            if (CustomsAndPackingCalc.IsMergeMember(item.var8))
            {
                Set(ws, r, 19, "");
            }
            else if (!string.IsNullOrWhiteSpace(item.var2))
            {
                var full = item.Pallet_qty == 0
                    ? ""
                    : $"{item.palletizing_method}{item.palletizing_method_unit} {Num(item.Pallet_qty)}托\n";
                Set(ws, r, 19, full + item.var4 + item.palletizing_method_unit + " 1托");
            }
            else
            {
                var dd = item.Pallet_qty == 0
                    ? ""
                    : $"{item.palletizing_method}{item.palletizing_method_unit} {Num(item.Pallet_qty)}托";
                if (!string.IsNullOrWhiteSpace(item.var4))
                    dd = string.IsNullOrEmpty(dd) ? item.var4 : dd + "\n" + item.var4;
                Set(ws, r, 19, dd);
            }

            Set(ws, r, 20, Num(item.Pallet_weight));
            Set(ws, r, 21, Num(item.Net_weight));
            Set(ws, r, 22, Num(item.gross_weight));
            Set(ws, r, 23, Num(item.total_Net_weight));
            Set(ws, r, 24, Num(item.total_gross_weight));
            Set(ws, r, 25, Num(item.volume));
            Set(ws, r, 26, item.Pallet_code);
            Set(ws, r, 27, item.emit);
        }

        var totalRow = dataNpoi + lists.Count;
        Set(ws, totalRow, 13, Num(main.all_qty));
        Set(ws, totalRow, 15, Num(main.all_box_qty));
        Set(ws, totalRow, 16, Num(main.all_Pallet_qty));
        Set(ws, totalRow, 23, Num(main.all_total_Net_weight));
        Set(ws, totalRow, 24, Num(main.all_total_gross_weight));
        Set(ws, totalRow, 25, Num(main.all_volume));
        Set(ws, totalRow, 27, main.all_emit);
        Set(ws, 0, 0, title);

        if (lists.Count > 1)
            MergeSame(ws, lists.Select(x => x.Pallet_code).ToList(), [1], dataNpoi + 1);
    }

    private async Task<string?> ResolveInvoiceNoAsync(HashSet<string> invoiceNos, CancellationToken ct)
    {
        if (invoiceNos.Count == 0) return null;
        var candidates = await _db.CotractPis.AsNoTracking()
            .Where(x => x.deleteSigh != "X" && x.INVOICE_NO != null && invoiceNos.Contains(x.INVOICE_NO))
            .Where(x => x.var6 == "标准订单"
                        || x.var6 == "贸易订单（1300）"
                        || x.var6 == "贸易订单（1300-2001）"
                        || x.var6 == "寄售订单（1300-2001）")
            .OrderByDescending(x => x.TOTAL)
            .Select(x => x.INVOICE_NO)
            .FirstOrDefaultAsync(ct);
        return candidates;
    }

    private async Task<string?> ResolveBoxLabelAsync(ZyqdCustomsAndPackingModel item, bool prefer2, CancellationToken ct)
    {
        var stored = prefer2 ? item.Box_label2 : item.Box_label;
        if (string.IsNullOrWhiteSpace(stored))
            return stored;

        var client = (item.ClientNo ?? "").Trim();
        var mat = (item.materialNo ?? "").Trim();
        if (client.Length == 0 || mat.Length == 0)
            return stored;

        var bas = await _db.CotractBases.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientNo == client && x.materialNo == mat, ct);
        if (bas is null) return stored;
        var raw = prefer2 ? bas.Box_label2 : bas.Box_label;
        return string.IsNullOrWhiteSpace(raw) ? stored : raw;
    }

    /// <summary>报关/生产导出前：有铭牌图但合同基础缺类型的行。</summary>
    public async Task<IReadOnlyList<NameplateTypeExportIssue>> FindMissingNameplateTypesAsync(
        string? documentGuid, CancellationToken ct = default)
    {
        var lists = await _db.CustomsAndPackings.AsNoTracking()
            .Where(x => x.DocumentGuid == documentGuid && x.deleteSigh != "X")
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        return await FindMissingNameplateTypesAsync(lists, ct);
    }

    public async Task<IReadOnlyList<NameplateTypeExportIssue>> FindMissingNameplateTypesAsync(
        List<ZyqdCustomsAndPackingModel> lists, CancellationToken ct = default)
    {
        var map = await LoadNameplateTypeMapAsync(lists, ct);
        var issues = new List<NameplateTypeExportIssue>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in lists)
        {
            var key = NameplateTypeKey(item.ClientNo, item.materialNo);
            map.TryGetValue(key, out var info);
            var client = (item.ClientNo ?? "").Trim();
            var mat = (item.materialNo ?? "").Trim();

            if (HasUrl(item.nameplate_url) && string.IsNullOrWhiteSpace(info.Type1))
            {
                var sig = key + "|1";
                if (seen.Add(sig))
                    issues.Add(new NameplateTypeExportIssue(client, mat, "铭牌1类型", info.BaseId));
            }

            if (HasUrl(item.nameplate_url2) && string.IsNullOrWhiteSpace(info.Type2))
            {
                var sig = key + "|2";
                if (seen.Add(sig))
                    issues.Add(new NameplateTypeExportIssue(client, mat, "铭牌2类型", info.BaseId));
            }
        }

        return issues;
    }

    public static string FormatNameplateTypeIssues(IReadOnlyList<NameplateTypeExportIssue> issues)
    {
        if (issues.Count == 0) return "";
        var lines = issues.Take(8)
            .Select(x => $"客户 {x.ClientNo} / 物料 {x.MaterialNo}：缺{x.MissingField}")
            .ToList();
        if (issues.Count > 8)
            lines.Add($"…等共 {issues.Count} 处");
        return "有铭牌图片但未维护铭牌类型，请先到合同基础数据补全：\n" + string.Join("\n", lines);
    }

    /// <summary>按客户+物料从合同基础批量取铭牌类型。</summary>
    private async Task<Dictionary<string, (int? BaseId, string? Type1, string? Type2)>> LoadNameplateTypeMapAsync(
        List<ZyqdCustomsAndPackingModel> lists, CancellationToken ct)
    {
        var clientNos = lists.Select(x => (x.ClientNo ?? "").Trim()).Where(x => x.Length > 0).Distinct().ToList();
        var matNos = lists.Select(x => (x.materialNo ?? "").Trim()).Where(x => x.Length > 0).Distinct().ToList();
        if (clientNos.Count == 0 || matNos.Count == 0)
            return new Dictionary<string, (int?, string?, string?)>(StringComparer.OrdinalIgnoreCase);

        var rows = await _db.CotractBases.AsNoTracking()
            .Where(x => x.ClientNo != null && x.materialNo != null
                        && clientNos.Contains(x.ClientNo) && matNos.Contains(x.materialNo))
            .Select(x => new { x.Id, x.ClientNo, x.materialNo, x.nameplate_type, x.nameplate_type2 })
            .ToListAsync(ct);

        var map = new Dictionary<string, (int?, string?, string?)>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            var key = NameplateTypeKey(r.ClientNo, r.materialNo);
            if (key.Length == 0 || map.ContainsKey(key)) continue;
            map[key] = (r.Id, NameplateTypes.Normalize(r.nameplate_type), NameplateTypes.Normalize(r.nameplate_type2));
        }

        return map;
    }

    private static string NameplateTypeKey(string? clientNo, string? materialNo)
    {
        var c = (clientNo ?? "").Trim();
        var m = (materialNo ?? "").Trim();
        return c.Length == 0 || m.Length == 0 ? "" : c + "\u001f" + m;
    }

    private static (string? Type1, string? Type2) LookupNameplateTypes(
        Dictionary<string, (int? BaseId, string? Type1, string? Type2)> map, ZyqdCustomsAndPackingModel item)
    {
        var key = NameplateTypeKey(item.ClientNo, item.materialNo);
        if (key.Length > 0 && map.TryGetValue(key, out var t))
            return (t.Type1, t.Type2);
        return (null, null);
    }

    /// <summary>写铭牌图（无图写「无」）及对应类型（无图不写类型）。</summary>
    private void PutNameplate(
        IXLWorksheet ws,
        Dictionary<string, byte[]> images,
        string? url,
        int npoiRow,
        int imgCol,
        string? type,
        int typeCol)
    {
        var hasImg = TryPicture(ws, images, url, npoiRow, imgCol);
        if (!hasImg)
            Set(ws, npoiRow, imgCol, "无");
        Set(ws, npoiRow, typeCol, hasImg ? type : null);
    }

    private async Task<string?> ResolveTuoMarkAsync(ZyqdCustomsAndPackingModel item, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(item.Tuo_Mark))
            return item.Tuo_Mark;
        var client = (item.ClientNo ?? "").Trim();
        var mat = (item.materialNo ?? "").Trim();
        if (client.Length == 0 || mat.Length == 0)
            return item.Tuo_Mark;
        var bas = await _db.CotractBases.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientNo == client && x.materialNo == mat, ct);
        return string.IsNullOrWhiteSpace(bas?.Tuo_Mark) ? item.Tuo_Mark : bas.Tuo_Mark;
    }

    private static string BuildTitle(ZyqdCustomsAndPackingModel main, List<ZyqdCustomsAndPackingModel> lists)
    {
        var title = (main.Customer_destination_country ?? "") + "  " + (main.ClientName ?? "") + " ";
        foreach (var sap in lists.Select(x => (x.SAP_order_NO ?? "").Trim()).Where(x => x.Length > 0).Distinct())
        {
            if (!title.Contains(sap, StringComparison.Ordinal))
                title += sap + "+";
        }
        return title.TrimEnd('+') + "  装箱单";
    }

    private static string FormatPacking(ZyqdCustomsAndPackingModel item)
    {
        var baseText = (item.Packing_method ?? "") + (item.Packing_method_unit ?? "");
        if (string.IsNullOrWhiteSpace(item.var1) || item.var1 == "0")
            return baseText;
        if (Math.Abs(item.box_qty - 1) < 0.0001)
            return baseText;
        return baseText + "\n" + item.var1 + item.Packing_method_unit;
    }

    /// <summary>
    /// 导出用托盘尺寸（尾托=拼托余量，尺寸由业务填写）：
    /// 整托≠0：有尾托尺寸 → 托盘尺寸+尾托尺寸；无 → 托盘尺寸；
    /// 整托=0：有尾托尺寸 → 只出尾托尺寸；无 → 都不显示。
    /// 拼托挂靠行始终不显示。
    /// </summary>
    private static string? ExportPalletSizeText(ZyqdCustomsAndPackingModel item)
    {
        if (CustomsAndPackingCalc.IsMergeMember(item.var8))
            return "";

        var hasTail = !string.IsNullOrWhiteSpace(item.var2);
        var hasFull = !string.IsNullOrWhiteSpace(item.Pallet_Size);

        if (item.Pallet_qty != 0)
        {
            if (hasTail && hasFull)
                return item.Pallet_Size + "\n" + item.var2;
            if (hasTail)
                return item.var2;
            return item.Pallet_Size;
        }

        // 整托数 = 0
        return hasTail ? item.var2 : "";
    }

    private static double ExportDisplayPalletQty(ZyqdCustomsAndPackingModel item)
        => CustomsAndPackingCalc.DisplayPalletQty(CustomsAndPackingService.ToLineDto(item, 0));

    private static void EnsureRows(IXLWorksheet ws, int templateExcelRow, int count)
    {
        if (count <= 1) return;
        var extra = count - 1;
        ws.Row(templateExcelRow).InsertRowsBelow(extra);
        var src = ws.Range(templateExcelRow, 1, templateExcelRow, ws.LastColumnUsed()?.ColumnNumber() ?? 40);
        for (var i = 0; i < extra; i++)
            src.CopyTo(ws.Cell(templateExcelRow + 1 + i, 1));
    }

    private static void MergeSame(IXLWorksheet ws, List<string?> keys, int[] npoiCols, int excelStartRow)
    {
        if (keys.Count <= 1) return;
        var i = 0;
        while (i < keys.Count)
        {
            var j = i + 1;
            while (j < keys.Count && string.Equals(keys[i] ?? "", keys[j] ?? "", StringComparison.Ordinal))
                j++;
            if (j - i > 1 && !string.IsNullOrWhiteSpace(keys[i]))
            {
                foreach (var c in npoiCols)
                {
                    var col = c + 1;
                    var range = ws.Range(excelStartRow + i, col, excelStartRow + j - 1, col);
                    if (!range.Cells().Any(cell => cell.IsMerged()))
                        range.Merge();
                }
            }
            i = j;
        }
    }

    private async Task<Dictionary<string, byte[]>> DownloadImagesAsync(
        List<ZyqdCustomsAndPackingModel> lists, CancellationToken ct)
    {
        var paths = lists
            .SelectMany(x => new[]
            {
                x.Customer_outer_box_logo, x.Tuo_Mark_url, x.Box_label_url, x.Box_label_url2,
                x.nameplate_url, x.nameplate_url2
            })
            .Where(HasUrl)
            .Select(p => LegacyFileUrl.Resolve(p, _imageBase))
            .Where(u => u.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        if (paths.Count == 0) return result;

        var http = _httpFactory.CreateClient(LegacyHttpClient);
        var bag = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        using var gate = new SemaphoreSlim(4);
        await Task.WhenAll(paths.Select(async url =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var bytes = await TryDownloadAsync(http, url, ct);
                if (bytes is { Length: > 0 })
                    bag[url] = bytes;
            }
            finally { gate.Release(); }
        }));
        foreach (var kv in bag)
            result[kv.Key] = kv.Value;
        return result;
    }

    private static async Task<byte[]?> TryDownloadAsync(HttpClient http, string url, CancellationToken ct)
    {
        try
        {
            using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            return bytes.Length > 0 ? bytes : null;
        }
        catch { return null; }
    }

    private bool TryPicture(
        IXLWorksheet ws, Dictionary<string, byte[]> images, string? path, int npoiRow, int npoiCol)
    {
        if (!HasUrl(path)) return false;
        var url = LegacyFileUrl.Resolve(path, _imageBase);
        if (!images.TryGetValue(url, out var bytes))
            return false;

        try
        {
            using var ms = new MemoryStream(bytes);
            var format = DetectPictureFormat(bytes);
            var name = "pk_" + Guid.NewGuid().ToString("N")[..8];
            var pic = format == XLPictureFormat.Unknown
                ? ws.AddPicture(ms, name)
                : ws.AddPicture(ms, format, name);

            // 对齐老系统：原图优先（上限 500×400），必要时撑开列/行，再居中放入单元格
            const double pxPerColChar = 7.0;
            const double pad = 6;
            const double maxPxW = 500;
            const double maxPxH = 400;
            const double maxColChars = 255;
            const double maxRowPts = 409;

            var xlRow = npoiRow + 1;
            var xlCol = npoiCol + 1;
            var cell = ws.Cell(xlRow, xlCol);

            var imgW = Math.Max(1.0, pic.OriginalWidth);
            var imgH = Math.Max(1.0, pic.OriginalHeight);
            var scale = Math.Min(1.0, Math.Min(maxPxW / imgW, maxPxH / imgH));
            imgW *= scale;
            imgH *= scale;

            var needCol = (imgW + 2 * pad) / pxPerColChar;
            var needRow = (imgH + 2 * pad) * 72.0 / 96.0;
            if (needCol > maxColChars || needRow > maxRowPts)
            {
                var lim = Math.Min(
                    maxColChars * pxPerColChar / (imgW + 2 * pad),
                    maxRowPts * 96.0 / 72.0 / (imgH + 2 * pad));
                imgW *= lim;
                imgH *= lim;
                needCol = (imgW + 2 * pad) / pxPerColChar;
                needRow = (imgH + 2 * pad) * 72.0 / 96.0;
            }

            if (ws.Column(xlCol).Width < needCol)
                ws.Column(xlCol).Width = needCol;
            if (ws.Row(xlRow).Height < needRow)
                ws.Row(xlRow).Height = needRow;

            var cellW = ws.Column(xlCol).Width * pxPerColChar;
            var cellH = ws.Row(xlRow).Height * 96.0 / 72.0;
            var finalW = Math.Max(1, (int)Math.Round(imgW));
            var finalH = Math.Max(1, (int)Math.Round(imgH));
            var ox = Math.Max(0, (int)Math.Round((cellW - finalW) / 2.0));
            var oy = Math.Max(0, (int)Math.Round((cellH - finalH) / 2.0));
            pic.MoveTo(cell, ox, oy, cell, ox + finalW, oy + finalH);
            return true;
        }
        catch { return false; }
    }

    private static XLPictureFormat DetectPictureFormat(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return XLPictureFormat.Jpeg;
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return XLPictureFormat.Png;
        if (bytes.Length >= 3 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return XLPictureFormat.Gif;
        if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D) return XLPictureFormat.Bmp;
        return XLPictureFormat.Unknown;
    }

    private static bool HasUrl(string? path) => !LegacyFileUrl.IsBlankOrPlaceholder(path);

    private static void Set(IXLWorksheet ws, int npoiRow, int npoiCol, string? value)
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        cell.Style.NumberFormat.Format = "@";
        cell.SetValue(value ?? "");
    }

    private static void SetText(IXLWorksheet ws, int npoiRow, int npoiCol, string? value)
        => Set(ws, npoiRow, npoiCol, value);

    /// <param name="numFmt">
    /// Excel 数字格式。勿用 <c>0.##</c>（整数常显示成「1000.」带尾点）；
    /// 计数用 <c>0</c>，重量体积用 <c>0.00</c>。
    /// </param>
    private static void SetNum(IXLWorksheet ws, int npoiRow, int npoiCol, double value, string numFmt = "0")
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        cell.Style.NumberFormat.Format = numFmt;
        cell.Value = value;
    }

    private static void SetFormula(
        IXLWorksheet ws, int npoiRow, int npoiCol, string formulaA1, double cachedValue, string numFmt = "0")
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        cell.Style.NumberFormat.Format = numFmt;
        // 先写入缓存值再设公式：ClosedXML 会保留公式，打开 Excel/LibreOffice 后重算
        cell.Value = cachedValue;
        cell.FormulaA1 = formulaA1.StartsWith('=') ? formulaA1 : "=" + formulaA1;
    }

    /// <summary>
    /// 报关明细：输入写数值，结果写公式（对齐 CustomsAndPackingCalc）。
    /// 隐藏辅助列：打包数/打托数/拼托标记/尺寸拆分。
    /// </summary>
    private static void WriteCustomsCalcRow(IXLWorksheet ws, int npoiRow, ZyqdCustomsAndPackingModel item)
    {
        var excelRow = npoiRow + 1;
        var dto = CustomsAndPackingService.ToLineDto(item, 0);
        var packing = ParsePositiveLocal(item.Packing_method, 1);
        var palletizing = ParsePositiveLocal(item.palletizing_method, 1);
        var hasTail = string.IsNullOrWhiteSpace(item.var2) ? 0 : 1;
        var mergeMember = CustomsAndPackingCalc.IsMergeMember(item.var8) ? 1 : 0;
        var shared = CustomsAndPackingCalc.NeedsSharedPalletSlot(dto) ? 1 : 0;
        var emit = ParseDoubleLocal(item.emit);

        // 辅助列（Excel 50+）：改数时公式依赖这些
        SetNum(ws, npoiRow, HelperPack, packing);
        SetNum(ws, npoiRow, HelperPalletizing, palletizing);
        SetNum(ws, npoiRow, HelperHasTail, hasTail);
        SetNum(ws, npoiRow, HelperMergeMember, mergeMember);
        SetNum(ws, npoiRow, HelperSharedSlot, shared);
        SetNum(ws, npoiRow, HelperEmit, emit);

        // 拼托成员：体积不算整托/尾托尺寸
        WriteSizeHelpers(ws, npoiRow, HelperPalletW,
            mergeMember == 1 ? null : item.Pallet_Size);
        WriteSizeHelpers(ws, npoiRow, HelperTailW,
            mergeMember == 1 ? null : item.var2);
        WriteSizeHelpers(ws, npoiRow, HelperCartonW, item.Carton_Size);

        // 可见输入
        SetNum(ws, npoiRow, 13, item.qty);                    // N 数量
        SetNum(ws, npoiRow, 23, item.Net_weight, "0.00");     // X 净重
        SetNum(ws, npoiRow, 24, item.gross_weight, "0.00");   // Y 毛重

        var n = $"N{excelRow}";
        var x = $"X{excelRow}";
        var y = $"Y{excelRow}";
        var p = $"P{excelRow}";
        var t = $"T{excelRow}";
        var w = $"W{excelRow}";
        var ax = ColLetter(HelperPack + 1) + excelRow;
        var ay = ColLetter(HelperPalletizing + 1) + excelRow;
        var az = ColLetter(HelperHasTail + 1) + excelRow;
        var ba = ColLetter(HelperMergeMember + 1) + excelRow;
        var bb = ColLetter(HelperSharedSlot + 1) + excelRow;
        var bc = ColLetter(HelperEmit + 1) + excelRow;
        var bd = ColLetter(HelperPalletW + 1) + excelRow;
        var be = ColLetter(HelperPalletH + 1) + excelRow;
        var bf = ColLetter(HelperPalletD + 1) + excelRow;
        var bg = ColLetter(HelperTailW + 1) + excelRow;
        var bh = ColLetter(HelperTailH + 1) + excelRow;
        var bi = ColLetter(HelperTailD + 1) + excelRow;
        var bj = ColLetter(HelperCartonW + 1) + excelRow;
        var bk = ColLetter(HelperCartonH + 1) + excelRow;
        var bl = ColLetter(HelperCartonD + 1) + excelRow;

        // P 箱数 = CEILING(数量/打包)
        SetFormula(ws, npoiRow, 15,
            $"IFERROR(CEILING({n}/{ax},1),0)", item.box_qty);
        // T 整托 = FLOOR(箱数/打托)
        SetFormula(ws, npoiRow, 19,
            $"IFERROR(FLOOR({p}/{ay},1),0)", item.Pallet_qty);
        // U 余箱
        SetFormula(ws, npoiRow, 20,
            $"IFERROR(MOD({p},{ay}),0)", ParseDoubleLocal(item.var3));
        // Q 显示托数（拼托成员=0；否则整托+尾托+拼托主补位）
        SetFormula(ws, npoiRow, 16,
            $"IF({ba}=1,0,IF({t}+{az}>0,{t}+{az},IF({bb}=1,1,0)))",
            ExportDisplayPalletQty(item));
        // W 托重 = 整托*15 +（非拼托成员且有尾托/补位时 +15）
        SetFormula(ws, npoiRow, 22,
            $"IF({ba}=1,{t}*{CustomsAndPackingCalc.KgPerPallet},{t}*{CustomsAndPackingCalc.KgPerPallet}+IF(OR({az}=1,{bb}=1),{CustomsAndPackingCalc.KgPerPallet},0))",
            item.Pallet_weight, "0.00");
        // Z 总净重
        SetFormula(ws, npoiRow, 25,
            $"IFERROR(ROUND({n}/{ax}*{x},2),0)", item.total_Net_weight, "0.00");
        // AA 总毛重
        SetFormula(ws, npoiRow, 26,
            $"IFERROR(ROUND({n}/{ax}*{y}+{w},2),0)", item.total_gross_weight, "0.00");
        // AB 体积（mm³→m³，向上取两位）；整托=0 且拼托主补位时按 1 托计
        SetFormula(ws, npoiRow, 27,
            $"IFERROR(CEILING((IF({t}>0,{t},IF({bb}=1,1,0))*{bd}*{be}*{bf}+IF({az}=1,{bg}*{bh}*{bi},0)+{bc}*{bj}*{bk}*{bl})/1000000000,0.01),0)",
            item.volume, "0.00");
    }

    private static void WriteCustomsTotalFormulas(
        IXLWorksheet ws,
        int totalNpoiRow,
        int firstExcelRow,
        int lastExcelRow,
        ZyqdCustomsAndPackingModel main)
    {
        SetFormula(ws, totalNpoiRow, 13, $"SUM(N{firstExcelRow}:N{lastExcelRow})", main.all_qty);
        SetFormula(ws, totalNpoiRow, 15, $"SUM(P{firstExcelRow}:P{lastExcelRow})", main.all_box_qty);
        SetFormula(ws, totalNpoiRow, 16, $"SUM(Q{firstExcelRow}:Q{lastExcelRow})", main.all_Pallet_qty);
        SetFormula(ws, totalNpoiRow, 25, $"SUM(Z{firstExcelRow}:Z{lastExcelRow})", main.all_total_Net_weight, "0.00");
        SetFormula(ws, totalNpoiRow, 26, $"SUM(AA{firstExcelRow}:AA{lastExcelRow})", main.all_total_gross_weight, "0.00");
        SetFormula(ws, totalNpoiRow, 27, $"SUM(AB{firstExcelRow}:AB{lastExcelRow})", main.all_volume, "0.00");
    }

    private static void HideCustomsHelperColumns(IXLWorksheet ws)
    {
        for (var c = HelperPack + 1; c <= HelperCartonD + 1; c++)
            ws.Column(c).Hide();
    }

    private static void WriteSizeHelpers(IXLWorksheet ws, int npoiRow, int startNpoiCol, string? size)
    {
        // 尺寸用常规小数，避免 0.## 整数尾点
        const string fmt = "0.######";
        if (TryParseSizeLocal(size, out var a, out var b, out var c))
        {
            SetNum(ws, npoiRow, startNpoiCol, a, fmt);
            SetNum(ws, npoiRow, startNpoiCol + 1, b, fmt);
            SetNum(ws, npoiRow, startNpoiCol + 2, c, fmt);
        }
        else
        {
            SetNum(ws, npoiRow, startNpoiCol, 0, fmt);
            SetNum(ws, npoiRow, startNpoiCol + 1, 0, fmt);
            SetNum(ws, npoiRow, startNpoiCol + 2, 0, fmt);
        }
    }

    // 隐藏辅助列（0-based npoi）：打包、打托、尾托标记、拼托成员、补位托、散发、尺寸
    private const int HelperPack = 49;
    private const int HelperPalletizing = 50;
    private const int HelperHasTail = 51;
    private const int HelperMergeMember = 52;
    private const int HelperSharedSlot = 53;
    private const int HelperEmit = 54;
    private const int HelperPalletW = 55;
    private const int HelperPalletH = 56;
    private const int HelperPalletD = 57;
    private const int HelperTailW = 58;
    private const int HelperTailH = 59;
    private const int HelperTailD = 60;
    private const int HelperCartonW = 61;
    private const int HelperCartonH = 62;
    private const int HelperCartonD = 63;

    private static string ColLetter(int excelCol1Based)
    {
        var n = excelCol1Based;
        var s = "";
        while (n > 0)
        {
            n--;
            s = (char)('A' + n % 26) + s;
            n /= 26;
        }
        return s;
    }

    private static double ParsePositiveLocal(string? text, double fallback)
    {
        if (!double.TryParse((text ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var n)
            && !double.TryParse((text ?? "").Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out n))
            return fallback;
        return n > 0 ? n : fallback;
    }

    private static double ParseDoubleLocal(string? text)
    {
        if (double.TryParse((text ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
            return n;
        if (double.TryParse((text ?? "").Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out n))
            return n;
        return 0;
    }

    private static bool TryParseSizeLocal(string? size, out double a, out double b, out double c)
    {
        a = b = c = 0;
        var parts = (size ?? "").Split('*', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;
        if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out a)
            && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out b)
            && double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out c))
            return true;
        return double.TryParse(parts[0], NumberStyles.Any, CultureInfo.CurrentCulture, out a)
               && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.CurrentCulture, out b)
               && double.TryParse(parts[2], NumberStyles.Any, CultureInfo.CurrentCulture, out c);
    }

    private static string Num(double n)
        => n.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Sanitize(string? s)
    {
        var t = (s ?? "").Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            t = t.Replace(c, '_');
        return t;
    }
}
