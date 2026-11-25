using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace ZYQDManager.Services;

/// <summary>
/// 往现成的 xlsx 模板里填值：只改 <c>xl/worksheets/sheet*.xml</c> 里目标单元格的 XML，
/// 其余部件（logo 图片、drawing、vmlDrawing、ctrlProps、styles）按原始字节搬运。
///
/// 为什么不用 OpenXML SDK 的 SpreadsheetDocument：SDK 保存时会把整张工作表按自己的模型重写，
/// 模板里那些 x14 扩展的「表单复选框控件」(<c>&lt;controls&gt;</c> + ctrlProps + VML) 会被改坏，
/// 导出的 Excel 里控件显示成破损方块。XDocument 只做局部增删，未知元素原样保留；
/// 其它 zip 条目干脆不解析，字节级复制。
///
/// 约定：
/// 1. 值为空时**不写**，保留模板里的原始说明文案；
/// 2. 合并单元格的值写到左上角（如 C8:F8 写 C8）；
/// 3. 统一用 inlineStr，不动 SharedStringTable；
/// 4. 顺手把页面设置改成「适应一页宽」，避免 PDF 右侧被裁。
/// </summary>
public static class ExcelTemplateWriter
{
    /// <summary>勾选后的方框字符：打勾的方框 ☑，与未选中的空方框 ☐ 同属一组字符。</summary>
    private const char CheckedMark = '☑';

    /// <summary>未勾选的空方框 ☐（U+2610）。</summary>
    private const char UncheckedMark = '☐';

    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Xml = XNamespace.Xml;

    private static readonly IReadOnlyDictionary<string, string?> Empty = new Dictionary<string, string?>();

    /// <summary>最近一次失败的原因，供调用方记日志排查。</summary>
    public static string? LastError { get; private set; }

    public static bool TryFill(
        string? templatePath,
        IReadOnlyDictionary<string, string?> cells,
        out byte[] bytes)
        => TryFill(templatePath, cells, null, null, out bytes);

    /// <summary>
    /// 三种写法一起用：
    /// <paramref name="cells"/> 直接覆盖（空白格、单值格）；
    /// <paramref name="checks"/> 勾选型（值为逗号分隔的选项），在模板原有的 □ 上打勾，没有方框就在选项前补一个；
    /// <paramref name="appends"/> 填空型（「Other____」「（自定义填写）」这类）：优先写到模板的下划线上，
    /// 模板没留位置时才另起一行追加。
    /// </summary>
    public static bool TryFill(
        string? templatePath,
        IReadOnlyDictionary<string, string?> cells,
        IReadOnlyDictionary<string, string?>? checks,
        IReadOnlyDictionary<string, string?>? appends,
        out byte[] bytes)
        => TryFill(templatePath, cells, checks, appends, null, out bytes);

    /// <summary>
    /// 相比上一个重载多一个 <paramref name="candidates"/>：坐标 → 该字段的全部可选值（竖线分隔）。
    /// 模板格子里本来就印着选项的，用模板原文打勾；模板是空白的（立项单那几行），
    /// 就按候选列表整排画出来，未选中的也留 ☐，免得单据上只剩一个值看不出可选项。
    /// </summary>
    public static bool TryFill(
        string? templatePath,
        IReadOnlyDictionary<string, string?> cells,
        IReadOnlyDictionary<string, string?>? checks,
        IReadOnlyDictionary<string, string?>? appends,
        IReadOnlyDictionary<string, string?>? candidates,
        out byte[] bytes)
    {
        bytes = [];
        LastError = null;

        if (string.IsNullOrWhiteSpace(templatePath))
        {
            LastError = "模板路径为空";
            return false;
        }

        if (!File.Exists(templatePath))
        {
            LastError = "模板不存在：" + templatePath;
            return false;
        }

        try
        {
            var raw = File.ReadAllBytes(templatePath);
            using var source = new MemoryStream(raw, writable: false);
            using var zip = new ZipArchive(source, ZipArchiveMode.Read);

            var sheetEntry = zip.Entries.FirstOrDefault(e => IsWorksheetEntry(e.FullName));
            if (sheetEntry is null)
            {
                LastError = "模板里没有工作表";
                return false;
            }

            XDocument sheet;
            using (var stream = sheetEntry.Open())
            {
                sheet = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            }

            var sheetData = sheet.Root?.Element(Ns + "sheetData");
            if (sheetData is null)
            {
                LastError = "模板工作表没有 sheetData";
                return false;
            }

            var writer = new CellWriter(sheetData, LoadSharedStrings(zip));

            foreach (var (reference, text) in cells)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                writer.Write(reference, text.Trim());
            }

            foreach (var (reference, optionText) in checks ?? Empty)
            {
                var options = SplitOptions(optionText);
                if (options.Count == 0) continue;

                var list = Array.Empty<string>();
                if (candidates is not null && candidates.TryGetValue(reference, out var candidateText))
                {
                    list = (candidateText ?? "")
                        .Split(['|', '、'], StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .ToArray();
                }

                writer.Write(reference, MarkOptions(writer.Read(reference), options, list));
            }

            foreach (var (reference, text) in appends ?? Empty)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                writer.Write(reference, MergeAppend(writer.Read(reference), text.Trim()));
            }

            FitToOnePageWidth(sheet);

            var patched = new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase)
            {
                [sheetEntry.FullName] = sheet
            };
            var dropped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            StripFormControls(zip, sheetEntry.FullName, patched, dropped);

            using var output = new MemoryStream();
            using (var target = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                foreach (var entry in zip.Entries)
                {
                    if (dropped.Contains(entry.FullName)) continue;

                    var copy = target.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                    using var destination = copy.Open();
                    if (patched.TryGetValue(entry.FullName, out var document))
                    {
                        document.Save(destination);
                        continue;
                    }

                    using var origin = entry.Open();
                    origin.CopyTo(destination);
                }
            }

            bytes = output.ToArray();
            return bytes.Length > 0;
        }
        catch (Exception ex)
        {
            LastError = ex.GetType().Name + ": " + ex.Message;
            return false;
        }
    }

    // ==================== 工作表读写 ====================

    private static bool IsWorksheetEntry(string name)
        => name.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase)
           && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
           && !name.Contains("_rels", StringComparison.OrdinalIgnoreCase);

    private static string[] LoadSharedStrings(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return [];

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document.Root?
                   .Elements(Ns + "si")
                   .Select(si => string.Concat(si.Descendants(Ns + "t").Select(t => t.Value)))
                   .ToArray()
               ?? [];
    }

    // ==================== 移除表单复选框控件 ====================

    /// <summary>
    /// 模板用 Excel 的「复选框控件」做勾选。这些控件是浮在单元格上的独立图形，锚点钉死在
    /// 下划线的空白处，还带着模板作者原来的勾选状态（有几行默认就是勾上的）。数据填进去以后
    /// 控件必然和文字错位、重复，所以导出时整组去掉，勾选状态改由单元格里的 ■ / □ 表达。
    ///
    /// 只处理真的有 ctrlProps 的模板：规格书 / 承认书模板里的 VML 是页眉 logo（legacyDrawingHF），
    /// 删了会丢公司标识。
    /// </summary>
    private static void StripFormControls(
        ZipArchive zip,
        string sheetName,
        Dictionary<string, XDocument> patched,
        HashSet<string> dropped)
    {
        var hasControls = zip.Entries.Any(e =>
            e.FullName.StartsWith("xl/ctrlProps/", StringComparison.OrdinalIgnoreCase));
        if (!hasControls) return;

        // 1) 工作表：去掉 <controls>（被 mc:AlternateContent 包着）和 legacyDrawing 引用
        if (patched.TryGetValue(sheetName, out var sheet) && sheet.Root is { } root)
        {
            foreach (var child in root.Elements().ToList())
            {
                if (child.Name.LocalName == "legacyDrawing")
                {
                    child.Remove();
                    continue;
                }

                if (child.Name.LocalName == "AlternateContent"
                    && child.ToString(SaveOptions.DisableFormatting).Contains("<controls", StringComparison.Ordinal))
                {
                    child.Remove();
                }
            }
        }

        // 2) 工作表关系：去掉指向 vmlDrawing / ctrlProps 的 Relationship
        var relsName = RelsPathOf(sheetName);
        var relsEntry = zip.GetEntry(relsName);
        if (relsEntry is not null)
        {
            var rels = LoadDocument(relsEntry);
            if (rels.Root is { } relsRoot)
            {
                foreach (var rel in relsRoot.Elements().ToList())
                {
                    var type = (string?)rel.Attribute("Type") ?? "";
                    if (type.EndsWith("/vmlDrawing", StringComparison.OrdinalIgnoreCase)
                        || type.Contains("/ctrlProp", StringComparison.OrdinalIgnoreCase))
                    {
                        rel.Remove();
                    }
                }

                patched[relsName] = rels;
            }
        }

        // 3) drawing：Excel 给每个控件留了个 hidden 的形状，一并删掉
        foreach (var entry in zip.Entries.Where(e =>
                     e.FullName.StartsWith("xl/drawings/drawing", StringComparison.OrdinalIgnoreCase)
                     && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            var drawing = LoadDocument(entry);
            if (drawing.Root is null) continue;

            var placeholders = drawing.Root
                .Descendants()
                .Where(e => e.Name.LocalName is "twoCellAnchor" or "oneCellAnchor" or "absoluteAnchor")
                .Where(a => (a.Descendants().FirstOrDefault(d => d.Name.LocalName == "cNvPr")
                                 ?.Attribute("name")?.Value ?? "")
                    .StartsWith("Check Box", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (placeholders.Count == 0) continue;

            foreach (var anchor in placeholders)
                anchor.Remove();

            patched[entry.FullName] = drawing;
        }

        // 4) 部件本体与 [Content_Types] 里的登记一并清掉
        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.StartsWith("xl/ctrlProps/", StringComparison.OrdinalIgnoreCase)
                || (entry.FullName.StartsWith("xl/drawings/vmlDrawing", StringComparison.OrdinalIgnoreCase)
                    && entry.FullName.EndsWith(".vml", StringComparison.OrdinalIgnoreCase)))
            {
                dropped.Add(entry.FullName);
            }
        }

        var typesEntry = zip.GetEntry("[Content_Types].xml");
        if (typesEntry is null) return;

        var types = LoadDocument(typesEntry);
        if (types.Root is null) return;

        foreach (var item in types.Root.Elements().ToList())
        {
            var partName = (string?)item.Attribute("PartName") ?? "";
            if (partName.StartsWith("/xl/ctrlProps/", StringComparison.OrdinalIgnoreCase)
                || partName.StartsWith("/xl/drawings/vmlDrawing", StringComparison.OrdinalIgnoreCase))
            {
                item.Remove();
            }
        }

        patched[typesEntry.FullName] = types;
    }

    private static XDocument LoadDocument(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
    }

    /// <summary>xl/worksheets/sheet1.xml → xl/worksheets/_rels/sheet1.xml.rels</summary>
    private static string RelsPathOf(string partName)
    {
        var slash = partName.LastIndexOf('/');
        var dir = slash < 0 ? "" : partName[..slash];
        var file = slash < 0 ? partName : partName[(slash + 1)..];
        return $"{dir}/_rels/{file}.rels";
    }

    /// <summary>把单元格当「坐标 → 文本」读写；只碰 sheetData，其余节点原样保留。</summary>
    private sealed class CellWriter(XElement sheetData, string[] sharedStrings)
    {
        public string? Read(string reference)
        {
            var cell = Find(reference);
            if (cell is null) return null;

            var type = (string?)cell.Attribute("t");
            if (type == "inlineStr")
                return cell.Element(Ns + "is")?.Value;

            var value = cell.Element(Ns + "v")?.Value;
            if (type == "s" && int.TryParse(value, out var index)
                && index >= 0 && index < sharedStrings.Length)
                return sharedStrings[index];

            return value;
        }

        public void Write(string reference, string text)
        {
            var (column, rowIndex) = SplitReference(reference);
            if (rowIndex <= 0 || column.Length == 0) return;

            var row = sheetData.Elements(Ns + "row")
                .FirstOrDefault(r => (int?)r.Attribute("r") == rowIndex);
            if (row is null) return;

            var cell = row.Elements(Ns + "c")
                .FirstOrDefault(c => string.Equals((string?)c.Attribute("r"), reference, StringComparison.OrdinalIgnoreCase));

            if (cell is null)
            {
                cell = new XElement(Ns + "c", new XAttribute("r", reference));
                var target = ColumnIndex(column);
                var before = row.Elements(Ns + "c")
                    .FirstOrDefault(c => ColumnIndex(ColumnOf((string?)c.Attribute("r"))) > target);
                if (before is null) row.Add(cell);
                else before.AddBeforeSelf(cell);
            }

            // 清掉旧的共享字符串引用 / 公式，换成 inlineStr，样式索引 s 保持不动
            cell.Elements(Ns + "v").Remove();
            cell.Elements(Ns + "f").Remove();
            cell.Elements(Ns + "is").Remove();
            cell.SetAttributeValue("t", "inlineStr");
            cell.Add(new XElement(Ns + "is",
                new XElement(Ns + "t", new XAttribute(Xml + "space", "preserve"), text)));
        }

        private XElement? Find(string reference)
            => sheetData.Elements(Ns + "row")
                .SelectMany(r => r.Elements(Ns + "c"))
                .FirstOrDefault(c => string.Equals((string?)c.Attribute("r"), reference, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>PDF 右侧被裁的根因：模板按固定缩放打印，表格比 A4 宽。改成适应一页宽。</summary>
    private static void FitToOnePageWidth(XDocument sheet)
    {
        var root = sheet.Root;
        if (root is null) return;

        var sheetPr = root.Element(Ns + "sheetPr");
        if (sheetPr is null)
        {
            sheetPr = new XElement(Ns + "sheetPr");
            root.AddFirst(sheetPr);
        }

        var pageSetupPr = sheetPr.Element(Ns + "pageSetUpPr");
        if (pageSetupPr is null)
        {
            pageSetupPr = new XElement(Ns + "pageSetUpPr");
            sheetPr.Add(pageSetupPr);
        }

        pageSetupPr.SetAttributeValue("fitToPage", "1");

        var pageSetup = root.Element(Ns + "pageSetup");
        if (pageSetup is null) return;

        pageSetup.SetAttributeValue("fitToWidth", "1");
        pageSetup.SetAttributeValue("fitToHeight", "0");
        pageSetup.Attribute("scale")?.Remove();
    }

    // ==================== 填空：写到模板的下划线上 ====================

    private static readonly string[] CustomHints = ["（自定义填写）", "(自定义填写)", "自定义填写"];

    /// <summary>
    /// 「□Other________」「□其它Other（自定义填写）」这类格子，模板已经印好了填写位置。
    /// 有下划线就把值写在横线上（两侧留等长下划线，像手写填进去），
    /// 没有下划线但有「自定义填写」字样就替换那几个字，两者都没有才另起一行追加。
    /// </summary>
    private static string MergeAppend(string? current, string text)
    {
        if (string.IsNullOrWhiteSpace(current)) return text;

        if (FindUnderline(current) is { } blank)
        {
            var padTotal = Math.Max(0, blank.Length - text.Length);
            var left = padTotal / 2;
            var right = padTotal - left;
            var filled = new string('_', left) + text + new string('_', right);
            return current[..blank.Start] + filled + current[(blank.Start + blank.Length)..];
        }

        foreach (var hint in CustomHints)
        {
            var index = current.IndexOf(hint, StringComparison.Ordinal);
            if (index < 0) continue;
            return current[..index] + " " + text + current[(index + hint.Length)..];
        }

        return current.TrimEnd() + "\n" + text;
    }

    /// <summary>找连续 3 个以上的下划线（模板的填写横线）。</summary>
    private static (int Start, int Length)? FindUnderline(string text)
    {
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] != '_')
            {
                i++;
                continue;
            }

            var start = i;
            while (i < text.Length && text[i] == '_') i++;
            if (i - start >= 3) return (start, i - start);
        }

        return null;
    }

    // ==================== 勾选：在模板原有说明上打勾 ====================

    /// <summary>
    /// 模板格子里的「□433.92 MHz  □447.x MHz」这类说明原样保留，
    /// 只把用户选中的选项前面换成 ☑；模板里没方框的（「室内　室外」）就在选项前补一个。
    /// 一个都没对上时补在末尾，数据不丢。
    /// </summary>
    private static string MarkOptions(
        string? templateText,
        IReadOnlyList<string> options,
        IReadOnlyList<string> candidates)
    {
        if (string.IsNullOrWhiteSpace(templateText))
        {
            // 模板这一行是空白的：按候选整排画出来，未选中的留 ☐
            if (candidates.Count == 0)
                return string.Join("  ", options.Select(o => CheckedMark + o));

            var rendered = candidates
                .Select(c => (options.Any(o => SameOption(o, c)) ? CheckedMark : UncheckedMark) + c)
                .ToList();

            // 候选里没有的自由填写值补在末尾，别丢
            var extras = options.Where(o => !candidates.Any(c => SameOption(o, c))).ToList();
            if (extras.Count > 0)
                rendered.Add(string.Join("  ", extras.Select(o => CheckedMark + o)));

            return string.Join("  ", rendered);
        }

        var chars = templateText.ToCharArray();
        var inserts = new List<int>();
        var hits = 0;

        foreach (var option in options)
        {
            var span = FindOption(chars, option);
            if (span is null) continue;

            // 每个选项都以方框开头，往前找最近的方框就是它自己的；
            // 中间隔着「是 JST」「（Li-ion）」这类前后缀文字也照样认。
            var mark = FindMarkBefore(chars, span.Value.Start);
            if (mark >= 0)
            {
                chars[mark] = CheckedMark;
                hits++;
                continue;
            }

            inserts.Add(span.Value.Start);
            hits++;
        }

        var result = new string(chars);
        foreach (var index in inserts.OrderByDescending(x => x))
            result = result.Insert(index, CheckedMark.ToString());

        if (hits == 0)
            result += "\n" + string.Join("  ", options.Select(o => CheckedMark + o));

        // 未勾选的方框统一成 ☐（U+2610），与已勾选的 ☑（U+2611）同属一个字符区段，
        // 字体回落一致，大小和基线才对得齐；模板里残留的 '£' 乱码方框也一并归一。
        return result.Replace('£', '☐').Replace('□', '☐');
    }

    private static (int Start, int Length)? FindOption(char[] text, string option)
    {
        var source = new string(text);

        foreach (var needle in Needles(option))
        {
            var exact = source.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (exact >= 0) return (exact, needle.Length);
        }

        // 模板里的选项常带不定长空格，压缩空白后再比一次
        var map = new List<int>(source.Length);
        var compact = new StringBuilder(source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            if (char.IsWhiteSpace(source[i])) continue;
            map.Add(i);
            compact.Append(char.ToLowerInvariant(source[i]));
        }

        var flat = compact.ToString();
        foreach (var needle in Needles(option))
        {
            var target = new string(needle.Where(c => !char.IsWhiteSpace(c))
                .Select(char.ToLowerInvariant).ToArray());
            if (target.Length == 0) continue;

            var pos = flat.IndexOf(target, StringComparison.Ordinal);
            if (pos >= 0)
                return (map[pos], map[pos + target.Length - 1] - map[pos] + 1);
        }

        return null;
    }

    /// <summary>同一选项的几种写法：原文、中文片段、英文片段（页面上「With Plug 有」对应模板「□With Plug 是」）。</summary>
    private static IEnumerable<string> Needles(string option)
    {
        var raw = option.Trim();
        if (raw.Length == 0) yield break;
        yield return raw;

        var cn = new string(raw.Where(IsChinese).ToArray());
        if (cn.Length >= 2 && !string.Equals(cn, raw, StringComparison.Ordinal))
            yield return cn;

        var en = new string(raw
                .Where(c => c < 128 && (char.IsLetterOrDigit(c) || c == ' ' || c == '.' || c == '/' || c == '-'))
                .ToArray())
            .Trim();
        if (en.Length >= 2
            && !string.Equals(en, raw, StringComparison.Ordinal)
            && !string.Equals(en, cn, StringComparison.Ordinal))
            yield return en;
    }

    /// <summary>选项与候选是否是同一个东西：忽略空白与大小写，允许「Standard」对上「捷昌Standard」。</summary>
    private static bool SameOption(string option, string candidate)
    {
        static string Normalize(string value)
            => new(value.Where(c => !char.IsWhiteSpace(c)).ToArray());

        var a = Normalize(option).ToLowerInvariant();
        var b = Normalize(candidate).ToLowerInvariant();
        if (a.Length == 0 || b.Length == 0) return false;
        return a == b || b.Contains(a, StringComparison.Ordinal) || a.Contains(b, StringComparison.Ordinal);
    }

    private static bool IsChinese(char c) => c >= 0x4E00 && c <= 0x9FFF;

    private static bool IsMark(char c) => c is '□' or '£' or '口' or '☐' or '◻' or '■' or '☒' or '☑' or '√';

    /// <summary>匹配点往前 12 个字符内最近的方框；模板里最长的选项块也不会超过这个跨度。</summary>
    private static int FindMarkBefore(char[] chars, int start)
    {
        for (var i = start - 1; i >= 0 && i >= start - 12; i--)
        {
            if (IsMark(chars[i])) return i;
        }

        return -1;
    }

    private static List<string> SplitOptions(string? raw)
        => (raw ?? "")
            .Split([',', '，', ';', '；', '、', '|'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();

    // ==================== 坐标工具 ====================

    private static (string Column, int Row) SplitReference(string reference)
    {
        var i = 0;
        while (i < reference.Length && char.IsLetter(reference[i])) i++;
        var column = reference[..i].ToUpperInvariant();
        var rest = reference[i..];
        return int.TryParse(rest, out var row) ? (column, row) : (column, 0);
    }

    private static string ColumnOf(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return "";
        var i = 0;
        while (i < reference.Length && char.IsLetter(reference[i])) i++;
        return reference[..i].ToUpperInvariant();
    }

    /// <summary>A=1, B=2 ... Z=26, AA=27（1 基，便于比较顺序）。</summary>
    private static int ColumnIndex(string columnName)
    {
        var index = 0;
        foreach (var ch in columnName)
        {
            if (ch < 'A' || ch > 'Z') return 0;
            index = index * 26 + (ch - 'A' + 1);
        }

        return index;
    }
}
