using System.Text.RegularExpressions;
using ClosedXML.Excel;
using ZYQDManager.DataModels;

namespace ZYQDManager.Services;

/// <summary>
/// 检测报告 Excel 填充（对齐老系统 SetSIRPDF），再交给 LibreOffice 转 PDF。
/// </summary>
public class SirExcelService
{
    private static readonly (string ProductType, string SpecType)[] MotorSpecCaps =
    [
        ("JCA-无RF机械行程交流管状电机", "M.12"),
        ("JCA-内置RF控制机械行程交流管状电机", "M.21"),
        ("JCA-内置RF控制电子行程交流管状电机", "M.22"),
        ("JCA-无RF电子行程交流管状电机", "M.19"),
        ("JCC-锂电池款开合帘电机", "M.23"),
        ("JCC-内置开关电源款开合帘电机", "M.22"),
        ("JCC-内置开关电源款开合帘电机", "M.25"),
        ("JCD-TE款直流管状电机", "M.24"),
        ("JCD-AE款直流管状电机", "M.24"),
        ("JCD-LE款直流管状电机", "M.26"),
        ("常规JCV系列管状电机", "M.21"),
        ("内置锂电池款JCV系列管状电机", "M.24"),
        ("内置开关电源款JCV系列管状电机", "M.23"),
        ("基础版电机", "M.24"),
        ("一次电池款双向遥控器", "M.43"),
        ("一次电池款单向遥控器", "M.39"),
        ("充电款双向遥控器", "M.44"),
        ("充电款单向遥控器", "M.40"),
        ("单推杆遮阳棚", "M.32"),
        ("双推杆遮阳棚", "M.37"),
    ];

    private static readonly (int Row, string Code)[] RemoteDetailRows =
    [
        (46, "1.1"), (47, "1.2"), (48, "1.3"), (49, "1.4"), (50, "1.5"), (51, "1.6"), (52, "1.7"),
        (54, "2.1"), (55, "2.2"), (56, "2.3"), (57, "2.4"), (58, "2.5"), (59, "2.6"), (60, "2.7"),
        (61, "2.8"), (62, "2.9"), (63, "2.10"),
        (65, "3.1"), (66, "3.2"), (67, "3.3"), (68, "3.4"),
        (70, "4.1"),
    ];

    private readonly IWebHostEnvironment _env;

    public SirExcelService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public byte[] Build(ZyqdSirMainModel main, IReadOnlyList<ZyqdSirDetailModel> details)
    {
        ArgumentNullException.ThrowIfNull(main);
        var path = ResolveTemplatePath(main.Report_type);
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet(1);
        FillHeader(ws, main);

        if (UsesMotorTemplate(main.Report_type))
            FillMotorDetails(ws, main, details);
        else
            FillRemoteDetails(ws, details);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public static string DownloadExcelName(ZyqdSirMainModel main)
    {
        var client = SanitizeFilePart(main.Client);
        var part = SanitizeFilePart(main.Product_partNo);
        var bits = new[] { "遮阳检验报告", client, part }.Where(x => x.Length > 0);
        return string.Join("_", bits) + ".xlsx";
    }

    public static string DownloadPdfName(ZyqdSirMainModel main)
    {
        var name = DownloadExcelName(main);
        return name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? name[..^5] + ".pdf"
            : name + ".pdf";
    }

    public static bool UsesMotorTemplate(string? reportType)
    {
        var t = (reportType ?? "").Trim();
        return t is "motor" or "remote_new" or "putter";
    }

    private void FillHeader(IXLWorksheet ws, ZyqdSirMainModel main)
    {
        Set(ws, 14, 7, main.Appearance_Approval_Report ? "√" : "");
        Set(ws, 16, 7, main.Dimension_Report ? "√" : "");
        Set(ws, 18, 7, main.Material_Report ? "√" : "");
        Set(ws, 20, 7, main.Function_Report ? "√" : "");
        if (!main.Appearance_Approval_Report && !main.Dimension_Report && !main.Material_Report && !main.Function_Report)
            Set(ws, 20, 7, "√");

        Set(ws, 15, 2, "sales");
        Set(ws, 16, 2, CleanSalesman(main.SalesmanName));
        Set(ws, 19, 2, main.Client);
        Set(ws, 20, 2, main.ClientDepartment);
        Set(ws, 21, 2, main.ContactName);

        // 产品信息整块（型号/名称/版本、日期/数量/送样、程序名/代码/校验码）：
        // 模板均为竖向合并格，C 列单格很窄；统一软换行（优先 '-'）+ 抬行高，对齐老系统排版。
        const int wNarrow = 11; // C 列（1 格宽）
        const int wMid = 18;    // E:G（3 格宽）
        const int wWide = 22;   // M:P（4 格宽）
        SetSoftWrapped(ws, 23, 2, main.Product_partNo, wNarrow);
        SetSoftWrapped(ws, 23, 4, main.ProductName, wMid);
        SetSoftWrapped(ws, 23, 12, main.SoftwareVersion, wWide);
        SetSoftWrapped(ws, 25, 2, main.IssuanceDate.ToString("yyyy-MM-dd"), wNarrow);
        SetSoftWrapped(ws, 25, 4, main.SampleQuantity.ToString(), wMid);
        SetSoftWrapped(ws, 25, 12, main.DeliveryTimes.ToString(), wWide);
        SetSoftWrapped(ws, 27, 2, main.program_name, wNarrow);
        SetSoftWrapped(ws, 27, 4, main.program_code, wMid);
        SetSoftWrapped(ws, 27, 12, main.check_code, wWide);

        const int hang = 2;
        Set(ws, 30 + hang, 0, main.UL ? "√" : "");
        Set(ws, 31 + hang, 0, main.FCC ? "√" : "");
        Set(ws, 32 + hang, 0, main.CE ? "√" : "");
        Set(ws, 33 + hang, 0, main.RoHS ? "√" : "");
        Set(ws, 34 + hang, 0, main.REACH ? "√" : "");
        Set(ws, 35 + hang, 0, main.Certification_None ? "√" : "");

        Set(ws, 30 + hang, 9, main.Test_Engineer);
        Set(ws, 32 + hang, 9, main.Test_Checked_by);
        Set(ws, 34 + hang, 9, main.Test_Approved_By);
    }

    private static void FillRemoteDetails(IXLWorksheet ws, IReadOnlyList<ZyqdSirDetailModel> details)
    {
        foreach (var (row, code) in RemoteDetailRows)
            InsertDetail(ws, row, details, code, motor: false);
    }

    private static void FillMotorDetails(
        IXLWorksheet ws,
        ZyqdSirMainModel main,
        IReadOnlyList<ZyqdSirDetailModel> details)
    {
        var product = (main.product_type ?? "").Trim();
        var cap = MotorSpecCaps.FirstOrDefault(x => x.ProductType == product);
        if (string.IsNullOrEmpty(cap.SpecType))
            return;
        if (!int.TryParse(cap.SpecType.Replace("M.", "", StringComparison.OrdinalIgnoreCase), out var maxI) || maxI < 1)
            return;

        // 模板已有第 46 行（NPOI），再插入 maxI-1 行
        var insertCount = Math.Max(0, maxI - 1);
        if (insertCount > 0)
            ws.Row(47).InsertRowsBelow(insertCount);

        for (var i = 1; i <= maxI; i++)
            InsertDetail(ws, 45 + i, details, $"M.{i}", motor: true);
    }

    private static void InsertDetail(
        IXLWorksheet ws,
        int npoiRow,
        IReadOnlyList<ZyqdSirDetailModel> details,
        string code,
        bool motor)
    {
        var model = details.FirstOrDefault(x =>
            string.Equals(x.Specification_type, code, StringComparison.OrdinalIgnoreCase));

        Set(ws, npoiRow, 0, code);
        // 与老系统 SetMergedCellValue 一致：Specification=B:C、Criteria=D:E（各合并 2 列）
        // 插入的明细行模板不会自带合并，不合并时文字只挤在窄列里异常换行
        SetMergedWrapped(ws, npoiRow, 1, model?.Specification);
        SetMergedWrapped(ws, npoiRow, 3, model?.Criteria);
        Set(ws, npoiRow, 5, model?.Actual_Results1);
        Set(ws, npoiRow, 6, model?.Actual_Results2);
        Set(ws, npoiRow, 7, model?.Actual_Results3);
        Set(ws, npoiRow, 8, model?.Actual_Results4);
        Set(ws, npoiRow, 9, model?.Actual_Results5);
        Set(ws, npoiRow, 10, model?.Actual_Results6);
        Set(ws, npoiRow, 11, model?.Conclusion_A);
        Set(ws, npoiRow, 12, model?.Conclusio_B);
        Set(ws, npoiRow, 13, model?.Conclusion_C);
        if (motor)
        {
            Set(ws, npoiRow, 14, model?.Conclusion_D);
            Set(ws, npoiRow, 15, model?.Note);
        }
    }

    private string ResolveTemplatePath(string? reportType)
    {
        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var dir = Path.Combine(webRoot, "Templates");
        var name = UsesMotorTemplate(reportType)
            ? "遮阳检验报告-电机测试新.xlsx"
            : "遮阳检验报告-遥控器测试新.xlsx";
        var path = Path.Combine(dir, name);
        if (File.Exists(path))
            return path;

        // 容错：按关键字匹配目录内文件
        if (Directory.Exists(dir))
        {
            var key = UsesMotorTemplate(reportType) ? "电机" : "遥控";
            var hit = Directory.GetFiles(dir, "*.xlsx")
                .FirstOrDefault(f => Path.GetFileName(f).Contains(key, StringComparison.OrdinalIgnoreCase)
                                     && Path.GetFileName(f).Contains("检验", StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
                return hit;
        }

        throw new FileNotFoundException("未找到检测报告模板，请确认文件在 wwwroot/Templates/" + name);
    }

    /// <summary>导出用勾号。库内/界面习惯写 √(U+221A)，LO 转 PDF 会画成数学根号，故改为 ✓。</summary>
    private const string CheckMark = "\u2713";

    /// <summary>NPOI 0-based → ClosedXML 1-based。</summary>
    private static void Set(IXLWorksheet ws, int npoiRow, int npoiCol, string? value)
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        var text = NormalizeExportMarks(value);
        cell.Value = text;
        // 保证 ✓ 有字形；宋体等对 √ 会显示成根号
        if (text == CheckMark)
            cell.Style.Font.FontName = "Segoe UI Symbol";
    }

    private static void SetWrapped(IXLWorksheet ws, int npoiRow, int npoiCol, string? value)
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        cell.Value = ToExcelText(value);
        cell.Style.Alignment.WrapText = true;
    }

    /// <summary>
    /// 写入可换行文本：优先在 '-' 处软换行（模仿老系统 Excel 排版），并按行数抬高合并区域行高。
    /// </summary>
    private static void SetSoftWrapped(
        IXLWorksheet ws, int npoiRow, int npoiCol, string? value, int maxLineWidth)
    {
        var text = SoftWrapPreferHyphen(ToExcelText(value), maxLineWidth);
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        cell.Value = text;
        cell.Style.Alignment.WrapText = true;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        var lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
        var totalHeight = Math.Max(27.0, lineCount * 13.5);
        ExpandMergedRowHeight(ws, cell, totalHeight);
    }

    private static void ExpandMergedRowHeight(IXLWorksheet ws, IXLCell cell, double totalHeight)
    {
        var merge = ws.MergedRanges.FirstOrDefault(m => m.Contains(cell));
        if (merge is null)
        {
            ws.Row(cell.Address.RowNumber).Height =
                Math.Max(ws.Row(cell.Address.RowNumber).Height, totalHeight);
            return;
        }

        var first = merge.FirstRow().RowNumber();
        var last = merge.LastRow().RowNumber();
        var rows = Math.Max(1, last - first + 1);
        var per = totalHeight / rows;
        for (var r = first; r <= last; r++)
            ws.Row(r).Height = Math.Max(ws.Row(r).Height, per);
    }

    /// <summary>
    /// 按显示宽度折行：ASCII≈1、中日韩≈2；尽量在 '-' 之后断行，避免把型号从中间拆开。
    /// </summary>
    private static string SoftWrapPreferHyphen(string text, int maxLineWidth)
    {
        if (string.IsNullOrEmpty(text) || maxLineWidth < 4)
            return text;
        if (MeasureDisplayWidth(text) <= maxLineWidth)
            return text;

        var lines = new List<string>();
        var line = new System.Text.StringBuilder();
        var lineWidth = 0;
        var i = 0;
        while (i < text.Length)
        {
            var piece = TakeNextPiece(text, i);
            var pieceWidth = MeasureDisplayWidth(piece);

            if (pieceWidth > maxLineWidth)
            {
                if (line.Length > 0)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                    lineWidth = 0;
                }

                foreach (var chunk in HardSplitByWidth(piece, maxLineWidth))
                    lines.Add(chunk);
                i += piece.Length;
                continue;
            }

            if (lineWidth > 0 && lineWidth + pieceWidth > maxLineWidth)
            {
                lines.Add(line.ToString());
                line.Clear();
                lineWidth = 0;
            }

            line.Append(piece);
            lineWidth += pieceWidth;
            i += piece.Length;
        }

        if (line.Length > 0)
            lines.Add(line.ToString());

        return string.Join("\n", lines);
    }

    /// <summary>取下一段：优先到下一个 '-' / '.'（含）为止；否则连续取同类字符。</summary>
    private static string TakeNextPiece(string text, int start)
    {
        var breakAt = -1;
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] is '-' or '.')
            {
                breakAt = i;
                break;
            }
        }

        if (breakAt >= start)
            return text.Substring(start, breakAt - start + 1);

        var end = start + 1;
        var firstCjk = IsWideChar(text[start]);
        while (end < text.Length && text[end] is not ('-' or '.') && IsWideChar(text[end]) == firstCjk)
            end++;
        return text[start..end];
    }

    private static IEnumerable<string> HardSplitByWidth(string text, int maxLineWidth)
    {
        var buf = new System.Text.StringBuilder();
        var w = 0;
        foreach (var ch in text)
        {
            var cw = IsWideChar(ch) ? 2 : 1;
            if (w > 0 && w + cw > maxLineWidth)
            {
                yield return buf.ToString();
                buf.Clear();
                w = 0;
            }

            buf.Append(ch);
            w += cw;
        }

        if (buf.Length > 0)
            yield return buf.ToString();
    }

    private static int MeasureDisplayWidth(string s)
    {
        var w = 0;
        foreach (var ch in s)
            w += IsWideChar(ch) ? 2 : 1;
        return w;
    }

    private static bool IsWideChar(char ch)
        => ch >= 0x2E80 && ch <= 0x9FFF
           || ch >= 0xF900 && ch <= 0xFAFF
           || ch >= 0xFE30 && ch <= 0xFE4F
           || ch >= 0xFF00 && ch <= 0xFFEF;

    /// <summary>
    /// 写入并确保横向合并（默认 2 列），对齐老系统 SetMergedCellValue。
    /// </summary>
    private static void SetMergedWrapped(
        IXLWorksheet ws, int npoiRow, int npoiCol, string? value, int mergeColumnCount = 2)
    {
        if (mergeColumnCount < 2)
        {
            SetWrapped(ws, npoiRow, npoiCol, value);
            return;
        }

        var excelRow = npoiRow + 1;
        var startCol = npoiCol + 1;
        var endCol = startCol + mergeColumnCount - 1;
        var range = ws.Range(excelRow, startCol, excelRow, endCol);

        foreach (var existing in ws.MergedRanges.Where(m => m.Intersects(range)).ToList())
            existing.Unmerge();

        range.Merge();
        var cell = ws.Cell(excelRow, startCol);
        cell.Value = ToExcelText(value);
        cell.Style.Alignment.WrapText = true;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static string NormalizeExportMarks(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        var t = value.Trim();
        // 界面/库内勾选常用 √(U+221A)，导出改为 ✓，避免 PDF 显示成根号
        if (t is "\u221A" or "√" or "✓" or "✔")
            return CheckMark;
        return value;
    }

    private static string ToExcelText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        var result = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        return NormalizeExportMarks(result.Replace("\r\n", "\n").Replace('\r', '\n'));
    }

    private static string CleanSalesman(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return "";
        return name
            .Replace("\t", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(" ", "")
            .Replace("\u00A0", "")
            .Replace("\u200B", "")
            .Replace("\uFEFF", "");
    }

    private static string SanitizeFilePart(string? s)
    {
        var t = (s ?? "").Trim();
        if (t.Length == 0) return "";
        foreach (var c in Path.GetInvalidFileNameChars())
            t = t.Replace(c, '_');
        return t;
    }
}
