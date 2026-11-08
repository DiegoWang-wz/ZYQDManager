using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ClosedXML.Excel;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;

namespace ZYQDManager.Services;

public class CotractPiExcelService
{
    private readonly IWebHostEnvironment _env;
    private readonly List<(string CellRef, double Value)> _formulaCache = new();

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

        _formulaCache.Clear();
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
        WriteCompletion(ws, rows);
        ApplyPrintLayout(ws);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return EmbedFormulaCachedValues(ms.ToArray(), _formulaCache);
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

    private void FillEnglish(IXLWorksheet ws, ZyqdCotractPiModel main, List<ZyqdCotractPiModel> rows, string curr)
    {
        Set(ws, 4, 1, main.BILL_TO_info1); Set(ws, 4, 10, main.INVOICE_NO);
        Set(ws, 5, 1, main.BILL_TO_info3); SetDate(ws, 5, 10, main.BILL_TO_DATE);
        Set(ws, 6, 1, main.BILL_TO_info4);
        Set(ws, 7, 1, main.SHIP_TO_info1); Set(ws, 6, 10, main.BILL_TO_PO_REF);
        Set(ws, 8, 1, main.SHIP_TO_info3);
        Set(ws, 9, 1, main.SHIP_TO_info4); Set(ws, 9, 9, main.BILL_TO_FOB);

        var extra = Math.Max(0, rows.Count - 1) * 6;
        EnsureBlocks(ws, 11, 16, 6, rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var item = rows[i];
            var r = 11 + 6 * i;
            Set(ws, r, 2, item.materiaCust);
            Set(ws, r, 8, FmtQty(item.qty));
            Set(ws, r, 9, item.var7);
            Set(ws, r, 11, item.var8);
            WriteLineMoney(ws, r, item, curr);
            MergeSeq(ws, r, r + 5, 0, (i + 1).ToString());
            Set(ws, r + 1, 2, item.materialModel);
            var (first, last) = SplitSpecs(item.materiaSpecs);
            Set(ws, r + 2, 2, first);
            Set(ws, r + 3, 2, last);
            Set(ws, r + 4, 2, item.materiaPowered);
            Set(ws, r + 5, 2, item.Notes);
        }

        Set(ws, 17 + extra, 0, main.var4);
        var fees = CotractPiTypes.ResolveFeeVisibility(main.BILL_TO_FOB);
        if (fees.Ship)
        {
            Set(ws, 17 + extra, 10, main.Shipping_Fee);
            Money(ws, 17 + extra, 12, ParseNum(main.var10), curr);
        }
        if (fees.Insure)
            Money(ws, 18 + extra, 12, main.Insurance_premium, curr);
        Money(ws, 19 + extra, 12, main.TOTAL, curr);
        WritePaymentBlockEnglish(ws, 20 + extra, 21 + extra, main, curr);
        Set(ws, 25 + extra, 0, "NOTE: UPON PAYMENT, PLEASE EMAIL A COPY OF THE BANK SLIP or SCREENSHOT TO " + main.email);
        SetDate(ws, 28 + extra, 11, main.var5);
    }

    private void FillChinese(IXLWorksheet ws, ZyqdCotractPiModel main, List<ZyqdCotractPiModel> rows, string curr)
    {
        Set(ws, 5, 1, main.BILL_TO_info1); Set(ws, 5, 10, main.INVOICE_NO);
        Set(ws, 6, 1, main.BILL_TO_info3); SetDate(ws, 6, 10, main.BILL_TO_DATE);
        Set(ws, 7, 1, main.BILL_TO_info4);
        Set(ws, 8, 1, main.SHIP_TO_info1); Set(ws, 7, 10, main.BILL_TO_PO_REF);
        Set(ws, 9, 1, main.SHIP_TO_info3);
        Set(ws, 10, 1, main.SHIP_TO_info4); Set(ws, 10, 9, main.BILL_TO_FOB);

        var extra = Math.Max(0, rows.Count - 1) * 6;
        EnsureBlocks(ws, 12, 17, 6, rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var item = rows[i];
            var r = 12 + 6 * i;
            Set(ws, r, 2, item.materiaCust);
            Set(ws, r, 8, FmtQty(item.qty));
            Set(ws, r, 9, item.var7);
            Set(ws, r, 11, item.var8);
            WriteLineMoney(ws, r, item, curr);
            MergeSeq(ws, r, r + 5, 0, (i + 1).ToString());
            Set(ws, r + 1, 2, item.materialModel);
            var (first, last) = SplitSpecs(item.materiaSpecs);
            Set(ws, r + 2, 2, first);
            Set(ws, r + 3, 2, last);
            Set(ws, r + 4, 2, item.materiaPowered);
            Set(ws, r + 5, 2, item.Notes);
        }

        Set(ws, 18 + extra, 0, main.var4);
        var fees = CotractPiTypes.ResolveFeeVisibility(main.BILL_TO_FOB);
        if (fees.Ship)
        {
            Set(ws, 18 + extra, 10, main.Shipping_Fee);
            Money(ws, 18 + extra, 12, ParseNum(main.var10), curr);
        }
        if (fees.Insure)
            Money(ws, 19 + extra, 12, main.Insurance_premium, curr);
        Money(ws, 20 + extra, 12, main.TOTAL, curr);
        WritePaymentBlockChinese(ws, 21 + extra, 22 + extra, main, curr);
        Set(ws, 30 + extra, 0, "NOTE: UPON PAYMENT, PLEASE EMAIL A COPY OF THE BANK SLIP OR SCREENSHOT TO " + main.email);
        SetDate(ws, 31 + extra, 11, main.var5);
    }

    /// <summary>
    /// 英文模板：定金行 + 付款行。第 15 条改为 UPON DESPATCH / WITHIN 10 DAYS 各 50%（按总金额）。
    /// </summary>
    private static void WritePaymentBlockEnglish(
        IXLWorksheet ws, int depositRow, int paymentRow, ZyqdCotractPiModel main, string curr)
    {
        if (CotractPiTypes.ShouldSplitBalancePayment(main.var3))
        {
            WriteSplitPaymentRows(ws, depositRow, paymentRow, main.TOTAL, curr, percentCol: 11);
            return;
        }

        Set(ws, depositRow, 11, main.Payment_ratio.ToString("0.##") + "%");
        Money(ws, depositRow, 12, main.Payment_ratio_Ext, curr);
        Set(ws, paymentRow, 5, main.var3);
        Money(ws, paymentRow, 12, main.balance_before_delivery, curr);
    }

    /// <summary>中文模板：同上，列位与英文略有差异。</summary>
    private static void WritePaymentBlockChinese(
        IXLWorksheet ws, int depositRow, int paymentRow, ZyqdCotractPiModel main, string curr)
    {
        if (CotractPiTypes.ShouldSplitBalancePayment(main.var3))
        {
            WriteSplitPaymentRows(ws, depositRow, paymentRow, main.TOTAL, curr, percentCol: 11);
            return;
        }

        Money(ws, depositRow, 12, main.Payment_ratio_Ext, curr);
        Set(ws, depositRow, 11, main.Payment_ratio.ToString("0.##") + "%");
        Set(ws, paymentRow, 5, main.var3);
        Money(ws, paymentRow, 12, main.balance_before_delivery, curr);
    }

    /// <summary>
    /// 第 15 条：定金行改成 UPON DESPATCH + 50%，付款行 WITHIN 10 DAYS AFTER ARRIVAL + 50%。
    /// 保留模板合并格与边框，标题右对齐贴着百分比列。
    /// </summary>
    private static void WriteSplitPaymentRows(
        IXLWorksheet ws, int depositNpoi, int paymentNpoi, double total, string curr, int percentCol)
    {
        var (first, second) = CotractPiTypes.SplitBalanceAmounts(total);
        var depositExcel = depositNpoi + 1;
        var paymentExcel = paymentNpoi + 1;
        var percentExcel = percentCol + 1; // ClosedXML：npoi 11 → 12
        var paymentLabelExcel = 6;         // 与 var3 同列（npoi 5）

        // 1) 定金行：原地替换 DEPOSIT，保留合并/边框，标题靠右
        var upon = ReplaceDepositLabelInPlace(ws, depositExcel, "UPON DESPATCH");
        Set(ws, depositNpoi, percentCol, "50%");
        Money(ws, depositNpoi, 12, first, curr);

        // 2) 付款行：若标题合并格盖住 % 列，收窄到 % 左侧，再写标题与 50%
        //    标题列与定金行对齐，右对齐贴着 50%
        var withinCol = upon?.Address.ColumnNumber ?? paymentLabelExcel;
        ShrinkPaymentLabelMerge(ws, paymentExcel, withinCol, percentExcel);
        ShrinkPaymentLabelMerge(ws, paymentExcel, paymentLabelExcel, percentExcel);
        var within = WriteRightLabel(ws, paymentExcel, withinCol, "WITHIN 10 DAYS AFTER ARRIVAL");
        if (upon is not null)
        {
            CopyFontStyle(within, upon);
            CopyBorderStyle(upon, within);
        }

        var depositPct = ResolveCell(ws, depositExcel, percentExcel);
        var paymentPct = ResolveCell(ws, paymentExcel, percentExcel);
        paymentPct.Style.NumberFormat.Format = "@";
        paymentPct.SetValue("50%");
        paymentPct.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        paymentPct.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        CopyFontStyle(depositPct, paymentPct);
        CopyBorderStyle(depositPct, paymentPct);

        Money(ws, paymentNpoi, 12, second, curr);
    }

    private static IXLCell ResolveCell(IXLWorksheet ws, int excelRow, int excelCol)
    {
        var cell = ws.Cell(excelRow, excelCol);
        return cell.IsMerged() ? cell.MergedRange().FirstCell() : cell;
    }

    private static IXLCell? ReplaceDepositLabelInPlace(IXLWorksheet ws, int excelRow, string newText)
    {
        IXLCell? hit = null;
        for (var c = 1; c <= 14; c++)
        {
            var probe = ResolveCell(ws, excelRow, c);
            var text = probe.GetString();
            if (text.Contains("DEPOSIT", StringComparison.OrdinalIgnoreCase)
                || text.Contains("DUE AT TIME OF ORDER", StringComparison.OrdinalIgnoreCase)
                || text.Contains("定金", StringComparison.Ordinal))
            {
                hit = probe;
                break;
            }
        }

        // 找不到原文时，写到 % 列左侧单元格，保证靠右贴着 50%
        hit ??= ResolveCell(ws, excelRow, 11);
        hit.Style.NumberFormat.Format = "@";
        hit.SetValue(newText);
        hit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        hit.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        return hit;
    }

    private static IXLCell WriteRightLabel(IXLWorksheet ws, int excelRow, int excelCol, string text)
    {
        var cell = ResolveCell(ws, excelRow, excelCol);
        cell.Style.NumberFormat.Format = "@";
        cell.SetValue(text);
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        return cell;
    }

    /// <summary>
    /// 付款条件合并格若延伸到 % 列，拆开后只合并到 % 列左侧，给 50% 留独立格。
    /// </summary>
    private static void ShrinkPaymentLabelMerge(
        IXLWorksheet ws, int excelRow, int labelExcelCol, int percentExcelCol)
    {
        var cell = ws.Cell(excelRow, labelExcelCol);
        if (!cell.IsMerged())
            return;

        var range = cell.MergedRange();
        if (range.FirstRow().RowNumber() != excelRow || range.LastRow().RowNumber() != excelRow)
            return;

        var firstCol = range.FirstColumn().ColumnNumber();
        var lastCol = range.LastColumn().ColumnNumber();
        if (lastCol < percentExcelCol)
            return;

        range.Unmerge();
        var newLast = percentExcelCol - 1;
        if (newLast >= firstCol)
        {
            try
            {
                ws.Range(excelRow, firstCol, excelRow, newLast).Merge();
            }
            catch
            {
                // 模板边框异常时忽略，至少保证 % 列可写
            }
        }
    }

    private static void CopyFontStyle(IXLCell from, IXLCell to)
    {
        to.Style.Font.FontName = from.Style.Font.FontName;
        to.Style.Font.FontSize = from.Style.Font.FontSize;
        to.Style.Font.Bold = from.Style.Font.Bold;
        to.Style.Font.Italic = from.Style.Font.Italic;
        to.Style.Font.FontColor = from.Style.Font.FontColor;
    }

    private static void CopyBorderStyle(IXLCell from, IXLCell to)
    {
        to.Style.Border.LeftBorder = from.Style.Border.LeftBorder;
        to.Style.Border.RightBorder = from.Style.Border.RightBorder;
        to.Style.Border.TopBorder = from.Style.Border.TopBorder;
        to.Style.Border.BottomBorder = from.Style.Border.BottomBorder;
        to.Style.Border.LeftBorderColor = from.Style.Border.LeftBorderColor;
        to.Style.Border.RightBorderColor = from.Style.Border.RightBorderColor;
        to.Style.Border.TopBorderColor = from.Style.Border.TopBorderColor;
        to.Style.Border.BottomBorderColor = from.Style.Border.BottomBorderColor;
    }

    /// <summary>
    /// 模板 F19:J19 为合并单元格，和 Shipping_Fee 同行；明细插入后整行下移。
    /// 必须在清理运费之后写，否则清 J 列会把合并格内容一起清掉。
    /// </summary>
    private static void WriteCompletion(IXLWorksheet ws, IReadOnlyList<ZyqdCotractPiModel> rows)
    {
        var text = CompletionText(rows);
        if (text.Length == 0) return;

        var row = FindCompletionExcelRow(ws);
        if (row <= 0) return;

        var cell = ws.Cell(row, 6);
        if (cell.IsMerged())
            cell = cell.MergedRange().FirstCell();
        cell.Style.NumberFormat.Format = "@";
        cell.SetValue("Estimated Time of Completion: " + text);
    }

    private static int FindCompletionExcelRow(IXLWorksheet ws)
    {
        var last = ws.LastRowUsed()?.RowNumber() ?? 40;
        if (last > 400) last = 400;
        for (var r = 1; r <= last; r++)
        {
            for (var c = 1; c <= 14; c++)
            {
                var label = ws.Cell(r, c).GetString();
                if (label.Contains("SUB-TOTAL", StringComparison.OrdinalIgnoreCase)
                    || label.Contains("SUB TOTAL", StringComparison.OrdinalIgnoreCase))
                    return r - 2;
            }
        }
        return 19;
    }

    /// <summary>预计完成时间：优先 var2，旧数据回退 Estimated_TimeofCompletion。</summary>
    private static string CompletionText(IEnumerable<ZyqdCotractPiModel> rows)
    {
        foreach (var row in rows)
        {
            var raw = FirstText(row.var2, row.Estimated_TimeofCompletion);
            if (raw.Length > 0)
                return CotractPiTypes.FormatDate(raw);
        }
        return "";
    }

    private static string FirstText(params string?[] values)
    {
        foreach (var value in values)
        {
            var text = (value ?? "").Trim();
            if (text.Length > 0)
                return text;
        }
        return "";
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
        // J 列（10）属于 F:J 预计完成时间合并格，不能清
        for (var c = 11; c <= 13; c++)
        {
            var cell = ws.Cell(excelRow, c);
            if (cell.IsMerged() && cell.MergedRange().FirstCell().Address.ColumnNumber <= 6)
                continue;
            cell.Clear(XLClearOptions.Contents);
        }
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

    /// <summary>
    /// 单价写入原价*折扣公式，金额引用单价格。单元格格式显示两位，点进去仍是原值和公式。
    /// </summary>
    private void WriteLineMoney(IXLWorksheet ws, int npoiRow, ZyqdCotractPiModel item, string curr)
    {
        var percent = item.percent == 0 ? 1d : item.percent;
        var origin = ParseNum(item.var9);
        if (origin == 0) origin = item.price;

        var excelRow = npoiRow + 1;
        var qtyCell = ws.Cell(excelRow, 9);
        var unitCell = ws.Cell(excelRow, 11);
        var amountCell = ws.Cell(excelRow, 13);
        var fmt = MoneyFormat(curr);
        var unit = origin * percent;
        var amount = origin * item.qty * percent;

        if (Math.Abs(percent - 1d) > 1e-9)
        {
            unitCell.FormulaA1 = "=" + ExcelLiteral(origin) + "*" + ExcelLiteral(percent);
            Remember(unitCell, unit);
        }
        else
        {
            unitCell.Value = origin;
        }

        unitCell.Style.NumberFormat.Format = fmt;

        var unitRef = unitCell.Address.ColumnLetter + unitCell.Address.RowNumber;
        var qtyRef = qtyCell.Address.ColumnLetter + qtyCell.Address.RowNumber;
        amountCell.FormulaA1 = "=" + unitRef + "*" + qtyRef;
        amountCell.Style.NumberFormat.Format = fmt;
        Remember(amountCell, amount);
    }

    private void Remember(IXLCell cell, double value)
    {
        if (!double.IsFinite(value)) return;
        var addr = cell.Address.ToString() ?? "";
        var bang = addr.LastIndexOf('!');
        if (bang >= 0)
            addr = addr[(bang + 1)..];
        if (addr.Length == 0) return;
        _formulaCache.Add((addr, value));
    }

    private static string ExcelLiteral(double n)
        => n.ToString("0.########", CultureInfo.InvariantCulture);

    private static string MoneyFormat(string curr)
        => curr switch
        {
            "$" => "\"$\"#,##0.00",
            "€" => "\"€\"#,##0.00",
            "C$" => "\"C$\"#,##0.00",
            _ => "\"¥\"#,##0.00"
        };

    private static void Money(IXLWorksheet ws, int npoiRow, int npoiCol, double value, string curr)
    {
        var cell = ws.Cell(npoiRow + 1, npoiCol + 1);
        cell.Value = value;
        cell.Style.NumberFormat.Format = MoneyFormat(curr);
    }

    /// <summary>
    /// ClosedXML 写公式时通常不带缓存值。把已算好的原值写回 &lt;v&gt;，公式保留，WPS 打开也能看到两位结果。
    /// </summary>
    private static byte[] EmbedFormulaCachedValues(
        byte[] xlsx, IReadOnlyList<(string CellRef, double Value)> cache)
    {
        if (xlsx.Length == 0 || cache.Count == 0)
            return xlsx;

        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var (cellRef, value) in cache)
        {
            var key = (cellRef ?? "").Trim();
            if (key.Length == 0 || !double.IsFinite(value))
                continue;
            map[key] = value;
        }

        if (map.Count == 0)
            return xlsx;

        try
        {
            using var output = new MemoryStream();
            output.Write(xlsx);
            output.Position = 0;
            using (var zip = new ZipArchive(output, ZipArchiveMode.Update, leaveOpen: true))
            {
                PatchWorkbookCalcOnLoad(zip);

                var sheetNames = zip.Entries
                    .Where(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
                                && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.FullName)
                    .ToList();

                foreach (var name in sheetNames)
                {
                    var entry = zip.GetEntry(name);
                    if (entry is null) continue;
                    XDocument doc;
                    using (var s = entry.Open())
                        doc = XDocument.Load(s);

                    XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                    var changed = false;
                    foreach (var c in doc.Descendants(ns + "c"))
                    {
                        var r = (string?)c.Attribute("r");
                        if (string.IsNullOrWhiteSpace(r) || !map.TryGetValue(r, out var val))
                            continue;
                        var f = c.Element(ns + "f");
                        if (f is null) continue;

                        c.Attribute("t")?.Remove();
                        f.SetAttributeValue("ca", "1");
                        var text = val.ToString("G15", CultureInfo.InvariantCulture);
                        var v = c.Element(ns + "v");
                        if (v is null)
                            f.AddAfterSelf(new XElement(ns + "v", text));
                        else
                            v.Value = text;
                        changed = true;
                    }

                    if (!changed) continue;
                    entry.Delete();
                    var neu = zip.CreateEntry(name);
                    using var w = neu.Open();
                    doc.Save(w);
                }
            }

            return output.ToArray();
        }
        catch
        {
            return xlsx;
        }
    }

    private static void PatchWorkbookCalcOnLoad(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/workbook.xml");
        if (entry is null) return;
        XDocument doc;
        using (var s = entry.Open())
            doc = XDocument.Load(s);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var root = doc.Root;
        if (root is null) return;
        var calcPr = root.Element(ns + "calcPr");
        if (calcPr is null)
        {
            calcPr = new XElement(ns + "calcPr");
            root.Add(calcPr);
        }

        calcPr.SetAttributeValue("fullCalcOnLoad", "1");
        calcPr.SetAttributeValue("calcMode", "auto");
        entry.Delete();
        var neu = zip.CreateEntry("xl/workbook.xml");
        using var w = neu.Open();
        doc.Save(w);
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
