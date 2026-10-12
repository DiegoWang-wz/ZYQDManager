using ClosedXML.Excel;
var path = args[0];
var outPath = args[1];
using var wb = new XLWorkbook(path);
using var ms = new MemoryStream();
wb.SaveAs(ms);
await File.WriteAllBytesAsync(outPath, ms.ToArray());
Console.WriteLine("OK bytes=" + ms.Length);
