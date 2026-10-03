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

        Set(ws, 23, 2, main.Product_partNo);
        Set(ws, 23, 4, main.ProductName);
        Set(ws, 23, 12, main.SoftwareVersion);
        Set(ws, 25, 2, main.IssuanceDate.ToString("yyyy-MM-dd"));
        Set(ws, 25, 4, main.SampleQuantity.ToString());
        Set(ws, 25, 12, main.DeliveryTimes.ToString());
        Set(ws, 27, 2, main.program_name);
        Set(ws, 27, 4, main.program_code);
        Set(ws, 27, 12, main.check_code);

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
        SetWrapped(ws, npoiRow, 1, model?.Specification);
        SetWrapped(ws, npoiRow, 3, model?.Criteria);
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

    /// <summary>NPOI 0-based → ClosedXML 1-based。</summary>
    private static void Set(IXLWorksheet ws, int npoiRow, int npoiCol, string? value)
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        cell.Value = value ?? "";
    }

    private static void SetWrapped(IXLWorksheet ws, int npoiRow, int npoiCol, string? value)
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        cell.Value = ToExcelText(value);
        cell.Style.Alignment.WrapText = true;
    }

    private static string ToExcelText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        var result = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        return result.Replace("\r\n", "\n").Replace('\r', '\n');
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
