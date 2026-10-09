using System.Globalization;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels.Quotation;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 报价单 Excel 填充（对齐老系统 SetQuotationPDF），再交给 LibreOffice 转 PDF。
/// 新系统明细为单档数量/单价；版式仍按旧模板多行规格写入。
/// </summary>
public class QuotationExcelService
{
    private const string LegacyHttpClient = "LegacyFiles";
    private const int RowsPerPage = 50;
    private const int DetailStartNpoi = 19;
    private const int PictureRows = 6;

    private static readonly string[] TypeOrder =
    [
        QuotationBaseTypes.Motor,
        QuotationBaseTypes.Remote,
        QuotationBaseTypes.CB,
        QuotationBaseTypes.PR,
        QuotationBaseTypes.Accessory,
    ];

    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _imageBase;

    public QuotationExcelService(
        IWebHostEnvironment env,
        IHttpClientFactory httpFactory,
        IOptions<LegacyFilesOptions> legacyFiles)
    {
        _env = env;
        _httpFactory = httpFactory;
        _imageBase = legacyFiles.Value.ResolvedBaseUrl();
    }

    public async Task<byte[]> BuildAsync(
        ZyqdQuotationMainModel main,
        IReadOnlyList<QuotationDetailLineDto> details,
        string lang,
        string? sellerMail = null,
        string? sellerTel = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(main);
        lang = NormalizeLang(lang);

        var path = ResolveTemplatePath(lang);
        var images = await DownloadImagesAsync(main, details, ct);

        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet(1);
        FillHeader(ws, main, lang, sellerMail, sellerTel);

        var ordered = details
            .Where(d => !string.IsNullOrWhiteSpace(d.Part_No))
            .OrderBy(d => TypeIndex(d.ItemType))
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.Id)
            .ToList();

        var ctx = new WriteContext(ws, images);
        foreach (var item in ordered)
            WriteItem(ctx, item, lang, main.currencyUnit);

        PlaceSignature(ctx, main.signature_url);
        ApplyPrintLayout(ws);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public static string DownloadExcelName(ZyqdQuotationMainModel main, string lang)
    {
        var no = Sanitize(main.Quota_No);
        if (no.Length == 0) no = "报价单";
        return $"{no}-{NormalizeLang(lang)}.xlsx";
    }

    public static string DownloadPdfName(ZyqdQuotationMainModel main, string lang)
    {
        var name = DownloadExcelName(main, lang);
        return name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? name[..^5] + ".pdf"
            : name + ".pdf";
    }

    public static string NormalizeLang(string? lang)
    {
        var t = (lang ?? "").Trim().ToUpperInvariant();
        return t is "EN" or "DE" or "ZH" ? t : "ZH";
    }

    private string ResolveTemplatePath(string lang)
    {
        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var dir = Path.Combine(webRoot, "Templates");
        var preferred = lang == "ZH" ? "报价单中文模板.xlsx" : "报价单英文模板.xlsx";
        var path = Path.Combine(dir, preferred);
        if (File.Exists(path))
            return path;

        if (Directory.Exists(dir))
        {
            var key = lang == "ZH" ? "中文" : "英文";
            var hit = Directory.GetFiles(dir, "*.xlsx")
                .FirstOrDefault(f => Path.GetFileName(f).Contains("报价", StringComparison.OrdinalIgnoreCase)
                                     && Path.GetFileName(f).Contains(key, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
                return hit;
        }

        throw new FileNotFoundException("未找到报价单模板，请确认文件在 wwwroot/Templates/" + preferred);
    }

    private void FillHeader(
        IXLWorksheet ws, ZyqdQuotationMainModel main, string lang, string? sellerMail, string? sellerTel)
    {
        var zh = lang == "ZH";
        var mail = (sellerMail ?? "").Trim();
        var tel = (sellerTel ?? "").Trim();

        if (lang == "DE")
        {
            Set(ws, 2, 0, "JIECANG Europe GmbH");
            Set(ws, 3, 0, "Heerdterbuschstr. 10，Halle 10");
            Set(ws, 4, 0, "41460 Neuss");
        }

        Set(ws, 5, 0, zh ? "邮箱:" + mail : "Mail:" + mail);
        Set(ws, 6, 0, zh ? "联系电话:" + tel : "Tel:" + tel);
        Set(ws, 8, 17, main.Quota_No);
        Set(ws, 9, 17, main.DATE);

        Set(ws, 11, 0, (zh ? "名称:" : "Client:") + (main.ClientName ?? ""));
        Set(ws, 12, 0, (zh ? "地址:" : "Address:") + (main.Address ?? ""));
        Set(ws, 13, 0, "Tel:" + (main.Tel ?? ""));
        Set(ws, 14, 0, (zh ? "邮箱:" : "Mail:") + (main.Mail ?? ""));
        Set(ws, 15, 0, (zh ? "联系人:" : "Attn:") + (main.Attn ?? ""));

        if (lang == "EN")
        {
            var unit = (main.currencyUnit ?? "").Trim();
            var rate = main.rate ?? "";
            var pair = unit switch
            {
                "€" => "EUR/CNY",
                "CAD" => "CAD/CNY",
                _ => "USD/CNY",
            };
            Set(ws, 25, 2, $"{pair} exchange rate :{rate}, the price will be kept if the flutuation change within ±3%");
        }
    }

    private void WriteItem(WriteContext ctx, QuotationDetailLineDto item, string lang, string? currencyUnit)
    {
        var type = QuotationBaseTypes.Normalize(item.ItemType);
        switch (type)
        {
            case QuotationBaseTypes.Motor:
                WriteMotor(ctx, item, lang, currencyUnit);
                break;
            case QuotationBaseTypes.Remote:
                WriteRemote(ctx, item, lang, currencyUnit);
                break;
            case QuotationBaseTypes.CB:
                WriteCb(ctx, item, lang, currencyUnit);
                break;
            case QuotationBaseTypes.PR:
                WritePr(ctx, item, lang, currencyUnit);
                break;
            default:
                WriteAccessory(ctx, item, lang, currencyUnit);
                break;
        }
    }

    private void WriteMotor(WriteContext ctx, QuotationDetailLineDto item, string lang, string? currencyUnit)
    {
        var L = Labels.For(lang);
        var pictureStart = ctx.Row + 1;
        BeginProduct(ctx, item, EstimateMotorRows(item, L), out var seq);

        WriteSpecRow(ctx, seq, item.Part_No, L.RatedTorque + item.Rated_torque, item, currencyUnit, priceOnThisRow: true);
        TryAddPicture(ctx, pictureStart, item.Quotation_logo);
        WriteSpecRow(ctx, null, null, L.RatedSpeed + item.Rated_speed);

        var power = L.PowerSupply + item.Power_supply;
        var powerLines = SplitDisplay(power, 50);
        if (powerLines.Length >= 2)
        {
            WriteSpecRow(ctx, null, null, powerLines[0]);
            WriteSpecRow(ctx, null, null, powerLines[1]);
        }
        else
        {
            WriteSpecRow(ctx, null, null, power);
        }

        WriteSpecRow(ctx, null, null, L.RatedPower + item.Rated_Power);
        WriteSpecRow(ctx, null, null, L.RatedCurrent + item.Rated_Current);
        WriteSpecRow(ctx, null, null, L.NoiseDba + item.Noise_dBA);
        WriteSpecRow(ctx, null, null, L.MotorLength + item.Motor_Length);
        WriteSpecRow(ctx, null, null, L.TubeDiameter + item.Tube_diameter);

        if (!string.IsNullOrWhiteSpace(item.Battery_Capacity))
            WriteSpecRow(ctx, null, null, L.BatteryCapacity + item.Battery_Capacity);
        if (!string.IsNullOrWhiteSpace(item.Exclude_text))
            WriteSpecRow(ctx, null, null, L.Exclude + item.Exclude_text);
        if (!string.IsNullOrWhiteSpace(item.Note))
            WriteSpecRow(ctx, null, null, L.Note + item.Note);

        EnsurePictureSpace(ctx, pictureStart, item.Quotation_logo);
        WriteBlank(ctx);
    }

    private void WriteRemote(WriteContext ctx, QuotationDetailLineDto item, string lang, string? currencyUnit)
    {
        var L = Labels.For(lang);
        var pictureStart = ctx.Row + 1;
        BeginProduct(ctx, item, EstimateRemoteRows(item, L), out var seq);

        var remote = L.RemoteType + item.Remote_controller_type;
        var remoteLines = SplitDisplay(remote, 50);
        if (remoteLines.Length >= 2)
        {
            WriteSpecRow(ctx, seq, item.Part_No, remoteLines[0], item, currencyUnit, priceOnThisRow: true);
            TryAddPicture(ctx, pictureStart, item.Quotation_logo);
            WriteSpecRow(ctx, null, null, remoteLines[1]);
        }
        else
        {
            WriteSpecRow(ctx, seq, item.Part_No, remote, item, currencyUnit, priceOnThisRow: true);
            TryAddPicture(ctx, pictureStart, item.Quotation_logo);
        }

        WriteSpecRow(ctx, null, null, L.BatteryType + item.Battery_Type);
        WriteSpecRow(ctx, null, null, L.RadioFrequency + item.Radio_Frequency);
        WriteSpecRow(ctx, null, null, L.WorkingTemperature + item.Working_Temperature);
        WriteSpecRow(ctx, null, null, L.NoOfChannels + item.NoOfChannels);
        if (!string.IsNullOrWhiteSpace(item.Note))
            WriteSpecRow(ctx, null, null, L.Note + item.Note);

        EnsurePictureSpace(ctx, pictureStart, item.Quotation_logo);
        WriteBlank(ctx);
    }

    private void WriteCb(WriteContext ctx, QuotationDetailLineDto item, string lang, string? currencyUnit)
    {
        var L = Labels.For(lang);
        var pictureStart = ctx.Row + 1;
        BeginProduct(ctx, item, EstimateCbRows(item), out var seq);

        WriteSpecRow(ctx, seq, item.Part_No, L.RatedVoltage + item.Rated_Voltage, item, currencyUnit, priceOnThisRow: true);
        TryAddPicture(ctx, pictureStart, item.Quotation_logo);
        WriteSpecRow(ctx, null, null, L.Size + item.Size);
        WriteSpecRow(ctx, null, null, L.RatedPowerStr + item.Rated_Power);
        WriteSpecRow(ctx, null, null, L.NoOfActuator + item.No_Of_Actuator);
        WriteSpecRow(ctx, null, null, L.OutputControl + item.Output_Control);
        WriteSpecRow(ctx, null, null, L.Sensor + item.Sensor);
        WriteSpecRow(ctx, null, null, L.ProtectionDegree + item.Protection_Degree);
        if (!string.IsNullOrWhiteSpace(item.Note))
            WriteSpecRow(ctx, null, null, L.Note + item.Note);

        EnsurePictureSpace(ctx, pictureStart, item.Quotation_logo);
        WriteBlank(ctx);
    }

    private void WritePr(WriteContext ctx, QuotationDetailLineDto item, string lang, string? currencyUnit)
    {
        var L = Labels.For(lang);
        var pictureStart = ctx.Row + 1;
        BeginProduct(ctx, item, EstimatePrRows(item), out var seq);

        WriteSpecRow(ctx, seq, item.Part_No, L.MaxLoading + item.Max_Loading, item, currencyUnit, priceOnThisRow: true);
        WriteSpecRow(ctx, null, null, L.MaxStroke + item.Max_Stroke);
        WriteSpecRow(ctx, null, null, L.ProtectionDegree2 + item.Protection_Degree2);
        WriteSpecRow(ctx, null, null, L.Noise + item.Noise);
        if (!string.IsNullOrWhiteSpace(item.Note))
            WriteSpecRow(ctx, null, null, L.Note + item.Note);

        WriteBlank(ctx);
        EnsurePictureSpace(ctx, pictureStart, item.Quotation_logo);
        TryAddPicture(ctx, pictureStart, item.Quotation_logo);
    }

    private void WriteAccessory(WriteContext ctx, QuotationDetailLineDto item, string lang, string? currencyUnit)
    {
        var L = Labels.For(lang);
        var pictureStart = ctx.Row + 1;
        BeginProduct(ctx, item, EstimateAccessoryRows(item, L), out var seq);

        WriteSpecRow(ctx, seq, item.Part_No, L.Adaptor + item.adaptor, item, currencyUnit, priceOnThisRow: true);

        if (!string.IsNullOrWhiteSpace(item.Note))
        {
            var note = L.Note + item.Note;
            var lines = SplitDisplay(note, 50);
            foreach (var line in lines)
                WriteSpecRow(ctx, null, null, line);
        }

        WriteBlank(ctx);
        EnsurePictureSpace(ctx, pictureStart, item.Quotation_logo);
        TryAddPicture(ctx, pictureStart, item.Quotation_logo);
    }

    private static void BeginProduct(WriteContext ctx, QuotationDetailLineDto item, int itemRows, out string seq)
    {
        ctx.InsertPageBreakIfNeeded(itemRows);
        ctx.ProductCount++;
        seq = ctx.ProductCount.ToString("00");
    }

    private static void WriteSpecRow(
        WriteContext ctx,
        string? seq,
        string? partNo,
        string? spec,
        QuotationDetailLineDto? item = null,
        string? currencyUnit = null,
        bool priceOnThisRow = false)
    {
        ctx.InsertBlankAfter();
        if (!string.IsNullOrWhiteSpace(seq))
            Set(ctx.Ws, ctx.Row, 0, seq);
        if (!string.IsNullOrWhiteSpace(partNo))
            Set(ctx.Ws, ctx.Row, 1, partNo);
        if (!string.IsNullOrWhiteSpace(spec))
            Set(ctx.Ws, ctx.Row, 7, spec);

        if (priceOnThisRow && item is not null && (item.Qty ?? 0) > 0)
            WritePrice(ctx.Ws, ctx.Row, item, currencyUnit);

        ctx.Advance();
    }

    private static void WriteBlank(WriteContext ctx)
    {
        ctx.InsertBlankAfter();
        ctx.Advance();
    }

    private static void WritePrice(IXLWorksheet ws, int npoiRow, QuotationDetailLineDto item, string? currencyUnit)
    {
        var unit = (currencyUnit ?? "").Trim();
        Set(ws, npoiRow, 14, FmtQty(item.Qty) + " PCS");
        Set(ws, npoiRow, 16, unit + FormatMoney(item.Price));
        Set(ws, npoiRow, 18, FormatDiscount(item.Discount) + "%");
        Set(ws, npoiRow, 20, unit + FormatMoney(item.Amount));
    }

    private void PlaceSignature(WriteContext ctx, string? signatureUrl)
    {
        ctx.InsertPageBreakIfNeeded(7);
        var npoiRow = ctx.Row + 12;
        TryAddPicture(ctx, npoiRow, signatureUrl, width: 160, height: 90);
    }

    private void TryAddPicture(WriteContext ctx, int npoiRow, string? path, int width = 140, int height = 140)
    {
        var url = LegacyFileUrl.Resolve(path, _imageBase);
        if (url.Length == 0 || !ctx.Images.TryGetValue(url, out var bytes))
            return;

        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            var format = DetectPictureFormat(bytes);
            var name = $"qimg_{ctx.PicSeq++}";
            var pic = format == XLPictureFormat.Unknown
                ? ctx.Ws.AddPicture(ms, name)
                : ctx.Ws.AddPicture(ms, format, name);
            var cell = ctx.Ws.Cell(npoiRow + 1, 2); // npoi col 1
            pic.MoveTo(cell);
            pic.WithSize(width, height);
        }
        catch
        {
            // 单张图失败不影响整份导出
        }
    }

    private static void EnsurePictureSpace(WriteContext ctx, int pictureStartNpoi, string? logo)
    {
        if (LegacyFileUrl.IsBlankOrPlaceholder(logo))
            return;
        var need = (pictureStartNpoi + PictureRows) - ctx.Row;
        while (need-- > 0)
            WriteBlank(ctx);
    }

    private static int EstimateMotorRows(QuotationDetailLineDto item, Labels L)
    {
        var text = 8;
        if (!string.IsNullOrWhiteSpace(item.Battery_Capacity)) text++;
        if (!string.IsNullOrWhiteSpace(item.Exclude_text)) text++;
        if (!string.IsNullOrWhiteSpace(item.Note)) text++;
        if (SplitDisplay(L.PowerSupply + item.Power_supply, 50).Length > 1) text++;
        var pic = LegacyFileUrl.IsBlankOrPlaceholder(item.Quotation_logo) ? 0 : PictureRows;
        return Math.Max(text, pic) + 1;
    }

    private static int EstimateRemoteRows(QuotationDetailLineDto item, Labels L)
    {
        var text = 5;
        if (!string.IsNullOrWhiteSpace(item.Note)) text++;
        if (SplitDisplay(L.RemoteType + item.Remote_controller_type, 50).Length > 1) text++;
        var pic = LegacyFileUrl.IsBlankOrPlaceholder(item.Quotation_logo) ? 0 : PictureRows;
        return Math.Max(text, pic) + 1;
    }

    private static int EstimateCbRows(QuotationDetailLineDto item)
    {
        var text = 7 + (string.IsNullOrWhiteSpace(item.Note) ? 0 : 1);
        var pic = LegacyFileUrl.IsBlankOrPlaceholder(item.Quotation_logo) ? 0 : PictureRows;
        return Math.Max(text, pic) + 1;
    }

    private static int EstimatePrRows(QuotationDetailLineDto item)
    {
        var text = 4 + (string.IsNullOrWhiteSpace(item.Note) ? 0 : 1);
        var pic = LegacyFileUrl.IsBlankOrPlaceholder(item.Quotation_logo) ? 0 : PictureRows;
        return Math.Max(text, pic) + 1;
    }

    private static int EstimateAccessoryRows(QuotationDetailLineDto item, Labels L)
    {
        var text = 1;
        if (!string.IsNullOrWhiteSpace(item.Note))
            text += Math.Max(1, SplitDisplay(L.Note + item.Note, 50).Length);
        var pic = LegacyFileUrl.IsBlankOrPlaceholder(item.Quotation_logo) ? 0 : PictureRows;
        return Math.Max(text, pic) + 1;
    }

    private static void ApplyPrintLayout(IXLWorksheet ws)
    {
        var ps = ws.PageSetup;
        ps.PaperSize = XLPaperSize.A4Paper;
        ps.PageOrientation = XLPageOrientation.Portrait;
        ps.PagesWide = 1;
        ps.PagesTall = 0;
        ps.Margins.Top = 0.7;
        ps.Margins.Bottom = 0.7;
        ps.Margins.Left = 0.4;
        ps.Margins.Right = 0.4;
        ps.Margins.Header = 0.3;
        ps.Margins.Footer = 0.3;
    }

    private async Task<Dictionary<string, byte[]>> DownloadImagesAsync(
        ZyqdQuotationMainModel main,
        IReadOnlyList<QuotationDetailLineDto> details,
        CancellationToken ct)
    {
        var urls = details.Select(d => d.Quotation_logo)
            .Append(main.signature_url)
            .Select(p => LegacyFileUrl.Resolve(p, _imageBase))
            .Where(u => u.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        if (urls.Count == 0) return result;

        var http = _httpFactory.CreateClient(LegacyHttpClient);
        using var gate = new SemaphoreSlim(6);
        var bag = new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        await Task.WhenAll(urls.Select(async url =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var bytes = await TryDownloadImageAsync(http, url, ct);
                if (bytes is { Length: > 0 })
                    bag[url] = bytes;
            }
            finally
            {
                gate.Release();
            }
        }));

        foreach (var kv in bag)
            result[kv.Key] = kv.Value;
        return result;
    }

    private static async Task<byte[]?> TryDownloadImageAsync(HttpClient http, string url, CancellationToken ct)
    {
        try
        {
            using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode) return null;
            if (resp.Content.Headers.ContentLength is > 8 * 1024 * 1024) return null;
            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            return LooksLikeImage(bytes) ? bytes : null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static bool LooksLikeImage(byte[] bytes)
    {
        if (bytes.Length < 12) return false;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8) return true;
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return true;
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return true;
        if (bytes[0] == 0x42 && bytes[1] == 0x4D) return true;
        return false;
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

    /// <summary>NPOI 0-based → ClosedXML 1-based。</summary>
    private static void Set(IXLWorksheet ws, int npoiRow, int npoiCol, string? value)
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        cell.Style.NumberFormat.Format = "@";
        cell.SetValue(value ?? "");
    }

    private static string FormatMoney(decimal? value)
    {
        var n = value ?? 0m;
        return n.ToString("N2", CultureInfo.GetCultureInfo("en-US"));
    }

    private static string FormatDiscount(decimal? value)
        => (value ?? 0m).ToString("0.##", CultureInfo.InvariantCulture);

    private static string FmtQty(decimal? value)
        => (value ?? 0m).ToString("0.##", CultureInfo.InvariantCulture);

    private static int TypeIndex(string? type)
    {
        var t = QuotationBaseTypes.Normalize(type);
        var i = Array.IndexOf(TypeOrder, t);
        return i < 0 ? TypeOrder.Length : i;
    }

    private static string Sanitize(string? s)
    {
        var t = (s ?? "").Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            t = t.Replace(c, '_');
        return t;
    }

    private static string[] SplitDisplay(string? input, int maxDisplayWidth)
    {
        if (string.IsNullOrEmpty(input))
            return [];
        if (GetDisplayWidth(input) <= maxDisplayWidth)
            return [input];

        var result = new List<string>();
        var remaining = input;
        while (remaining.Length > 0)
        {
            var width = 0;
            var split = 0;
            for (var i = 0; i < remaining.Length; i++)
            {
                width += remaining[i] > 127 ? 2 : 1;
                if (width > maxDisplayWidth) break;
                split = i + 1;
            }
            if (split <= 0) split = 1;
            result.Add(remaining[..split]);
            remaining = remaining[split..];
        }
        return result.ToArray();
    }

    private static int GetDisplayWidth(string s)
    {
        var w = 0;
        foreach (var ch in s)
            w += ch > 127 ? 2 : 1;
        return w;
    }

    private sealed class WriteContext
    {
        public WriteContext(IXLWorksheet ws, Dictionary<string, byte[]> images)
        {
            Ws = ws;
            Images = images;
            Row = DetailStartNpoi;
            Remaining = RowsPerPage - DetailStartNpoi;
        }

        public IXLWorksheet Ws { get; }
        public Dictionary<string, byte[]> Images { get; }
        public int Row { get; private set; }
        public int Remaining { get; private set; }
        public int ProductCount { get; set; }
        public int PicSeq { get; set; }

        public void InsertBlankAfter()
        {
            var excelRow = Row + 1;
            var src = Ws.Row(excelRow);
            src.InsertRowsBelow(1);
            var dst = Ws.Row(excelRow + 1);
            dst.Height = src.Height;
            for (var c = 1; c <= 22; c++)
                dst.Cell(c).Style = src.Cell(c).Style;
        }

        public void Advance()
        {
            Row++;
            Remaining--;
        }

        public void InsertPageBreakIfNeeded(int itemRows)
        {
            if (itemRows > Remaining && Remaining > 0)
            {
                Ws.PageSetup.AddHorizontalPageBreak(Row + 1);
                Remaining = RowsPerPage;
            }
        }
    }

    private readonly struct Labels
    {
        public required string RatedTorque { get; init; }
        public required string RatedSpeed { get; init; }
        public required string PowerSupply { get; init; }
        public required string RatedPower { get; init; }
        public required string RatedCurrent { get; init; }
        public required string NoiseDba { get; init; }
        public required string MotorLength { get; init; }
        public required string TubeDiameter { get; init; }
        public required string BatteryCapacity { get; init; }
        public required string Exclude { get; init; }
        public required string Note { get; init; }
        public required string RemoteType { get; init; }
        public required string BatteryType { get; init; }
        public required string RadioFrequency { get; init; }
        public required string WorkingTemperature { get; init; }
        public required string NoOfChannels { get; init; }
        public required string Adaptor { get; init; }
        public required string RatedVoltage { get; init; }
        public required string Size { get; init; }
        public required string RatedPowerStr { get; init; }
        public required string NoOfActuator { get; init; }
        public required string OutputControl { get; init; }
        public required string Sensor { get; init; }
        public required string ProtectionDegree { get; init; }
        public required string MaxLoading { get; init; }
        public required string MaxStroke { get; init; }
        public required string ProtectionDegree2 { get; init; }
        public required string Noise { get; init; }

        public static Labels For(string lang)
        {
            var zh = lang == "ZH";
            return new Labels
            {
                RatedTorque = zh ? "扭矩:" : "Rated torque:",
                RatedSpeed = zh ? "转速:" : "Rated speed:",
                PowerSupply = zh ? "供电方式:" : "Power supply:",
                RatedPower = zh ? "额定电压:" : "Rated Power:",
                RatedCurrent = zh ? "额定电流:" : "Rated Current:",
                NoiseDba = zh ? "噪音值:" : "Noise dBA:",
                MotorLength = zh ? "电机长度:" : "Motor Length:",
                TubeDiameter = zh ? "外管尺寸:" : "Tube diameter:",
                BatteryCapacity = zh ? "电池类型:" : "Battery Capacity:",
                Exclude = zh ? "价格排除:" : "Exclude:",
                Note = zh ? "备注:" : "Note:",
                RemoteType = zh ? "遥控器型号:" : "Remote controller type:",
                BatteryType = zh ? "电池型号:" : "Battery Type:",
                RadioFrequency = zh ? "电机频率:" : "Radio Frequency:",
                WorkingTemperature = zh ? "工作温度:" : "Working Temperature:",
                NoOfChannels = zh ? "通道数:" : "No Of Channels:",
                Adaptor = zh ? "配件:" : "adaptor:",
                RatedVoltage = zh ? "额定电压:" : "Rated Voltage:",
                Size = zh ? "尺寸:" : "Size:",
                RatedPowerStr = zh ? "额定功率:" : "Rated Power:",
                NoOfActuator = zh ? "执行器数量:" : "No Of Actuator:",
                OutputControl = zh ? "输出控制:" : "Output Control:",
                Sensor = zh ? "传感器:" : "Sensor:",
                ProtectionDegree = zh ? "防护等级:" : "Protection Degree:",
                MaxLoading = zh ? "最大负载:" : "Max Loading:",
                MaxStroke = zh ? "最大行程:" : "Max Stroke:",
                ProtectionDegree2 = zh ? "防护等级:" : "Protection Degree:",
                Noise = zh ? "噪音:" : "Noise:",
            };
        }
    }
}
