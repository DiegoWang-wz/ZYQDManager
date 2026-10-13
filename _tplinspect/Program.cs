using ClosedXML.Excel;
foreach (var name in new[]{"遮阳报价基础数据导入模板.xlsx","遮阳报价基础数据导入模板推杆类.xlsx"}) {
  var path = Path.Combine(@"d:\JC_Project\ZYQDManager\ZYQDManager\wwwroot\Templates", name);
  using var wb = new XLWorkbook(path);
  var ws = wb.Worksheets.First();
  Console.WriteLine("==== "+name+" ====");
  var lr = Math.Min(4, ws.LastRowUsed()?.RowNumber() ?? 1);
  var lc = Math.Min(20, ws.LastColumnUsed()?.ColumnNumber() ?? 1);
  for (int r=1;r<=lr;r++)
  for (int c=1;c<=lc;c++) {
    var v = ws.Cell(r,c).GetFormattedString();
    if (!string.IsNullOrWhiteSpace(v)) Console.WriteLine($"R{r}C{c}={v}");
  }
}
