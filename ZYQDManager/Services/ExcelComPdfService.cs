using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

namespace ZYQDManager.Services;

public class ExcelComPdfService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly FileOperationService _fileOp;
    private readonly ILogger<ExcelComPdfService> _log;

    public ExcelComPdfService(FileOperationService fileOp, ILogger<ExcelComPdfService> log)
    {
        _fileOp = fileOp;
        _log = log;
    }

    public async Task<byte[]> ConvertXlsxToPdfAsync(
        byte[] xlsx,
        int lineCount,
        bool english,
        CancellationToken ct = default)
    {
        if (xlsx is null || xlsx.Length == 0)
            throw new InvalidOperationException("没有可转换的 Excel 内容");

        var work = Path.Combine(Path.GetTempPath(), "zyqd-xl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        var xlsxPath = Path.Combine(work, "pi.xlsx");
        var pdfPath = Path.Combine(work, "pi.pdf");
        await File.WriteAllBytesAsync(xlsxPath, xlsx, ct);

        await Gate.WaitAsync(ct);
        try
        {
            await RunStaAsync(() =>
            {
                ExportWithExcel(xlsxPath, pdfPath, lineCount, english);
                return 0;
            });
        }
        finally
        {
            Gate.Release();
        }

        if (!File.Exists(pdfPath) || new FileInfo(pdfPath).Length == 0)
            throw new InvalidOperationException("Excel 未生成 PDF，请确认本机已安装 Microsoft Excel");

        try
        {
            return await _fileOp.ReadDecryptedPdfAsync(pdfPath, _log, ct);
        }
        finally
        {
            TryDeleteDir(work);
        }
    }

    private void ExportWithExcel(string xlsxPath, string pdfPath, int lineCount, bool english)
    {
        Excel.Application? app = null;
        Excel.Workbooks? books = null;
        Excel.Workbook? book = null;
        Excel.Sheets? sheets = null;
        Excel.Worksheet? sheet = null;
        Excel.PageSetup? setup = null;
        Excel.Range? breakRow = null;
        try
        {
            app = new Excel.Application
            {
                Visible = false,
                DisplayAlerts = false,
                ScreenUpdating = false
            };
            books = app.Workbooks;
            book = books.Open(xlsxPath, Type.Missing, true);
            sheets = book.Sheets;
            sheet = (Excel.Worksheet)sheets[1];
            setup = sheet.PageSetup;
            setup.Orientation = Excel.XlPageOrientation.xlPortrait;
            setup.PaperSize = Excel.XlPaperSize.xlPaperA4;
            setup.FitToPagesWide = false;
            setup.FitToPagesTall = false;
            setup.Zoom = 85;
            setup.LeftMargin = app.InchesToPoints(0.1);
            setup.RightMargin = app.InchesToPoints(0.1);
            setup.TopMargin = app.InchesToPoints(0.5);
            setup.BottomMargin = app.InchesToPoints(0.5);

            var everyPage = 59;
            var topQty = (english ? 12 : 13) + Math.Max(lineCount, 1) * 6;
            var leftQty = english ? 11 : 13;
            var remainder = topQty % everyPage;
            if (remainder + leftQty > everyPage)
            {
                breakRow = (Excel.Range)sheet.Rows[topQty + 1];
                breakRow.PageBreak = (int)Excel.XlPageBreak.xlPageBreakManual;
            }

            book.ExportAsFixedFormat(Excel.XlFixedFormatType.xlTypePDF, pdfPath);
            _log.LogInformation("Excel COM 已导出 PDF {Path}", pdfPath);
        }
        finally
        {
            Release(breakRow);
            setup = null;
            Release(sheet);
            Release(sheets);
            if (book is not null)
            {
                try { book.Close(false); } catch { /* ignore */ }
                Release(book);
            }
            Release(books);
            if (app is not null)
            {
                try { app.Quit(); } catch { /* ignore */ }
                Release(app);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private static Task<T> RunStaAsync<T>(Func<T> work)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try { tcs.SetResult(work()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Name = "ExcelComPdf";
        thread.Start();
        return tcs.Task;
    }

    private static void Release(object? com)
    {
        if (com is null) return;
        try { Marshal.ReleaseComObject(com); } catch { /* ignore */ }
    }

    private static void TryDeleteDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        catch
        {
            // Excel 可能短暂占用
        }
    }
}
