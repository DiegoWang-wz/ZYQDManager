using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels.Quotation;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 报价基础数据导入/导出（对齐老系统两套模板，按 Type 写入拆分后的五张表）。
/// </summary>
public class QuotationBaseExcelService
{
    public const string MotorTemplateFileName = "遮阳报价基础数据导入模板.xlsx";
    public const string ControllerTemplateFileName = "遮阳报价基础数据导入模板推杆类.xlsx";

    private const string LegacyHttpClient = "LegacyFiles";
    /// <summary>
    /// 官方模板：第1行中文分组、第2行英文字段名，数据从第3行起（对齐老 NPOI 1+ii）。
    /// </summary>
    private const int DataStartRow = 3;

    // 电机/遥控器/配件模板列（与老 NPOI 导出 0 基列 +1 一致）
    private const int MPartNo = 1;
    private const int MType = 2;
    private const int MTorque = 3;
    private const int MSpeed = 4;
    private const int MPowerSupply = 5;
    private const int MRatedPower = 6;
    private const int MCurrent = 7;
    private const int MNoise = 8;
    private const int MLength = 9;
    private const int MTube = 10;
    private const int MBatteryCap = 11;
    private const int MExclude = 12;
    private const int MRemoteType = 13;
    private const int MBatteryType = 14;
    private const int MRadio = 15;
    private const int MTemp = 16;
    private const int MChannels = 17;
    private const int MAdaptor = 18;
    private const int MRemark = 19;
    private const int MLogo = 20;

    // 控制盒/推杆模板
    private const int CPartNo = 1;
    private const int CType = 2;
    private const int CVoltage = 3;
    private const int CSize = 4;
    private const int CRatedPower = 5;
    private const int CActuator = 6;
    private const int COutput = 7;
    private const int CSensor = 8;
    private const int CProtect = 9;
    private const int CMaxLoad = 10;
    private const int CMaxStroke = 11;
    private const int CProtect2 = 12;
    private const int CNoise = 13;
    private const int CAdaptor = 14;
    private const int CRemark = 15;
    private const int CLogo = 16;

    private static readonly string[] MotorHeaders =
    [
        "Part_No", "Type", "Rated_torque", "Rated_speed", "Power_supply", "Rated_Power",
        "Rated_Current", "Noise_dBA", "Motor_Length", "Tube_diameter", "Battery_Capacity",
        "Exclude_text", "Remote_controller_type", "Battery_Type", "Radio_Frequency",
        "Working_Temperature", "NoOfChannels", "adaptor", "Remark", "Quotation_logo"
    ];

    private static readonly string[] ControllerHeaders =
    [
        "Part_No", "Type", "Rated_Voltage", "Size", "Rated_Power", "No_Of_Actuator",
        "Output_Control", "Sensor", "Protection_Degree", "Max_Loading", "Max_Stroke",
        "Protection_Degree2", "Noise", "adaptor", "Remark", "Quotation_logo"
    ];

    private readonly QuotationBaseService _base;
    private readonly CotractBaseService _cotractBase;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _imageBase;

    public QuotationBaseExcelService(
        QuotationBaseService @base,
        CotractBaseService cotractBase,
        IWebHostEnvironment env,
        IHttpClientFactory httpFactory,
        IOptions<LegacyFilesOptions> legacyFiles)
    {
        _base = @base;
        _cotractBase = cotractBase;
        _env = env;
        _httpFactory = httpFactory;
        _imageBase = legacyFiles.Value.ResolvedBaseUrl();
    }

    public static bool IsControllerGroup(string? itemType)
    {
        var t = QuotationBaseTypes.Normalize(itemType);
        return t is QuotationBaseTypes.CB or QuotationBaseTypes.PR;
    }

    public static string TemplateFileNameFor(string? itemType)
        => IsControllerGroup(itemType) ? ControllerTemplateFileName : MotorTemplateFileName;

    public static string TypeLabel(string? itemType) => QuotationBaseTypes.Normalize(itemType) switch
    {
        QuotationBaseTypes.Remote => "遥控器",
        QuotationBaseTypes.Accessory => "配件",
        QuotationBaseTypes.CB => "控制盒",
        QuotationBaseTypes.PR => "推杆",
        _ => "电机",
    };

    public Task<byte[]> GetTemplateBytesAsync(string? itemType, CancellationToken ct = default)
    {
        var path = EnsureTemplate(IsControllerGroup(itemType));
        return File.ReadAllBytesAsync(path, ct);
    }

    public async Task<byte[]> ExportAsync(
        QuotationBaseQueryDto query,
        IReadOnlyCollection<int>? selectedIds = null,
        CancellationToken ct = default)
    {
        var rows = await _base.QueryAllAsync(query, selectedIds, ct);
        if (rows.Count == 0)
            return [];

        var controller = IsControllerGroup(query.ItemType);
        var templateBytes = await File.ReadAllBytesAsync(EnsureTemplate(controller), ct);
        using var input = new MemoryStream(templateBytes);
        using var wb = new XLWorkbook(input);
        var ws = wb.Worksheets.First();
        // 在英文字段行下方插入数据行，避免覆盖第2行表头
        if (rows.Count > 0)
            ws.Row(DataStartRow - 1).InsertRowsBelow(rows.Count);

        var images = await DownloadImagesAsync(rows, ct);
        var pending = new List<PendingPicture>();
        var typeLabel = TypeLabel(query.ItemType);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var excelRow = DataStartRow + i;
            if (controller)
                FillControllerRow(ws, excelRow, row, typeLabel, images, pending);
            else
                FillMotorGroupRow(ws, excelRow, row, typeLabel, images, pending);
        }

        PlacePendingPictures(ws, pending);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<QuotationBaseImportResult> ImportAsync(
        Stream excelStream,
        string? empNo,
        CancellationToken ct = default)
    {
        var actor = (empNo ?? "").Trim();
        if (actor.Length == 0)
            return Fail("无法获取当前登录工号，请重新登录后再导入");

        if (excelStream.CanSeek)
            excelStream.Position = 0;

        XLWorkbook wb;
        try
        {
            wb = new XLWorkbook(excelStream);
        }
        catch (Exception ex)
        {
            return Fail("无法打开 Excel，请使用 xlsx 模板：" + ex.Message);
        }

        using (wb)
        {
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws is null)
                return Fail("工作簿中没有工作表");

            var controller = DetectControllerTemplate(ws);
            var pictures = IndexPictures(ws);
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            foreach (var key in pictures.Keys)
                lastRow = Math.Max(lastRow, key.Row);

            if (lastRow < DataStartRow)
                return Fail("没有可导入的数据");

            var imported = 0;
            var updated = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var excelRow = DataStartRow; excelRow <= lastRow; excelRow++)
            {
                if (IsRowEmpty(ws, excelRow, pictures, controller ? CLogo : MLogo))
                    continue;

                var parsed = await ParseRowAsync(ws, excelRow, pictures, controller, ct);
                if (!parsed.Ok)
                    return Fail(parsed.Error ?? $"第{excelRow}行解析失败");

                var dto = parsed.Row!;
                var type = QuotationBaseTypes.Normalize(dto.ItemType);
                var partNo = (dto.Part_No ?? "").Trim();
                if (partNo.Length == 0)
                    return Fail($"第{excelRow}行，型号 Part_No 不能为空");

                var key = $"{type}\u001f{partNo}";
                if (!seen.Add(key))
                    return Fail($"第{excelRow}行与文件内其它行型号重复（{TypeLabel(type)} / {partNo}）");

                dto.Part_No = partNo;
                dto.ItemType = type;

                var existing = await FindExistingAsync(type, partNo, ct);
                var isCreate = existing is null;
                if (!isCreate)
                    dto.Id = existing!.Id;

                var (ok, msg, _) = await _base.SaveAsync(type, dto, actor, isCreate, ct);
                if (!ok)
                    return Fail($"第{excelRow}行，{msg ?? "保存失败"}");

                if (isCreate) imported++;
                else updated++;
            }

            if (imported + updated == 0)
                return Fail("没有可导入的数据");

            return new QuotationBaseImportResult
            {
                Ok = true,
                Imported = imported,
                Updated = updated,
                Message = updated > 0
                    ? $"成功导入 {imported} 条，更新 {updated} 条"
                    : $"成功导入 {imported} 条"
            };
        }
    }

    private async Task<QuotationBaseListItemDto?> FindExistingAsync(
        string type, string partNo, CancellationToken ct)
    {
        var list = await _base.QueryAllAsync(new QuotationBaseQueryDto
        {
            ItemType = type,
            FilterColumn = "Part_No",
            Keyword = partNo,
        }, ct: ct);
        return list.FirstOrDefault(x =>
            string.Equals((x.Part_No ?? "").Trim(), partNo, StringComparison.OrdinalIgnoreCase));
    }

    private static bool DetectControllerTemplate(IXLWorksheet ws)
    {
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        // 官方模板英文字段在第2行；兼容仅1行表头。推杆类约16列，电机类约20列。
        for (var r = 1; r <= 2; r++)
        {
            for (var c = 1; c <= Math.Min(lastCol, 20); c++)
            {
                var h = CellText(ws, r, c);
                if (h.Contains("Rated_Voltage", StringComparison.OrdinalIgnoreCase)
                    || h.Contains("Rated Voltage", StringComparison.OrdinalIgnoreCase)
                    || h.Contains("Max_Loading", StringComparison.OrdinalIgnoreCase)
                    || h.Contains("Max Loading", StringComparison.OrdinalIgnoreCase)
                    || h.Contains("额定电压", StringComparison.Ordinal)
                    || h.Contains("最大负载", StringComparison.Ordinal)
                    || h.Contains("控制盒", StringComparison.Ordinal)
                    || h.Contains("推杆必填", StringComparison.Ordinal))
                    return true;
            }
        }

        return lastCol > 0 && lastCol <= 17;
    }

    private async Task<(bool Ok, string? Error, QuotationBaseListItemDto? Row)> ParseRowAsync(
        IXLWorksheet ws,
        int row,
        Dictionary<(int Row, int Col), (byte[] Bytes, XLPictureFormat Format)> pictures,
        bool controller,
        CancellationToken ct)
    {
        try
        {
            if (controller)
            {
                var typeRaw = CellText(ws, row, CType);
                var type = ResolveType(typeRaw, preferController: true);
                if (type is not (QuotationBaseTypes.CB or QuotationBaseTypes.PR))
                    return (false, $"第{row}行，Type 应为「控制盒」或「推杆」（当前：{typeRaw}）", null);

                var dto = new QuotationBaseListItemDto
                {
                    ItemType = type,
                    Part_No = CellText(ws, row, CPartNo),
                    Remark = CellText(ws, row, CRemark),
                };
                if (type == QuotationBaseTypes.CB)
                {
                    dto.Rated_Voltage = CellText(ws, row, CVoltage);
                    dto.Size = CellText(ws, row, CSize);
                    dto.Rated_Power = CellText(ws, row, CRatedPower);
                    dto.No_Of_Actuator = CellText(ws, row, CActuator);
                    dto.Output_Control = CellText(ws, row, COutput);
                    dto.Sensor = CellText(ws, row, CSensor);
                    dto.Protection_Degree = CellText(ws, row, CProtect);
                }
                else
                {
                    dto.Max_Loading = CellText(ws, row, CMaxLoad);
                    dto.Max_Stroke = CellText(ws, row, CMaxStroke);
                    dto.Protection_Degree2 = CellText(ws, row, CProtect2);
                    dto.Noise = CellText(ws, row, CNoise);
                }

                dto.Quotation_logo = await TryImportPictureAsync(pictures, row, CLogo, ct);
                return (true, null, dto);
            }
            else
            {
                var typeRaw = CellText(ws, row, MType);
                var type = ResolveType(typeRaw, preferController: false);
                if (type is QuotationBaseTypes.CB or QuotationBaseTypes.PR)
                    return (false, $"第{row}行，电机类模板不能导入「{typeRaw}」，请使用推杆类模板", null);

                var dto = new QuotationBaseListItemDto
                {
                    ItemType = type,
                    Part_No = CellText(ws, row, MPartNo),
                    Remark = CellText(ws, row, MRemark),
                };
                switch (type)
                {
                    case QuotationBaseTypes.Remote:
                        dto.Remote_controller_type = CellText(ws, row, MRemoteType);
                        dto.Battery_Type = CellText(ws, row, MBatteryType);
                        dto.Radio_Frequency = CellText(ws, row, MRadio);
                        dto.Working_Temperature = CellText(ws, row, MTemp);
                        dto.NoOfChannels = CellText(ws, row, MChannels);
                        break;
                    case QuotationBaseTypes.Accessory:
                        dto.adaptor = CellText(ws, row, MAdaptor);
                        break;
                    default:
                        dto.Rated_torque = CellText(ws, row, MTorque);
                        dto.Rated_speed = CellText(ws, row, MSpeed);
                        dto.Power_supply = CellText(ws, row, MPowerSupply);
                        dto.Rated_Power = CellText(ws, row, MRatedPower);
                        dto.Rated_Current = CellText(ws, row, MCurrent);
                        dto.Noise_dBA = CellText(ws, row, MNoise);
                        dto.Motor_Length = CellText(ws, row, MLength);
                        dto.Tube_diameter = CellText(ws, row, MTube);
                        dto.Battery_Capacity = CellText(ws, row, MBatteryCap);
                        dto.Exclude_text = CellText(ws, row, MExclude);
                        break;
                }

                dto.Quotation_logo = await TryImportPictureAsync(pictures, row, MLogo, ct);
                return (true, null, dto);
            }
        }
        catch (Exception ex)
        {
            return (false, $"第{row}行解析异常：{ex.Message}", null);
        }
    }

    private static string ResolveType(string? raw, bool preferController)
    {
        var s = (raw ?? "").Trim();
        if (s.Length == 0)
            return preferController ? QuotationBaseTypes.CB : QuotationBaseTypes.Motor;
        return QuotationBaseTypes.Normalize(s);
    }

    private async Task<string?> TryImportPictureAsync(
        Dictionary<(int Row, int Col), (byte[] Bytes, XLPictureFormat Format)> pictures,
        int row,
        int col,
        CancellationToken ct)
    {
        if (!pictures.TryGetValue((row, col), out var pic) || pic.Bytes.Length == 0)
            return null;
        try
        {
            var ext = pic.Format switch
            {
                XLPictureFormat.Jpeg => ".jpg",
                XLPictureFormat.Gif => ".gif",
                XLPictureFormat.Bmp => ".bmp",
                _ => ".png"
            };
            await using var ms = new MemoryStream(pic.Bytes, writable: false);
            return await _cotractBase.UploadImageAsync(ms, "qb" + ext, ct);
        }
        catch
        {
            return null;
        }
    }

    private void FillMotorGroupRow(
        IXLWorksheet ws,
        int row,
        QuotationBaseListItemDto item,
        string typeLabel,
        Dictionary<string, byte[]> images,
        List<PendingPicture> pending)
    {
        ws.Cell(row, MPartNo).Value = item.Part_No;
        ws.Cell(row, MType).Value = typeLabel;
        ws.Cell(row, MTorque).Value = item.Rated_torque;
        ws.Cell(row, MSpeed).Value = item.Rated_speed;
        ws.Cell(row, MPowerSupply).Value = item.Power_supply;
        ws.Cell(row, MRatedPower).Value = item.Rated_Power;
        ws.Cell(row, MCurrent).Value = item.Rated_Current;
        ws.Cell(row, MNoise).Value = item.Noise_dBA;
        ws.Cell(row, MLength).Value = item.Motor_Length;
        ws.Cell(row, MTube).Value = item.Tube_diameter;
        ws.Cell(row, MBatteryCap).Value = item.Battery_Capacity;
        ws.Cell(row, MExclude).Value = item.Exclude_text;
        ws.Cell(row, MRemoteType).Value = item.Remote_controller_type;
        ws.Cell(row, MBatteryType).Value = item.Battery_Type;
        ws.Cell(row, MRadio).Value = item.Radio_Frequency;
        ws.Cell(row, MTemp).Value = item.Working_Temperature;
        ws.Cell(row, MChannels).Value = item.NoOfChannels;
        ws.Cell(row, MAdaptor).Value = item.adaptor;
        ws.Cell(row, MRemark).Value = item.Remark;
        PutImageOrText(ws, row, MLogo, item.Quotation_logo, images, pending);
    }

    private void FillControllerRow(
        IXLWorksheet ws,
        int row,
        QuotationBaseListItemDto item,
        string typeLabel,
        Dictionary<string, byte[]> images,
        List<PendingPicture> pending)
    {
        ws.Cell(row, CPartNo).Value = item.Part_No;
        ws.Cell(row, CType).Value = typeLabel;
        ws.Cell(row, CVoltage).Value = item.Rated_Voltage;
        ws.Cell(row, CSize).Value = item.Size;
        ws.Cell(row, CRatedPower).Value = item.Rated_Power;
        ws.Cell(row, CActuator).Value = item.No_Of_Actuator;
        ws.Cell(row, COutput).Value = item.Output_Control;
        ws.Cell(row, CSensor).Value = item.Sensor;
        ws.Cell(row, CProtect).Value = item.Protection_Degree;
        ws.Cell(row, CMaxLoad).Value = item.Max_Loading;
        ws.Cell(row, CMaxStroke).Value = item.Max_Stroke;
        ws.Cell(row, CProtect2).Value = item.Protection_Degree2;
        ws.Cell(row, CNoise).Value = item.Noise;
        ws.Cell(row, CAdaptor).Value = item.adaptor;
        ws.Cell(row, CRemark).Value = item.Remark;
        PutImageOrText(ws, row, CLogo, item.Quotation_logo, images, pending);
    }

    private string EnsureTemplate(bool controller)
    {
        var name = controller ? ControllerTemplateFileName : MotorTemplateFileName;
        var path = ResolveTemplatePath(name, createDir: true);
        if (File.Exists(path))
            return path;

        var headers = controller ? ControllerHeaders : MotorHeaders;
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        // 与官方模板一致：第1行分组占位，第2行英文字段，数据从第3行起
        ws.Cell(1, 1).Value = controller ? "控制盒/推杆" : "电机/遥控器/配件";
        ws.Cell(1, 1).Style.Font.Bold = true;
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(2, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
        }

        ws.SheetView.FreezeRows(2);
        ws.Row(1).Height = 22;
        ws.Row(2).Height = 22;
        ws.Columns(1, headers.Length).Width = 14;
        wb.SaveAs(path);
        return path;
    }

    private string ResolveTemplatePath(string fileName, bool createDir = false)
    {
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(webRoot, "Templates");
        if (createDir)
            Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);
        if (!createDir && !File.Exists(path))
            throw new FileNotFoundException("未找到模板，请确认文件在 wwwroot/Templates/" + fileName);
        return path;
    }

    private async Task<Dictionary<string, byte[]>> DownloadImagesAsync(
        List<QuotationBaseListItemDto> rows, CancellationToken ct)
    {
        var urls = rows
            .Select(r => LegacyFileUrl.Resolve(r.Quotation_logo, _imageBase))
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
                using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode) return;
                if (resp.Content.Headers.ContentLength is > 8 * 1024 * 1024) return;
                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                if (bytes.Length >= 2)
                    bag[url] = bytes;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch { /* 单张失败忽略 */ }
            finally { gate.Release(); }
        }));

        foreach (var kv in bag)
            result[kv.Key] = kv.Value;
        return result;
    }

    private void PutImageOrText(
        IXLWorksheet ws, int row, int col, string? path,
        Dictionary<string, byte[]> images, List<PendingPicture> pending)
    {
        var raw = (path ?? "").Trim();
        if (LegacyFileUrl.IsBlankOrPlaceholder(raw))
        {
            if (raw.Length > 0)
                ws.Cell(row, col).Value = raw;
            return;
        }

        var url = LegacyFileUrl.Resolve(raw, _imageBase);
        if (url.Length > 0 && images.TryGetValue(url, out var bytes))
        {
            QueuePicture(ws, row, col, bytes, $"qbR{row}C{col}", pending);
            return;
        }

        ws.Cell(row, col).Value = raw;
    }

    private static void QueuePicture(
        IXLWorksheet ws, int row, int col, byte[] bytes, string name, List<PendingPicture> pending)
    {
        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            var format = DetectPictureFormat(bytes);
            var pic = format == XLPictureFormat.Unknown
                ? ws.AddPicture(ms, name)
                : ws.AddPicture(ms, format, name);
            pending.Add(new PendingPicture
            {
                Pic = pic,
                Row = row,
                Col = col,
                ImgW = Math.Max(1, pic.OriginalWidth),
                ImgH = Math.Max(1, pic.OriginalHeight)
            });
        }
        catch
        {
            // 单张图失败不影响整份导出
        }
    }

    private static void PlacePendingPictures(IXLWorksheet ws, List<PendingPicture> pending)
    {
        const double pad = 5;
        const double pxPerColChar = 7.0;
        // 与老系统 SetPicture 一致：显示尺寸上限 500×400
        const double maxDisplayW = 500;
        const double maxDisplayH = 400;
        const double maxColChars = 255;
        const double maxRowPts = 409;

        foreach (var p in pending)
        {
            if (p.ImgW > maxDisplayW || p.ImgH > maxDisplayH)
            {
                var fit = Math.Min(maxDisplayW / p.ImgW, maxDisplayH / p.ImgH);
                p.ImgW *= fit;
                p.ImgH *= fit;
            }

            var needCol = (p.ImgW + 2 * pad) / pxPerColChar;
            var needRow = (p.ImgH + 2 * pad) * 72.0 / 96.0;
            if (needCol > maxColChars || needRow > maxRowPts)
            {
                var scale = Math.Min(
                    maxColChars * pxPerColChar / (p.ImgW + 2 * pad),
                    maxRowPts * 96.0 / 72.0 / (p.ImgH + 2 * pad));
                p.ImgW *= scale;
                p.ImgH *= scale;
                needCol = (p.ImgW + 2 * pad) / pxPerColChar;
                needRow = (p.ImgH + 2 * pad) * 72.0 / 96.0;
            }

            if (ws.Column(p.Col).Width < needCol)
                ws.Column(p.Col).Width = needCol;
            if (ws.Row(p.Row).Height < needRow)
                ws.Row(p.Row).Height = needRow;
        }

        foreach (var p in pending)
        {
            var cellW = ws.Column(p.Col).Width * pxPerColChar;
            var cellH = ws.Row(p.Row).Height * 96.0 / 72.0;
            var finalW = (int)Math.Max(1, Math.Round(p.ImgW));
            var finalH = (int)Math.Max(1, Math.Round(p.ImgH));
            var ox = (int)Math.Max(0, Math.Round((cellW - finalW) / 2.0));
            var oy = (int)Math.Max(0, Math.Round((cellH - finalH) / 2.0));
            var cell = ws.Cell(p.Row, p.Col);
            p.Pic.MoveTo(cell, ox, oy, cell, ox + finalW, oy + finalH);
        }
    }

    private static XLPictureFormat DetectPictureFormat(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
            return XLPictureFormat.Jpeg;
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return XLPictureFormat.Png;
        if (bytes.Length >= 3 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
            return XLPictureFormat.Gif;
        if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
            return XLPictureFormat.Bmp;
        return XLPictureFormat.Unknown;
    }

    private static Dictionary<(int Row, int Col), (byte[] Bytes, XLPictureFormat Format)> IndexPictures(IXLWorksheet ws)
    {
        var map = new Dictionary<(int, int), (byte[], XLPictureFormat)>();
        foreach (var pic in ws.Pictures)
        {
            try
            {
                var r = pic.TopLeftCell.Address.RowNumber;
                var c = pic.TopLeftCell.Address.ColumnNumber;
                var src = pic.ImageStream;
                using var ms = new MemoryStream();
                if (src.CanSeek) src.Position = 0;
                src.CopyTo(ms);
                var bytes = ms.ToArray();
                if (bytes.Length > 0)
                    map[(r, c)] = (bytes, pic.Format);
            }
            catch
            {
                // ignore
            }
        }

        return map;
    }

    private static bool IsRowEmpty(
        IXLWorksheet ws,
        int row,
        Dictionary<(int Row, int Col), (byte[] Bytes, XLPictureFormat Format)> pictures,
        int lastCol)
    {
        if (pictures.Keys.Any(k => k.Row == row))
            return false;
        for (var col = 1; col <= lastCol; col++)
        {
            if (!string.IsNullOrWhiteSpace(CellText(ws, row, col)))
                return false;
        }

        return true;
    }

    private static string CellText(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);
        if (cell.IsEmpty()) return "";
        return (cell.GetString() ?? cell.Value.ToString() ?? "").Trim();
    }

    private static QuotationBaseImportResult Fail(string message)
        => new() { Ok = false, Message = message };

    private sealed class PendingPicture
    {
        public required IXLPicture Pic { get; init; }
        public int Row { get; init; }
        public int Col { get; init; }
        public double ImgW { get; set; }
        public double ImgH { get; set; }
    }
}
