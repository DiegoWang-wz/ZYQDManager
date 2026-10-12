using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

var src = @"d:\JC_Project\ZYQDManager\ZYQDManager\wwwroot\Templates\装箱单模板-报关.xlsx";
var rtDir = @"d:\JC_Project\ZYQDManager\_xlprobe";
var rt = Directory.GetFiles(rtDir, "rt_*.xlsx").OrderByDescending(File.GetLastWriteTime).First(f => {
  // pick the 报关 one by comparing after roundtrip size or by re-creating
  return true;
});
// recreate RT specifically
using (var wb = new ClosedXML.Excel.XLWorkbook(src))
using (var ms = new MemoryStream()) { wb.SaveAs(ms); File.WriteAllBytes(Path.Combine(rtDir, "rt_baoguan.xlsx"), ms.ToArray()); }
rt = Path.Combine(rtDir, "rt_baoguan.xlsx");
Console.WriteLine($"SRC {(new FileInfo(src).Length)} RT {(new FileInfo(rt).Length)}");
void Dump(string path, string label) {
  using var z = ZipFile.OpenRead(path);
  foreach (var e in z.Entries.OrderByDescending(x => x.Length).Take(12))
    Console.WriteLine($"{label} {e.Length,10} {e.FullName}");
  using var doc = SpreadsheetDocument.Open(path, false);
  var v = new OpenXmlValidator();
  var errs = v.Validate(doc).Take(20).ToList();
  Console.WriteLine($"{label} validator errors: {errs.Count}");
  foreach (var err in errs.Take(15))
    Console.WriteLine($"  {err.ErrorType} {err.Description} PART={err.Part?.Uri} PATH={err.Path?.XPath}");
}
Dump(src, "SRC");
Dump(rt, "RT ");
