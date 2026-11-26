using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 三份表单的 Excel 导出。PDF 由 <see cref="LibreOfficePdfService"/> 把这个 xlsx 转过去，
/// 所以屏幕上看到的版式和导出的文件是同一套数据、同一种左右结构。
///
/// 版式：5 列（16% / 16% / 21% / 23% / 24%），标签占 A:B，值占 C:E，
/// 与 wwwroot/css/motor-sheet.css 里的 .msheet 比例一致。
/// </summary>
public class RemoteSheetExportService
{
    private const string Company = "浙江捷昌线性驱动科技股份有限公司";

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<RemoteSheetExportService> _logger;
    private readonly SqlServerDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _imageBase;

    public RemoteSheetExportService(
        IWebHostEnvironment env,
        ILogger<RemoteSheetExportService> logger,
        SqlServerDbContext db,
        IHttpClientFactory httpFactory,
        IOptions<LegacyFilesOptions> legacyFiles)
    {
        _env = env;
        _logger = logger;
        _db = db;
        _httpFactory = httpFactory;
        _imageBase = legacyFiles.Value.ResolvedBaseUrl();
    }

    /// <summary>工号 → 姓名（员工表）。业务表只存工号，导出时补姓名。</summary>
    private string? EmpName(string? empNo)
    {
        if (string.IsNullOrWhiteSpace(empNo)) return null;
        var no = empNo.Trim();
        return _db.Employees.AsNoTracking()
            .Where(x => x.EmpNo == no)
            .Select(x => x.EmpName)
            .FirstOrDefault();
    }

    /// <summary>wwwroot/Templates 下的模板路径；文件不存在时返回 null（调用方回退旧实现）。</summary>
    private string? TemplatePath(string fileName)
    {
        var root = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(AppContext.BaseDirectory, "wwwroot");

        var path = Path.Combine(root, "Templates", fileName);
        return File.Exists(path) ? path : null;
    }

    // ==================== 对外入口 ====================

    /// <summary>下载文件名：单据名_客户_型号_版本.扩展名，去掉文件系统非法字符。</summary>
    public string FileName(string docLabel, string? customer, string? model, string? versionNo, string ext)
    {
        var parts = new[] { docLabel, customer, model, versionNo }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Sanitize(x!))
            .Where(x => x.Length > 0);

        var name = string.Join("_", parts);
        if (name.Length == 0) name = docLabel;
        if (name.Length > 90) name = name[..90];
        return name + "." + ext.TrimStart('.');
    }

    /// <summary>
    /// 规格书导出：优先套用 wwwroot/Templates/遥控器规格书.xlsx。
    /// 模板里的表格线、合并区域、中英文对照说明全部原样保留，只填数据与勾选。
    /// </summary>
    public byte[] BuildGg(RemoteGgModel head, RemoteDocExtra extra)
    {
        var filled = TryBuildSpecFromTemplate(extra, "遥控器规格书.xlsx", GgParamStart, approval: false);
        if (filled is not null) return filled;

        _logger.LogWarning("规格书模板导出未生效，已回退到内置版式：{Reason}",
            ExcelTemplateWriter.LastError ?? "未知原因");
        return BuildGgLegacy(head, extra);
    }

    /// <summary>承认书导出：模板「遥控器样品承认书.xlsx」，比规格书多一块样品检验结果。</summary>
    public byte[] BuildCr(RemoteCrModel head, RemoteDocExtra extra)
    {
        var filled = TryBuildSpecFromTemplate(extra, "遥控器样品承认书.xlsx", CrParamStart, approval: true);
        if (filled is not null) return filled;

        _logger.LogWarning("承认书模板导出未生效，已回退到内置版式：{Reason}",
            ExcelTemplateWriter.LastError ?? "未知原因");
        return BuildCrLegacy(head, extra);
    }

    /// <summary>两份模板表头以下的行偏移恒为 3，参数表起始行不同，其余按偏移推。</summary>
    private const int GgParamStart = 33;
    private const int CrParamStart = 30;

    /// <summary>
    /// 模板坐标 ← 单据字段。行号按 <paramref name="paramStart"/>（参数表首行）算偏移，
    /// 两份模板共用一个映射，承认书只多出检验结果与少一块变更履历。
    /// </summary>
    private byte[]? TryBuildSpecFromTemplate(RemoteDocExtra extra, string fileName, int paramStart, bool approval)
    {
        int Row(int offset) => paramStart + offset;

        var cells = new Dictionary<string, string?>
        {
            // 表头：产品名称 / 产品型号 / 客户 / 公司（模板标签在 B 列，值填 D 列，C 列留白避免挤掉标签）
            ["D9"] = Field(extra, RemoteDocKeys.ProductName),
            ["D10"] = Field(extra, RemoteDocKeys.PartCode),
            ["D11"] = Field(extra, RemoteDocKeys.Customer),
            ["D12"] = Field(extra, RemoteDocKeys.Company),

            // 一、产品功性能要求及参数规格表：偏移量 = 模板 A 列标签行 - 参数表首行(paramStart)
            // 规格书 33 起、承认书 30 起，两份模板行序完全一致
            ["C" + Row(0)] = Field(extra, RemoteDocKeys.ModelNo),
            ["C" + Row(1)] = Field(extra, RemoteDocKeys.RemoteSize),
            ["C" + Row(2)] = Field(extra, RemoteDocKeys.BracketSize),
            ["C" + Row(3)] = Field(extra, RemoteDocKeys.RemoteWeight),
            ["C" + Row(4)] = Field(extra, RemoteDocKeys.RemoteColor),
            // Row(5) 电池型号、Row(6) 充电口是复选行，见 checks
            ["C" + Row(7)] = Field(extra, RemoteDocKeys.BatteryEndurance),
            ["C" + Row(8)] = Field(extra, RemoteDocKeys.TransmitPower),
            // Row(9)(10)(11)(12)(13) 全为复选行，见 checks
            ["C" + Row(14)] = WithUnit("室内Indoor", Field(extra, RemoteDocKeys.RfDistanceIndoor)),
            ["C" + Row(15)] = WithUnit("室外Ondoor", Field(extra, RemoteDocKeys.RfDistanceOutdoor)),
            // Row(16) 应用场合、Row(17) 丝印图纸见 checks；Row(18) 是丝印行跨到的第 2 行，不填
            ["C" + Row(20)] = Field(extra, RemoteDocKeys.Warranty),
            ["C" + Row(21)] = Field(extra, RemoteDocKeys.SpecialFunction),
            ["C" + Row(22)] = Field(extra, RemoteDocKeys.OtherRequirement),

            // 二、产品使用工况（Row(26) 使用环境、Row(27) IP 等级为复选行）
            ["C" + Row(28)] = Field(extra, RemoteDocKeys.WorkingTemp),
            ["C" + Row(29)] = Field(extra, RemoteDocKeys.Esd)
        };

        // 三、选配配件（模板固定 4 行：A 序号 / B 零件 / C 型号 / D 示意图 / E 备注）
        for (var i = 0; i < extra.Accessories.Count && i < 4; i++)
        {
            var row = Row(33 + i);
            cells["B" + row] = extra.Accessories[i].Name;
            cells["C" + row] = extra.Accessories[i].Type;
            cells["E" + row] = extra.Accessories[i].Remark;
        }

        // 五、产品附件（模板固定 7 行，零件名称印在表上，只填型号与备注）
        for (var i = 0; i < extra.Attachments.Count && i < 7; i++)
        {
            var row = Row(44 + i);
            cells["C" + row] = extra.Attachments[i].Type;
            cells["E" + row] = extra.Attachments[i].Remark;
        }

        // 六、包装（模板固定 6 行：A 项目 / B 示意图 / C 尺寸 / D 材质 / E 内含数量）
        for (var i = 0; i < extra.Packages.Count && i < 6; i++)
        {
            var row = Row(54 + i);
            cells["C" + row] = extra.Packages[i].Size;
            cells["D" + row] = extra.Packages[i].Material;
            cells["E" + row] = extra.Packages[i].Qty;
        }

        // 七、产品操作说明：模板 Row(61) 是章节标题，内容行从 Row(62) 起与 OperationRows 顺序一致
        for (var i = 0; i < OperationRows.Length; i++)
        {
            var code = OperationRows[i].Code;
            if (code.Length == 0) continue;
            extra.Operations.TryGetValue(code, out var value);
            if (string.IsNullOrWhiteSpace(value)) continue;
            cells["C" + Row(62 + i)] = value;
        }

        // 复选行：在模板原有的方框上打勾（值来自页面多选，逗号分隔）
        var checks = new Dictionary<string, string?>
        {
            ["C" + Row(5)] = Field(extra, RemoteDocKeys.BatteryType),
            ["C" + Row(6)] = Field(extra, RemoteDocKeys.RechargePort),
            ["C" + Row(9)] = Field(extra, RemoteDocKeys.RadioMode),
            ["C" + Row(10)] = Field(extra, RemoteDocKeys.RadioFrequency),
            ["C" + Row(11)] = Field(extra, RemoteDocKeys.RadioModeSecond),
            ["C" + Row(12)] = Field(extra, RemoteDocKeys.Protocol),
            ["C" + Row(13)] = Field(extra, RemoteDocKeys.ChannelQuantity),
            ["C" + Row(16)] = Field(extra, RemoteDocKeys.Applications),
            ["C" + Row(17)] = Field(extra, RemoteDocKeys.SurfacePrint),
            ["C" + Row(19)] = Field(extra, RemoteDocKeys.Certification),
            ["C" + Row(26)] = Field(extra, RemoteDocKeys.UsageEnv),
            ["C" + Row(27)] = Field(extra, RemoteDocKeys.IpDegree)
        };

        // 勾完之后的「Other____」「其它（自定义填写）」：另起一行补在该格子末尾
        var appends = new Dictionary<string, string?>
        {
            ["C" + Row(5)] = Field(extra, RemoteDocKeys.BatteryTypeOther),
            ["C" + Row(6)] = Field(extra, RemoteDocKeys.RechargePortOther),
            ["C" + Row(10)] = Field(extra, RemoteDocKeys.RfOther),
            ["C" + Row(16)] = Field(extra, RemoteDocKeys.ApplicationsOther),
            ["C" + Row(19)] = Field(extra, RemoteDocKeys.CertificationOther),
            ["C" + Row(26)] = Field(extra, RemoteDocKeys.UsageOther),
            ["C" + Row(27)] = Field(extra, RemoteDocKeys.IpOther)
        };

        if (approval)
        {
            // 承认书：样品检验结果的结论列（E16~E21，模板预置 □ PASS □ FAIL）
            for (var i = 0; i < extra.SampleChecks.Count && i < 6; i++)
                checks["E" + (16 + i)] = extra.SampleChecks[i].Result;
        }
        else
        {
            // 规格书：变更履历表 A20 标题、21~22 表头（A21 变更日期 / B21 版本 / C21 变更人 /
            // D21:E21 变更内容描述，22 行 D22 变更前、E22 变更后），数据行 23~30 共 8 行，
            // 第 31 行已经是「一、产品功性能…」标题，多写的记录会盖掉章节标题。
            var row = 23;
            foreach (var log in extra.ChangeLog.Take(8))
            {
                cells["A" + row] = log.Date;
                cells["B" + row] = log.Rev;
                cells["C" + row] = log.By;
                cells["D" + row] = log.Before;
                cells["E" + row] = log.After;
                row++;
            }
        }

        if (!ExcelTemplateWriter.TryFill(TemplatePath(fileName), cells, checks, appends, out var bytes))
            return null;

        // 配件 / 附件 / 包装的示意图：库里只存老站相对路径，导出时下载回来贴进模板
        var pictures = BuildPictures(extra, paramStart);
        if (pictures.Count > 0)
            bytes = AddPictures(bytes, pictures);

        return bytes;
    }

    /// <summary>
    /// 贴示意图：与报价单导出同一条路 —— 用 ClosedXML 打开已填好的工作簿，
    /// <c>AddPicture</c> 后按单元格大小等比缩放、居中放进「示意图」格，再存回去。
    ///
    /// 之前是自己往 zip 里拼 drawing XML，图片在文件里但 Excel / WPS 不显示；
    /// ClosedXML 写出来的 drawing 是 Office 自己的产物，各版本都能正常渲染，
    /// 而且模板的页眉公司标识（legacyDrawingHF）和打印区域都会原样保留。
    /// </summary>
    private byte[] AddPictures(byte[] xlsx, IReadOnlyList<SheetPicture> pictures)
    {
        try
        {
            using var input = new MemoryStream(xlsx, writable: false);
            using var wb = new XLWorkbook(input);
            var ws = wb.Worksheets.First();

            foreach (var picture in pictures)
            {
                if (picture.Data.Length == 0) continue;

                try
                {
                    var cell = ws.Cell(picture.CellReference);
                    using var ms = new MemoryStream(picture.Data, writable: false);
                    var format = DetectPictureFormat(picture.Data);
                    var name = "pk_" + Guid.NewGuid().ToString("N")[..8];
                    var pic = format == XLPictureFormat.Unknown
                        ? ws.AddPicture(ms, name)
                        : ws.AddPicture(ms, format, name);

                    // 格子内可用像素：列宽（字符数）≈ 7px/字 + 5px 边距；行高按 pt→px。
                    // 尺寸图那几格是 C:E 合并格，要按整块合并区域算，否则图只有一列宽。
                    const double pad = 4;
                    var merged = ws.MergedRanges.FirstOrDefault(r => r.Contains(cell));
                    var firstCol = merged?.RangeAddress.FirstAddress.ColumnNumber ?? cell.Address.ColumnNumber;
                    var lastCol = merged?.RangeAddress.LastAddress.ColumnNumber ?? cell.Address.ColumnNumber;
                    var firstRow = merged?.RangeAddress.FirstAddress.RowNumber ?? cell.Address.RowNumber;
                    var lastRow = merged?.RangeAddress.LastAddress.RowNumber ?? cell.Address.RowNumber;

                    var rawW = 0.0;
                    for (var c = firstCol; c <= lastCol; c++) rawW += ws.Column(c).Width * 7 + 5;

                    var rawH = 0.0;
                    for (var r = firstRow; r <= lastRow; r++) rawH += ws.Row(r).Height * 96.0 / 72.0;

                    var cellW = Math.Max(16, rawW - pad * 2);
                    var cellH = Math.Max(16, rawH - pad * 2);
                    var imgW = Math.Max(1.0, pic.OriginalWidth);
                    var imgH = Math.Max(1.0, pic.OriginalHeight);
                    var scale = Math.Min(1.0, Math.Min(cellW / imgW, cellH / imgH));
                    var w = Math.Max(1, (int)Math.Round(imgW * scale));
                    var h = Math.Max(1, (int)Math.Round(imgH * scale));

                    // 先设自由浮动再改尺寸：ClosedXML 默认的 MoveAndSize 不允许 SetSize，
                    // 直接 WithSize 会抛“To set the width, the placement should be FreeFloating or Move”，
                    // 图片就退回原始大尺寸/尺寸异常，看起来像没贴上。
                    pic.Placement = XLPicturePlacement.FreeFloating;
                    pic.WithSize(w, h);
                    pic.MoveTo(cell,
                        Math.Max(0, (int)Math.Round((cellW - w) / 2.0)),
                        Math.Max(0, (int)Math.Round((cellH - h) / 2.0)));
                }
                catch (Exception ex)
                {
                    // 单张图坏掉不影响整份导出
                    _logger.LogWarning("示意图贴图失败：{Cell} {Error}", picture.CellReference, ex.Message);
                }
            }

            using var output = new MemoryStream();
            wb.SaveAs(output);
            _logger.LogInformation("示意图：已嵌入 {Count} 张", pictures.Count);
            return output.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("规格书示意图嵌入失败：{Error}", ex.Message);
            return xlsx;
        }
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

    /// <summary>要贴到某个单元格上的一张示意图。</summary>
    private sealed record SheetPicture(string CellReference, byte[] Data);

    /// <summary>
    /// 「室内Indoor （自定义填写）米m」这类格子：模板把标签和填写处印在同一格里，
    /// 只填数字会把标签冲掉，所以保留标签、把值塞进填写位置。
    /// </summary>
    private static string? WithUnit(string label, string? value)
        => string.IsNullOrWhiteSpace(value) ? null : $"{label}   {value.Trim()} 米m";

    // ==================== 示意图嵌入 ====================

    /// <summary>
    /// 配件 / 附件 / 包装的示意图存在 3.240 老站（库里只存 /Img/... 相对路径），
    /// 导出时下载回来贴到模板对应的「示意图」格上：
    /// 配件与附件在 D 列，包装的示意图在 B 列。
    /// 下载失败的格子留空，不影响整份导出。
    /// </summary>
    private IReadOnlyList<SheetPicture> BuildPictures(RemoteDocExtra extra, int paramStart)
    {
        var slots = new List<(string Cell, string? Path)>();

        for (var i = 0; i < extra.Accessories.Count && i < 4; i++)
            slots.Add(("D" + (paramStart + 33 + i), extra.Accessories[i].Photo));

        for (var i = 0; i < extra.Attachments.Count && i < 7; i++)
            slots.Add(("D" + (paramStart + 44 + i), extra.Attachments[i].Photo));

        for (var i = 0; i < extra.Packages.Count && i < 6; i++)
            slots.Add(("B" + (paramStart + 54 + i), extra.Packages[i].Drawing));

        // 四、产品尺寸图：外形尺寸图 / 电控系统图，两格都是 C:E 合并格（标签在 A:B）
        slots.Add(("C" + (paramStart + 39), Field(extra, RemoteDocKeys.OverallDrawing)));
        slots.Add(("C" + (paramStart + 40), Field(extra, RemoteDocKeys.ControlDiagram)));

        var wanted = slots.Where(x => !LegacyFileUrl.IsBlankOrPlaceholder(x.Path)).ToList();
        _logger.LogInformation("示意图：模板位置 {Slots} 个，有图片 {Wanted} 个", slots.Count, wanted.Count);
        if (wanted.Count == 0) return [];

        var http = _httpFactory.CreateClient("LegacyFiles");
        var cache = new Dictionary<string, byte[]?>(StringComparer.OrdinalIgnoreCase);
        var result = new List<SheetPicture>(wanted.Count);

        foreach (var (cell, path) in wanted)
        {
            var url = LegacyFileUrl.Resolve(path, _imageBase);
            if (url.Length == 0) continue;

            if (!cache.TryGetValue(url, out var data))
            {
                data = DownloadImage(http, url);
                cache[url] = data;
                if (data is null) _logger.LogWarning("示意图下载失败：{Url}", url);
            }

            if (data is { Length: > 0 }) result.Add(new SheetPicture(cell, data));
        }

        _logger.LogInformation("示意图：成功下载 {Ok} 张", result.Count);
        return result;
    }

    /// <summary>
    /// 同步下载：导出接口是同步的（Blazor 事件里直接调用），
    /// 这里丢到线程池里等结果，避免在 UI 同步上下文上原地阻塞。
    /// </summary>
    private static byte[]? DownloadImage(HttpClient http, string url)
    {
        try
        {
            return Task.Run(async () =>
            {
                using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (!resp.IsSuccessStatusCode) return null;
                if (resp.Content.Headers.ContentLength is > 8 * 1024 * 1024) return null;

                var data = await resp.Content.ReadAsByteArrayAsync();
                return LooksLikeImage(data) ? data : null;
            }).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    private static bool LooksLikeImage(byte[] data)
    {
        if (data.Length < 12) return false;
        if (data[0] == 0xFF && data[1] == 0xD8) return true;
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return true;
        if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46) return true;
        if (data[0] == 0x42 && data[1] == 0x4D) return true;
        return false;
    }

    /// <summary>模板不可用时的兜底：从零构建规格书（不含模板版式）。</summary>
    private byte[] BuildGgLegacy(RemoteGgModel head, RemoteDocExtra extra)
    {
        using var wb = new XLWorkbook();
        var ws = NewSheet(wb, "规格书");

        Title(ws, "产品规格书", "Product Specification Sheet");

        HeadRow(ws, "产品名称 Part name", Field(extra, RemoteDocKeys.ProductName));
        HeadRow(ws, "产品型号 Part code", Field(extra, RemoteDocKeys.PartCode));
        HeadRow(ws, "客户 Customer", Field(extra, RemoteDocKeys.Customer));
        HeadRow(ws, "公司 Company", Field(extra, RemoteDocKeys.Company) ?? Company);

        Note(ws, "保密要求：未经同意，不得向第三方透露 (Confidential — do not disclose to third parties without consent)");

        // 变更履历 ECN List
        Section(ws, "变更履历表 ECN List");
        SubHeader(ws, "变更日期 Updated on", "版本 Rev.", "变更人 Updated by", "变更内容 Description of Change");
        if (extra.ChangeLog.Count == 0)
        {
            DataRow(ws, "", "", "", "");
        }
        else
        {
            foreach (var log in extra.ChangeLog)
            {
                var r = ws.LastRowUsed()!.RowNumber() + 1;
                ws.Cell(r, 1).Value = log.Date ?? "";
                ws.Cell(r, 2).Value = log.Rev ?? "";
                ws.Cell(r, 3).Value = log.By ?? "";
                ws.Cell(r, 4).Value = $"{(log.Before ?? "")} → {(log.After ?? "")}";
                ws.Range(r, 4, r, 5).Merge();
                Body(ws.Range(r, 1, r, 5));
            }
        }

        AppendSpecSections(ws, extra, includeSampleChecks: false);
        Finish(ws);
        return Save(wb);
    }

    /// <summary>模板不可用时的兜底：从零构建承认书（不含模板版式）。</summary>
    private byte[] BuildCrLegacy(RemoteCrModel head, RemoteDocExtra extra)
    {
        using var wb = new XLWorkbook();
        var ws = NewSheet(wb, "承认书");

        Title(ws, "样品承认书", "Sample Approval Sheet");

        HeadRow(ws, "产品名称 Part name", Field(extra, RemoteDocKeys.ProductName));
        HeadRow(ws, "产品型号 Part code", Field(extra, RemoteDocKeys.PartCode));
        HeadRow(ws, "客户 Customer", Field(extra, RemoteDocKeys.Customer));
        HeadRow(ws, "公司 Company", Field(extra, RemoteDocKeys.Company) ?? Company);

        // 样品检验结果
        Section(ws, "样品检验结果 Sample Inspection Result");
        var hr = ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(hr, 1).Value = "检验项目 Inspection Item";
        ws.Range(hr, 1, hr, 3).Merge();
        ws.Cell(hr, 4).Value = "结论 Conclusion";
        ws.Range(hr, 4, hr, 5).Merge();
        Body(ws.Range(hr, 1, hr, 5));

        foreach (var item in extra.SampleChecks)
        {
            var r = ws.LastRowUsed()!.RowNumber() + 1;
            ws.Cell(r, 1).Value = item.Name ?? "";
            ws.Range(r, 1, r, 3).Merge();
            ws.Cell(r, 4).Value = string.IsNullOrWhiteSpace(item.Result) ? "" : item.Result;
            ws.Range(r, 4, r, 5).Merge();
            Body(ws.Range(r, 1, r, 5));
        }

        AppendSpecSections(ws, extra, includeSampleChecks: false);
        Finish(ws);
        return Save(wb);
    }

    /// <summary>
    /// 立项单导出：优先套用 wwwroot/Templates/遥控器立项单.xlsx 模板，
    /// 只把数据写进对应单元格 —— 公司 logo、表单编号、保存期限、字体边框全部原样保留。
    /// 模板缺失或损坏时回退到 <see cref="BuildLxLegacy"/>。
    /// </summary>
    public byte[] BuildLx(RemoteLxModel lx)

    {

        var filled = TryBuildLxFromTemplate(lx);

        if (filled is not null) return filled;



        _logger.LogWarning("遥控器立项单模板导出未生效，已回退到内置版式：{Reason}",

            ExcelTemplateWriter.LastError ?? "未知原因");

        return BuildLxLegacy(lx);

    }



    /// <summary>遥控器立项单模板坐标 ← 单据字段（17 项与模板一一对应）。</summary>

    private byte[]? TryBuildLxFromTemplate(RemoteLxModel lx)

    {

        var cells = new Dictionary<string, string?>

        {

            // 表头：需求人 / 提出需求日期 / 要求完成日期 / 客户

            ["B5"] = lx.DemandPerson,

            ["D5"] = lx.TimeToRequest?.ToString("yyyy-MM-dd"),

            ["F5"] = lx.RequiredCompletionTime?.ToString("yyyy-MM-dd"),

            ["B6"] = lx.Customer,



            // 1~17：与遥控器模板左侧序号一一对应

            ["C8"] = lx.MotorModelSpec,

            ["C9"] = lx.Description,

            ["C10"] = lx.RemoteSize,

            ["C11"] = lx.BracketSize,

            ["C12"] = lx.RemoteWeight,

            ["C13"] = lx.RemoteColor,

            ["C14"] = lx.BatteryType,

            ["C15"] = Join(lx.ChargingMethod, lx.ChargingMethodNo),

            ["C16"] = lx.TransmitPower,

            ["C17"] = JoinComma(lx.RemoteFrequency, lx.RemoteFrequencyOther),

            ["C18"] = lx.RemoteTechnology,

            ["C19"] = JoinComma(lx.RemoteProtocol, lx.RemoteProtocolOther),

            ["C20"] = lx.ChannelQuantity,

            ["C21"] = lx.WorkingTemperature,

            ["C22"] = lx.RemoteDistanceIndoor,

            ["C23"] = lx.RemoteDistanceOutdoor,

            ["C24"] = lx.Application,

            ["C25"] = JoinComma(lx.IpLevel, lx.IpLevelOther)

        };



        // 充电口 / 频率 / 遥控技术 / 协议 这几行模板里印着可选项，按候选整排画勾

        var checks = new Dictionary<string, string?>

        {

            ["C15"] = lx.ChargingMethod,

            ["C17"] = lx.RemoteFrequency,

            ["C18"] = lx.RemoteTechnology

        };

        var candidates = new Dictionary<string, string?>

        {

            ["C15"] = "None|Micro-USB|Type-C",

            ["C17"] = "433.92 MHz|447.x MHz|868.x MHz|Other",

            ["C18"] = "单向Single-way|双向Bi-directional"

        };



        return ExcelTemplateWriter.TryFill(TemplatePath("遥控器立项单.xlsx"), cells, checks, null, candidates, out var bytes)

            ? bytes

            : null;

    }



    /// <summary>值里点到了这个选项就返回打勾方框 ☑，否则空方框 ☐。</summary>

    private static string Box(string? value, string option)

        => !string.IsNullOrWhiteSpace(value) && value.Contains(option, StringComparison.OrdinalIgnoreCase)

            ? "☑"

            : "☐";



    /// <summary>立项单内置版式（模板不可用时的回退）。</summary>

    private byte[] BuildLxLegacy(RemoteLxModel lx)

    {

        using var wb = new XLWorkbook();

        var ws = wb.AddWorksheet("遥控器立项单");



        ws.Cell(1, 1).Value = "遮阳驱动立项申请输入项目清单";

        ws.Cell(1, 1).Style.Font.Bold = true;

        ws.Cell(1, 1).Style.Font.FontSize = 16;



        void HeadRow(int row, string label, string? value)

        {

            ws.Cell(row, 1).Value = label;

            ws.Cell(row, 2).Value = value ?? "";

        }



        HeadRow(2, "需求人", lx.DemandPerson);

        HeadRow(3, "提出需求日期", lx.TimeToRequest?.ToString("yyyy-MM-dd"));

        HeadRow(4, "要求完成日期", lx.RequiredCompletionTime?.ToString("yyyy-MM-dd"));

        HeadRow(5, "客户", lx.Customer);

        HeadRow(6, "客户产品编号", lx.CustomerPartCode);



        HeadRow(8, "1 型号 Model No.", lx.MotorModelSpec);

        HeadRow(9, "2 描述 Description", lx.Description);

        HeadRow(10, "3 遥控器尺寸 Remote Size", lx.RemoteSize);

        HeadRow(11, "4 支架尺寸 Bracket Size", lx.BracketSize);

        HeadRow(12, "5 遥控器重量 Remote Weight", lx.RemoteWeight);

        HeadRow(13, "6 遥控器颜色 Remote Color", lx.RemoteColor);

        HeadRow(14, "7 电池型号 Battery Type", lx.BatteryType);

        HeadRow(15, "8 充电口要求 Charging port", Join(lx.ChargingMethod, lx.ChargingMethodNo));

        HeadRow(16, "9 发射功率 Transmit Power", lx.TransmitPower);

        HeadRow(17, "10 频率 Radio Frequency", JoinComma(lx.RemoteFrequency, lx.RemoteFrequencyOther));

        HeadRow(18, "11 遥控技术 Radio Mode", lx.RemoteTechnology);

        HeadRow(19, "12 遥控协议 Protocol", JoinComma(lx.RemoteProtocol, lx.RemoteProtocolOther));

        HeadRow(20, "13 通道数量 Channel Quantity", lx.ChannelQuantity);

        HeadRow(21, "14 工作温度 Working Temperature", lx.WorkingTemperature);

        HeadRow(22, "15 传输距离-室内 Indoor", lx.RemoteDistanceIndoor);

        HeadRow(23, "15 传输距离-室外 Outdoor", lx.RemoteDistanceOutdoor);

        HeadRow(24, "16 应用场景 Application", lx.Application);

        HeadRow(25, "17 防水等级 IP Degree", JoinComma(lx.IpLevel, lx.IpLevelOther));



        ws.Column(1).Width = 42;

        ws.Column(2).Width = 50;

        return Save(wb);

    }





    private void AppendSpecSections(IXLWorksheet ws, RemoteDocExtra extra, bool includeSampleChecks)
    {
        Section(ws, "一、产品功性能要求及参数规格表");
        SubHeader(ws, "参数名称 Parameter", "", "", "参数说明 Parameter Description");
        HeadRow(ws, "型号 Model No.", Field(extra, RemoteDocKeys.ModelNo));
        HeadRow(ws, "遥控器尺寸 Remote Size", Field(extra, RemoteDocKeys.RemoteSize));
        HeadRow(ws, "支架尺寸 Bracket Size", Field(extra, RemoteDocKeys.BracketSize));
        HeadRow(ws, "遥控器重量 Remote Weight", Field(extra, RemoteDocKeys.RemoteWeight));
        HeadRow(ws, "遥控器颜色 Remote Color", Field(extra, RemoteDocKeys.RemoteColor));
        HeadRow(ws, "电池型号 Battery Type", Join(Field(extra, RemoteDocKeys.BatteryType), Field(extra, RemoteDocKeys.BatteryTypeOther)));
        HeadRow(ws, "充电口要求 Charging port", Join(Field(extra, RemoteDocKeys.RechargePort), Field(extra, RemoteDocKeys.RechargePortOther)));
        HeadRow(ws, "电池续航时间 Battery Service Life", Field(extra, RemoteDocKeys.BatteryEndurance));
        HeadRow(ws, "发射功率 Transmit Power", Field(extra, RemoteDocKeys.TransmitPower));
        HeadRow(ws, "遥控技术 Radio Mode", Field(extra, RemoteDocKeys.RadioMode));
        HeadRow(ws, "遥控频率 Radio Frequency", Join(Field(extra, RemoteDocKeys.RadioFrequency), Field(extra, RemoteDocKeys.RfOther)));
        HeadRow(ws, "遥控技术 Radio Mode", Field(extra, RemoteDocKeys.RadioModeSecond));
        HeadRow(ws, "遥控协议 Protocol", Join(Field(extra, RemoteDocKeys.Protocol), Field(extra, "protocolOther")));
        HeadRow(ws, "通道数量 Channel Quantity", Field(extra, RemoteDocKeys.ChannelQuantity));
        HeadRow(ws, "传输距离-室内 Indoor", Field(extra, RemoteDocKeys.RfDistanceIndoor));
        HeadRow(ws, "传输距离-室外 Outdoor", Field(extra, RemoteDocKeys.RfDistanceOutdoor));
        HeadRow(ws, "产品应用场合 Application Occasions", Join(Field(extra, RemoteDocKeys.Applications), Field(extra, RemoteDocKeys.ApplicationsOther)));
        HeadRow(ws, "丝印或镭雕图纸 Surface printing drawing", Field(extra, RemoteDocKeys.SurfacePrint));
        HeadRow(ws, "认证要求 Certification Requirements", Join(Field(extra, RemoteDocKeys.Certification), Field(extra, RemoteDocKeys.CertificationOther)));
        HeadRow(ws, "质保期限 Warranty Period", Field(extra, RemoteDocKeys.Warranty));
        HeadRow(ws, "产品特殊功能 Special Function", Field(extra, RemoteDocKeys.SpecialFunction));
        HeadRow(ws, "客户特殊要求 Other Requirement", Field(extra, RemoteDocKeys.OtherRequirement));

        Section(ws, "二、产品使用工况 Product Operating Conditions");
        HeadRow(ws, "使用环境 Application", Join(Field(extra, RemoteDocKeys.UsageEnv), Field(extra, "usageOther")));
        HeadRow(ws, "IP 等级 IP Degree", Join(Field(extra, RemoteDocKeys.IpDegree), Field(extra, RemoteDocKeys.IpOther)));
        HeadRow(ws, "工作温度 Working Temperature", Field(extra, RemoteDocKeys.WorkingTemp));
        HeadRow(ws, "静电防护等级 ESD Protection Level", Field(extra, RemoteDocKeys.Esd));

        Section(ws, "三、选配配件 Optional Accessories");
        SubHeader(ws, "序号 No.", "零件 Component", "型号 Type", "备注 Remark");
        foreach (var (row, i) in extra.Accessories.Select((x, i) => (x, i)))
        {
            var r = ws.LastRowUsed()!.RowNumber() + 1;
            ws.Cell(r, 1).Value = (i + 1).ToString("00");
            ws.Cell(r, 2).Value = row.Name ?? "";
            ws.Cell(r, 3).Value = row.Type ?? "";
            ws.Cell(r, 4).Value = row.Remark ?? "";
            ws.Range(r, 4, r, 5).Merge();
            Body(ws.Range(r, 1, r, 5));
        }

        Section(ws, "四、产品尺寸图 Product Dimension Drawing");
        HeadRow(ws, "外形尺寸图 Overall Dimension Drawing", "（见随附图纸）");
        HeadRow(ws, "电控系统图 Electrical Control System Diagram", "（见随附图纸）");

        Section(ws, "五、产品附件 Production Accessories");
        SubHeader(ws, "序号 No.", "零件 Component", "型号 Type", "备注 Remark");
        foreach (var (row, i) in extra.Attachments.Select((x, i) => (x, i)))
        {
            var r = ws.LastRowUsed()!.RowNumber() + 1;
            ws.Cell(r, 1).Value = (i + 1).ToString("00");
            ws.Cell(r, 2).Value = row.Name ?? "";
            ws.Cell(r, 3).Value = row.Type ?? "";
            ws.Cell(r, 4).Value = row.Remark ?? "";
            ws.Range(r, 4, r, 5).Merge();
            Body(ws.Range(r, 1, r, 5));
        }

        Section(ws, "六、包装 Package");
        SubHeader(ws, "项目 Item", "尺寸 Size(mm)", "", "材质 Material", "内含数量 Package Qty");
        foreach (var row in extra.Packages)
        {
            var r = ws.LastRowUsed()!.RowNumber() + 1;
            ws.Cell(r, 1).Value = row.Name ?? "";
            ws.Cell(r, 2).Value = row.Size ?? "";
            ws.Cell(r, 3).Value = row.Material ?? "";
            ws.Cell(r, 4).Value = row.Qty ?? "";
            Body(ws.Range(r, 1, r, 4));
        }

        Section(ws, "七、产品操作说明 Product Operating Instructions");
        foreach (var (code, label) in OperationRows)
        {
            if (code.Length == 0)
            {
                var sr = ws.LastRowUsed()!.RowNumber() + 1;
                ws.Cell(sr, 1).Value = label;
                ws.Range(sr, 1, sr, 5).Merge();
                var sRange = ws.Range(sr, 1, sr, 5);
                sRange.Style.Font.Bold = true;
                sRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                sRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                continue;
            }
            extra.Operations.TryGetValue(code, out var value);
            HeadRow(ws, label, value);
        }

        if (includeSampleChecks && extra.SampleChecks.Count > 0)
        {
            Section(ws, "样品检验 Sample Checks");
            SubHeader(ws, "序号", "检验项目", "结果", "备注");
            foreach (var row in extra.SampleChecks)
            {
                var r = ws.LastRowUsed()!.RowNumber() + 1;
                ws.Cell(r, 1).Value = row.Code ?? "";
                ws.Cell(r, 2).Value = row.Name ?? "";
                ws.Cell(r, 3).Value = row.Result ?? "";
                ws.Cell(r, 4).Value = row.Remark ?? "";
                ws.Range(r, 4, r, 5).Merge();
                Body(ws.Range(r, 1, r, 5));
            }
        }
    }

    private static readonly (string Code, string Label)[] OperationRows =
    [
        ("", "① 控制器按键与运行 Controller Keys & Operation"),
        ("keyHandler", "按键与通道定义 Key & Channel Definition"),
        ("lcdIcon", "液晶显示/图标说明 LCD Display / Icon Description"),
        ("lowPower", "低功耗/待机 Low Power / Standby"),
        ("", "② 通道与群组 Channels & Groups"),
        ("channelSwitch", "通道/群组切换 Channel / Group Switching"),
        ("setChannelCount", "设置通道数量 Set Channel Count"),
        ("setGroupCount", "设置群组数量 Set Group Count"),
        ("groupChannels", "配置群组内通道 Configure Channels in a Group"),
        ("masterChannel", "群控通道(0通道)说明 Master Channel (CH0)"),
        ("", "③ 对码与删码 Pairing & Unpairing"),
        ("addPairing", "添码(滚码/配对) Add to Motor"),
        ("delPairing", "删码(单删/全删) Delete Code"),
        ("", "④ 对电机的行程操作 Limit Operations on Motor"),
        ("direction", "换向 Direction Reversal"),
        ("setLimits", "设置行程(上/下限位) Set Limits"),
        ("delLimits", "删除行程 Delete Limits"),
        ("favorite", "最佳行程点 Favorite Position"),
        ("", "⑤ 运行控制与安全 Run Control & Safety"),
        ("jogMode", "点动/连动模式切换 Jog / Continuous"),
        ("childLock", "童锁 Child Lock"),
        ("", "⑥ 网络与复位 Network & Reset"),
        ("networkReset", "重置电机网络 Reset Motor Network"),
        ("motorFactoryReset", "电机恢复出厂 Factory Reset the Motor"),
        ("factoryReset", "遥控器恢复出厂 Factory Reset the Remote")
    ];

    // ==================== 版式工具 ====================

    private static IXLWorksheet NewSheet(XLWorkbook wb, string name)
    {
        var ws = wb.Worksheets.Add(name);
        ws.Column(1).Width = 26;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 26;
        ws.Column(4).Width = 26;
        ws.Column(5).Width = 26;
        ws.Style.Font.FontName = "微软雅黑";
        ws.Style.Font.FontSize = 9;
        return ws;
    }

    private static void Title(IXLWorksheet ws, string cn, string en)
    {
        var r1 = ws.LastRowUsed() is null ? 1 : ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(r1, 1).Value = cn;
        ws.Range(r1, 1, r1, 5).Merge();
        var r2 = r1 + 1;
        ws.Cell(r2, 1).Value = en;
        ws.Range(r2, 1, r2, 5).Merge();
        foreach (var r in new[] { r1, r2 })
        {
            var range = ws.Range(r, 1, r, 5);
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = r == r1 ? 15 : 10;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Row(r).Height = r == r1 ? 28 : 18;
        }
    }

    private static void Section(IXLWorksheet ws, string text)
    {
        var r = ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(r, 1).Value = text;
        ws.Range(r, 1, r, 5).Merge();
        var range = ws.Range(r, 1, r, 5);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        range.Style.Alignment.WrapText = true;
        ws.Row(r).Height = 20;
    }

    private static void Note(IXLWorksheet ws, string text)
    {
        var r = ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(r, 1).Value = text;
        ws.Range(r, 1, r, 5).Merge();
        var range = ws.Range(r, 1, r, 5);
        range.Style.Font.Italic = true;
        range.Style.Font.FontSize = 8;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    private static void HeadRow(IXLWorksheet ws, string label, string? value)
    {
        var r = ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(r, 1).Value = label;
        ws.Range(r, 1, r, 2).Merge();
        ws.Cell(r, 3).Value = value ?? "";
        ws.Range(r, 3, r, 5).Merge();
        Body(ws.Range(r, 1, r, 5));
    }

    private static void SubHeader(IXLWorksheet ws, params string[] cells)
    {
        var r = ws.LastRowUsed()!.RowNumber() + 1;
        for (var i = 0; i < cells.Length && i < 5; i++)
            ws.Cell(r, i + 1).Value = cells[i];
        if (cells.Length < 5)
            ws.Range(r, Math.Max(1, cells.Length), r, 5).Merge();
        var range = ws.Range(r, 1, r, 5);
        Body(range);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void DataRow(IXLWorksheet ws, string c1, string c2, string c3, string c4)
    {
        var r = ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(r, 1).Value = c1;
        ws.Cell(r, 2).Value = c2;
        ws.Cell(r, 3).Value = c3;
        ws.Cell(r, 4).Value = c4;
        ws.Range(r, 4, r, 5).Merge();
        Body(ws.Range(r, 1, r, 5));
    }

    private static void Body(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Alignment.WrapText = true;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void Finish(IXLWorksheet ws)
    {
        ws.SheetView.FreezeRows(2);
        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.Margins.Top = 0.4;
        ws.PageSetup.Margins.Bottom = 0.4;
        ws.PageSetup.Margins.Left = 0.4;
        ws.PageSetup.Margins.Right = 0.4;
    }

    private static byte[] Save(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ==================== 小工具 ====================

    private static string? Field(RemoteDocExtra extra, string key)
        => extra.Fields.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

    private static string? Join(params string?[] parts)
    {
        var list = parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).ToList();
        return list.Count == 0 ? null : string.Join(" / ", list);
    }

    /// <summary>多选字段用逗号连接：勾选逻辑按逗号拆项，用「 / 」连会把「其它」值当成一项吞掉。</summary>
    private static string? JoinComma(params string?[] parts)
    {
        var list = parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).ToList();
        return list.Count == 0 ? null : string.Join(",", list);
    }

    /// <summary>模板里有固定句式的格子：值空着就返回 null，保留模板原文。</summary>
    private static string? Format(string template, string? value)
        => string.IsNullOrWhiteSpace(value) ? null : string.Format(template, value.Trim());

    /// <summary>内置锂电：是/否 + 节数 + 容量，模板原文是「□是Yes □否No 2节cell，2600 mAh」。</summary>
    private static string? Lithium(RemoteDocExtra extra)
    {
        var flag = Field(extra, RemoteDocKeys.InternalBattery) ?? "";
        var cells = Field(extra, "batteryCells");
        var mah = Field(extra, "batteryMah");
        if (flag.Length == 0 && cells is null && mah is null) return null;

        var yes = flag.Contains("是", StringComparison.Ordinal);
        var no = flag.Contains("否", StringComparison.Ordinal);
        if (!yes && !no) yes = true;

        var head = $"{(yes ? "☑" : "☐")}是 Yes  {(no ? "☑" : "☐")}否 No";
        var detail = Join(cells is null ? null : cells + " 节 cell", mah is null ? null : mah + " mAh");
        return detail is null ? head : head + "    " + detail.Replace(" / ", "，");
    }

    /// <summary>成品遥控器电池续航测试：四行说明，模板里本来就是一整格多行文本。</summary>
    private static string? BatteryTest(RemoteDocExtra extra)
    {
        var load = Field(extra, "batteryTestLoad");
        var duty = Field(extra, "batteryTestDuty");
        var height = Field(extra, "batteryTestHeight");
        var cycles = Field(extra, "batteryTestCycles");
        if (load is null && duty is null && height is null && cycles is null) return null;

        var lines = new List<string>();
        if (load is not null) lines.Add("负载Load：" + load);
        if (duty is not null) lines.Add("工作制Duty Cycle：" + duty);
        if (height is not null) lines.Add("运行高度Travel Height：" + height);
        if (cycles is not null) lines.Add("运行次数Operation Cycles：" + cycles);
        return string.Join("\n", lines);
    }

    /// <summary>线材长度：长度 + 有无特殊要求。</summary>
    private static string? CordLength(RemoteDocExtra extra)
    {
        var length = Field(extra, RemoteDocKeys.CordLength);
        var other = Field(extra, "cordLengthOther");
        if (length is null && other is null) return null;

        var lines = new List<string>();
        if (length is not null) lines.Add($"线材长度Cord Length {length} 米M");
        if (other is not null) lines.Add("有无特殊要求Other requirement：" + other);
        return string.Join("\n", lines);
    }

    /// <summary>外管：直径 / 颜色 / 长度限制，只写填了的部分。</summary>
    private static string? Tube(RemoteDocExtra extra)
    {
        var diameter = Field(extra, "tubeDiameter");
        var color = Field(extra, "tubeColor");
        var limit = Field(extra, "tubeLengthLimit");
        if (diameter is null && color is null && limit is null) return null;

        var parts = new List<string>();
        if (diameter is not null) parts.Add($"直径Diameter {diameter}mm");
        if (color is not null) parts.Add($"颜色Color {color}");
        if (limit is not null) parts.Add($"长度限制lengthlimit {limit}mm");
        return string.Join("，", parts);
    }

    private static string Sanitize(string raw)
    {
        var text = raw.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            text = text.Replace(c, '_');
        return text.Replace('/', '_').Replace('\\', '_');
    }
}


