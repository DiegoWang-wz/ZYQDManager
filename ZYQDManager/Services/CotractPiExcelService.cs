using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;

namespace ZYQDManager.Services;

public class CotractPiExcelService
{
    private readonly IWebHostEnvironment _env;

    public CotractPiExcelService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public byte[] Build(List<ZyqdCotractPiModel> rows)
    {
        if (rows.Count == 0)
            throw new InvalidOperationException("没有可导出的明细");

        var main = rows[0];
        var type = string.IsNullOrWhiteSpace(main.Cotract_PI_type)
            ? CotractPiTypes.ResolveTemplateType(main.var6, main.ClientNo)
            : main.Cotract_PI_type.Trim();
        if (CotractPiTypes.NormalizeClientNo(main.ClientNo) == "10008831")
            type = "JCK";

        var path = ResolveTemplatePath(type);
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet(1);
        var curr = CotractPiTypes.CurrencySymbol(main.currency_unit);
        var english = type == "JS";

        if (english)
            FillEnglish(ws, main, rows, curr);
        else
            FillChinese(ws, main, rows, curr);

        ApplyInvoiceTitle(ws, english, main.var6);

        var extra = Math.Max(0, rows.Count - 1) * 6;
        ApplyTradeFeeLayout(ws, extra, english, main.BILL_TO_FOB);
        ApplyPrintLayout(ws);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void ApplyInvoiceTitle(IXLWorksheet ws, bool english, string? orderType)
    {
        if (!CotractPiTypes.IsCustomerCi(orderType))
            return;
        Set(ws, english ? 3 : 4, 0, "COMMERCIAL INVOICE");
    }

    public static string DownloadName(ZyqdCotractPiModel main)
    {
        var prefix = CotractPiTypes.IsCustomerCi(main.var6) ? "CI" : "PI";
        var no = string.IsNullOrWhiteSpace(main.INVOICE_NO) ? prefix : main.INVOICE_NO.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            no = no.Replace(c, '_');
        return $"{prefix}  {no}.xlsx";
    }

    public static string DownloadPdfName(ZyqdCotractPiModel main, string? tag = null)
    {
        var name = DownloadName(main);
        var stem = name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? name[..^5]
            : name;
        var suffix = string.IsNullOrWhiteSpace(tag) ? "" : "-" + tag.Trim();
        return stem + suffix + ".pdf";
    }

    private string ResolveTemplatePath(string type)
    {
        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var dir = Path.Combine(webRoot, "Templates");
        var name = CotractPiTypes.TemplateFileName(type);
        var path = Path.Combine(dir, name);
        if (File.Exists(path))
            return path;
        throw new FileNotFoundException("未找到 PI 模板，请确认文件在 wwwroot/Templates/" + name);
    }

    private static void FillEnglish(IXLWorksheet ws, ZyqdCotractPiModel main, List<ZyqdCotractPiModel> rows, string curr)
    {
        Set(ws, 4, 1, main.BILL_TO_info1); Set(ws, 4, 10, main.INVOICE_NO);
        Set(ws, 5, 1, main.BILL_TO_info3); SetDate(ws, 5, 10, main.BILL_TO_DATE);
        Set(ws, 6, 1, main.BILL_TO_info4);
        Set(ws, 7, 1, main.SHIP_TO_info1); Set(ws, 6, 10, main.BILL_TO_PO_REF);
        Set(ws, 8, 1, main.SHIP_TO_info3);
        Set(ws, 9, 1, main.SHIP_TO_info4); Set(ws, 9, 9, main.BILL_TO_FOB);
        Set(ws, 17, 0, main.var4);
        if (!string.IsNullOrWhiteSpace(main.var2))
            Set(ws, 17, 5, "Estimated Time of Completion: " + CotractPiTypes.FormatDate(main.var2));
        var fees = CotractPiTypes.ResolveFeeVisibility(main.BILL_TO_FOB);
        if (fees.Ship)
        {
            Set(ws, 17, 10, main.Shipping_Fee);
            Money(ws, 17, 12, ParseNum(main.var10), curr);
        }
        if (fees.Insure)
            Money(ws, 18, 12, main.Insurance_premium, curr);
        Money(ws, 19, 12, main.TOTAL, curr);
        Set(ws, 20, 11, main.Payment_ratio.ToString("0.##") + "%");
        Money(ws, 20, 12, main.Payment_ratio_Ext, curr);
        Money(ws, 21, 12, main.balance_before_delivery, curr);
        Set(ws, 21, 5, main.var3);
        Set(ws, 25, 0, "NOTE: UPON PAYMENT, PLEASE EMAIL A COPY OF THE BANK SLIP or SCREENSHOT TO " + main.email);
        SetDate(ws, 28, 11, main.var5);

        EnsureBlocks(ws, 11, 16, 6, rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var item = rows[i];
            var r = 11 + 6 * i;
            Set(ws, r, 2, item.materiaCust);
            Set(ws, r, 8, FmtQty(item.qty));
            Set(ws, r, 9, item.var7);
            Set(ws, r, 11, item.var8);
            Money(ws, r, 10, item.price, curr);
            Money(ws, r, 12, item.ext, curr);
            MergeSeq(ws, r, r + 5, 0, (i + 1).ToString());
            Set(ws, r + 1, 2, item.materialModel);
            var (first, last) = SplitSpecs(item.materiaSpecs);
            Set(ws, r + 2, 2, first);
            Set(ws, r + 3, 2, last);
            Set(ws, r + 4, 2, item.materiaPowered);
            Set(ws, r + 5, 2, item.Notes);
        }
    }

    private static void FillChinese(IXLWorksheet ws, ZyqdCotractPiModel main, List<ZyqdCotractPiModel> rows, string curr)
    {
        Set(ws, 5, 1, main.BILL_TO_info1); Set(ws, 5, 10, main.INVOICE_NO);
        Set(ws, 6, 1, main.BILL_TO_info3); SetDate(ws, 6, 10, main.BILL_TO_DATE);
        Set(ws, 7, 1, main.BILL_TO_info4);
        Set(ws, 8, 1, main.SHIP_TO_info1); Set(ws, 7, 10, main.BILL_TO_PO_REF);
        Set(ws, 9, 1, main.SHIP_TO_info3);
        Set(ws, 10, 1, main.SHIP_TO_info4); Set(ws, 10, 9, main.BILL_TO_FOB);
        Set(ws, 18, 0, main.var4);
        if (!string.IsNullOrWhiteSpace(main.var2))
            Set(ws, 18, 5, "Estimated Time of Completion: " + CotractPiTypes.FormatDate(main.var2));
        var fees = CotractPiTypes.ResolveFeeVisibility(main.BILL_TO_FOB);
        if (fees.Ship)
        {
            Set(ws, 18, 10, main.Shipping_Fee);
            Money(ws, 18, 12, ParseNum(main.var10), curr);
        }
        if (fees.Insure)
            Money(ws, 19, 12, main.Insurance_premium, curr);
        Money(ws, 20, 12, main.TOTAL, curr);
        Money(ws, 21, 12, main.Payment_ratio_Ext, curr);
        Money(ws, 22, 12, main.balance_before_delivery, curr);
        Set(ws, 21, 11, main.Payment_ratio.ToString("0.##") + "%");
        Set(ws, 22, 5, main.var3);
        Set(ws, 30, 0, "NOTE: UPON PAYMENT, PLEASE EMAIL A COPY OF THE BANK SLIP OR SCREENSHOT TO " + main.email);
        SetDate(ws, 31, 11, main.var5);

        EnsureBlocks(ws, 12, 17, 6, rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var item = rows[i];
            var r = 12 + 6 * i;
            var percent = item.percent == 0 ? 1 : item.percent;
            Set(ws, r, 2, item.materiaCust);
            Set(ws, r, 8, FmtQty(item.qty));
            Set(ws, r, 9, item.var7);
            Set(ws, r, 11, item.var8);
            Money(ws, r, 10, item.price * percent, curr);
            Money(ws, r, 12, item.ext, curr);
            MergeSeq(ws, r, r + 5, 0, (i + 1).ToString());
            Set(ws, r + 1, 2, item.materialModel);
            var (first, last) = SplitSpecs(item.materiaSpecs);
            Set(ws, r + 2, 2, first);
            Set(ws, r + 3, 2, last);
            Set(ws, r + 4, 2, item.materiaPowered);
            Set(ws, r + 5, 2, item.Notes);
        }
    }

    private static void ApplyPrintLayout(IXLWorksheet ws)
    {
        var page = ws.PageSetup;
        page.PageOrientation = XLPageOrientation.Portrait;
        page.PaperSize = XLPaperSize.A4Paper;
        page.AdjustTo(85);
        page.PagesWide = 1;
        page.PagesTall = 0;
        page.Margins.Left = 0.1;
        page.Margins.Right = 0.1;
        page.Margins.Top = 0.5;
        page.Margins.Bottom = 0.5;
        page.CenterHorizontally = true;
    }

    private static void ApplyTradeFeeLayout(IXLWorksheet ws, int extra, bool english, string? tradeTerm)
    {
        var fees = CotractPiTypes.ResolveFeeVisibility(tradeTerm);
        var shipExcel = (english ? 18 : 19) + extra;
        var insExcel = (english ? 19 : 20) + extra;
        if (!fees.Ship)
            ClearFeeCells(ws, shipExcel);
        if (!fees.Insure)
        {
            ClearFeeCells(ws, insExcel);
            ws.Row(insExcel).Hide();
        }
    }

    private static void ClearFeeCells(IXLWorksheet ws, int excelRow)
    {
        for (var c = 10; c <= 13; c++)
            ws.Cell(excelRow, c).Clear(XLClearOptions.Contents);
    }

    private static void EnsureBlocks(IXLWorksheet ws, int srcStartNpoi, int srcEndNpoi, int rowsPer, int count)
    {
        if (count <= 1) return;
        var first = srcStartNpoi + 1;
        var last = srcEndNpoi + 1;
        var extra = count - 1;
        ws.Row(last).InsertRowsBelow(extra * rowsPer);
        var src = ws.Range(first, 1, last, 13);
        for (var i = 0; i < extra; i++)
            src.CopyTo(ws.Cell(last + 1 + i * rowsPer, 1));
    }

    private static void MergeSeq(IXLWorksheet ws, int npoiStart, int npoiEnd, int npoiCol, string text)
    {
        var c = npoiCol + 1;
        var r1 = npoiStart + 1;
        var r2 = npoiEnd + 1;
        try
        {
            ws.Range(r1, c, r2, c).Merge();
        }
        catch
        {
            // 模板里可能已经合并
        }
        var cell = ws.Cell(r1, c);
        cell.Value = text;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void SetDate(IXLWorksheet ws, int npoiRow, int npoiCol, string? value)
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        var text = CotractPiTypes.FormatDate(value);
        if (text.Length == 0)
        {
            cell.Clear(XLClearOptions.Contents);
            return;
        }
        cell.Style.NumberFormat.Format = "@";
        cell.SetValue(text);
    }

    private static void Set(IXLWorksheet ws, int npoiRow, int npoiCol, string? value)
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        var text = value ?? "";
        if (double.TryParse(text, out var n) && !text.StartsWith('0') && text.Length < 16)
            cell.Value = n;
        else
            cell.Value = text;
    }

    private static void Money(IXLWorksheet ws, int npoiRow, int npoiCol, double value, string curr)
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        cell.Value = value;
        cell.Style.NumberFormat.Format = curr switch
        {
            "$" => "\"$\"#,##0.00",
            "€" => "\"€\"#,##0.00",
            "C$" => "\"C$\"#,##0.00",
            _ => "\"¥\"#,##0.00"
        };
    }

    private static double ParseNum(string? s)
        => double.TryParse(s, out var n) ? n : 0;

    private static string FmtQty(double qty)
        => qty.ToString("0.##");

    private static (string First, string Last) SplitSpecs(string? specs)
    {
        var content = Regex.Replace(specs ?? "", @"[\x00-\x1F\x7F]", "");
        var first = new StringBuilder();
        var last = new StringBuilder();
        var width = 0;
        const int max = 170;
        foreach (var c in content)
        {
            var w = c > 127 ? 2 : 1;
            if (width + w > max)
                last.Append(c);
            else
            {
                first.Append(c);
                width += w;
            }
        }
        return (first.ToString(), last.ToString());
    }
}
