using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using ClosedXML.Excel;

namespace XlProbe;

internal static class Program
{
    private const string LogPath = @"d:\JC_Project\ZYQDManager\_xlprobe\probe.log";
    private const string OutDir = @"d:\JC_Project\ZYQDManager\_xlprobe";
    private const string TplDir = @"d:\JC_Project\ZYQDManager\ZYQDManager\wwwroot\Templates";
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    [STAThread]
    private static void Main()
    {
        File.WriteAllText(LogPath, "", Encoding.UTF8);
        Log("start ClosedXML=" + typeof(XLWorkbook).Assembly.GetName().Version);
        try
        {
            var srcPath = Path.Combine(TplDir, "装箱单模板-报关.xlsx");
            var rt = ClosedXmlRoundtrip(File.ReadAllBytes(srcPath));
            var rtPath = Path.Combine(OutDir, "rt.xlsx");
            File.WriteAllBytes(rtPath, rt);
            Log("RT=" + ExcelOpenStatus(rtPath));

            var fixed1 = Path.Combine(OutDir, "fix_inject_dxfs.xlsx");
            File.Copy(rtPath, fixed1, true);
            try
            {
                InjectDxfsViaLinq(srcPath, fixed1);
                Log("inject dxfs=" + ExcelOpenStatus(fixed1));
            }
            catch (Exception ex)
            {
                Log("inject fail " + ex.Message);
            }

            var fixed2 = Path.Combine(OutDir, "fix_drop_tablestyles.xlsx");
            File.Copy(rtPath, fixed2, true);
            DropTableStyles(fixed2);
            Log("drop tableStyles=" + ExcelOpenStatus(fixed2));

            var fixed3 = Path.Combine(OutDir, "fix_drop_ts_from_src.xlsx");
            File.Copy(srcPath, fixed3, true);
            DropTableStyles(fixed3);
            Log("src-no-ts excel=" + ExcelOpenStatus(fixed3));
            var rt3 = ClosedXmlRoundtrip(File.ReadAllBytes(fixed3));
            var rt3Path = Path.Combine(OutDir, "rt_no_ts.xlsx");
            File.WriteAllBytes(rt3Path, rt3);
            Log("rt-no-ts excel=" + ExcelOpenStatus(rt3Path));

            if (ExcelOpenStatus(rt3Path) == "OK")
            {
                var dest = Path.Combine(TplDir, "装箱单模板-报关.xlsx");
                var bak = Path.Combine(TplDir, "装箱单模板-报关.bak.xlsx");
                if (!File.Exists(bak)) File.Copy(dest, bak);
                File.Copy(fixed3, dest, true);
                Log("REPLACED template (removed tableStyles)");
                var v = ClosedXmlRoundtrip(File.ReadAllBytes(dest));
                var vp = Path.Combine(OutDir, "verify.xlsx");
                File.WriteAllBytes(vp, v);
                Log("VERIFY=" + ExcelOpenStatus(vp));
            }

            if (ExcelOpenStatus(fixed1) == "OK")
                Log("inject approach works - use runtime postprocess");

            Log("DONE");
        }
        catch (Exception ex)
        {
            Log("FATAL " + ex);
        }
    }

    private static void InjectDxfsViaLinq(string src, string dest)
    {
        XElement srcDxfs;
        using (var z = ZipFile.OpenRead(src))
        using (var s = z.GetEntry("xl/styles.xml")!.Open())
        {
            var doc = XDocument.Load(s);
            srcDxfs = doc.Root!.Element(Ns + "dxfs")
                ?? throw new InvalidOperationException("src has no dxfs");
        }

        using var z2 = ZipFile.Open(dest, ZipArchiveMode.Update);
        var entry = z2.GetEntry("xl/styles.xml")!;
        XDocument destDoc;
        using (var s = entry.Open())
            destDoc = XDocument.Load(s);

        destDoc.Root!.Element(Ns + "dxfs")?.Remove();
        var clone = XElement.Parse(srcDxfs.ToString());
        var tableStyles = destDoc.Root.Element(Ns + "tableStyles");
        var extLst = destDoc.Root.Element(Ns + "extLst");
        if (tableStyles is not null) tableStyles.AddBeforeSelf(clone);
        else if (extLst is not null) extLst.AddBeforeSelf(clone);
        else destDoc.Root.Add(clone);

        entry.Delete();
        var neu = z2.CreateEntry("xl/styles.xml");
        using var w = neu.Open();
        destDoc.Save(w);
        Log("injected dxfs children=" + clone.Elements().Count());
    }

    private static void DropTableStyles(string path)
    {
        using var z = ZipFile.Open(path, ZipArchiveMode.Update);
        var entry = z.GetEntry("xl/styles.xml")!;
        XDocument doc;
        using (var s = entry.Open())
            doc = XDocument.Load(s);
        doc.Root!.Element(Ns + "tableStyles")?.Remove();
        // also remove dxfs if present (optional, only needed for table styles)
        doc.Root.Element(Ns + "dxfs")?.Remove();
        entry.Delete();
        var neu = z.CreateEntry("xl/styles.xml");
        using var w = neu.Open();
        doc.Save(w);
    }

    private static void Log(string msg)
    {
        File.AppendAllText(LogPath, msg + Environment.NewLine, Encoding.UTF8);
        Console.WriteLine(msg);
    }

    private static void KillExcel()
    {
        foreach (var p in Process.GetProcessesByName("EXCEL"))
            try { p.Kill(true); } catch { }
        Thread.Sleep(700);
    }

    private static string ExcelOpenStatus(string path)
    {
        KillExcel();
        var t = Type.GetTypeFromProgID("Excel.Application");
        dynamic excel = Activator.CreateInstance(t!)!;
        excel.Visible = false;
        excel.DisplayAlerts = false;
        try
        {
            try
            {
                dynamic book = excel.Workbooks.Open(path, 0, true, CorruptLoad: 0);
                book.Close(false);
                return "OK";
            }
            catch (COMException)
            {
                try
                {
                    dynamic book = excel.Workbooks.Open(path, 0, true, CorruptLoad: 1);
                    book.Close(false);
                    return "NEEDS_REPAIR";
                }
                catch (Exception ex)
                {
                    return "FAIL " + ex.Message;
                }
            }
        }
        finally
        {
            try { excel.Quit(); } catch { }
            KillExcel();
        }
    }

    private static byte[] ClosedXmlRoundtrip(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var wb = new XLWorkbook(input);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
