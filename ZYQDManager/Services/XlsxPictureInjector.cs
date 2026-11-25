using System.IO.Compression;
using System.Xml.Linq;

namespace ZYQDManager.Services;

/// <summary>要贴到某个单元格上的一张图。</summary>
/// <param name="CellReference">锚点单元格，如 D66（图片左上角对齐该格）。</param>
/// <param name="Data">图片原始字节。</param>
public sealed record XlsxPicture(string CellReference, byte[] Data);

/// <summary>
/// 往现成的 xlsx 里贴图片，只增不改：新增 media 部件、drawings 部件与 rels，
/// 其余部件（模板的 VML 页眉 logo、页面设置、单元格样式）按字节原样搬运。
///
/// 为什么不用 ClosedXML / NPOI：它们保存时会把整个工作簿按自己的对象模型重写，
/// 模板里的 legacyDrawingHF（页眉公司标识）和自定义页面设置会被丢掉，
/// 而规格书模板的这些部件是必保的。
/// </summary>
public static class XlsxPictureInjector
{
    private static readonly XNamespace Xdr = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Ct = "http://schemas.openxmlformats.org/package/2006/content-types";

    /// <summary>96 像素 = 1 英寸 = 914400 EMU。</summary>
    private const double EmuPerPixel = 9525d;

    private const int MaxSidePx = 108;

    public static bool TryInject(
        byte[] xlsx,
        IReadOnlyList<XlsxPicture> pictures,
        out byte[] bytes,
        out string? error)
    {
        bytes = xlsx;
        error = null;

        if (pictures.Count == 0) return true;

        try
        {
            // 1. 全部条目读进内存，后面按名字增删改
            var parts = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            using (var source = new MemoryStream(xlsx, writable: false))
            using (var zip = new ZipArchive(source, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    using var stream = entry.Open();
                    using var buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    parts[entry.FullName] = buffer.ToArray();
                }
            }

            var sheetName = parts.Keys.FirstOrDefault(k =>
                k.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase)
                && k.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                && !k.Contains("_rels", StringComparison.OrdinalIgnoreCase));
            if (sheetName is null)
            {
                error = "xlsx 里没有工作表";
                return false;
            }

            var sheet = XDocument.Parse(GetText(parts, sheetName), LoadOptions.PreserveWhitespace);
            var relsName = RelsPathOf(sheetName);
            var rels = parts.TryGetValue(relsName, out var relsBytes)
                ? XDocument.Parse(GetText(parts, relsName), LoadOptions.PreserveWhitespace)
                : new XDocument(new XElement(Rel + "Relationships"));

            // 2. 找到（或新建）工作表的 drawing 部件
            var drawingName = EnsureDrawingPart(parts, sheet, rels, relsName, out var drawingRid);
            if (drawingName is null)
            {
                error = "无法建立 drawing 部件";
                return false;
            }

            var drawingRelsName = RelsPathOf(drawingName);
            var drawing = parts.TryGetValue(drawingName, out var drawingBytes)
                ? XDocument.Parse(GetText(parts, drawingName), LoadOptions.PreserveWhitespace)
                : new XDocument(new XElement(Xdr + "wsDr",
                    new XAttribute(XNamespace.Xmlns + "xdr", Xdr.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "a", A.NamespaceName)));

            var drawingRels = parts.TryGetValue(drawingRelsName, out var drawingRelsBytes)
                ? XDocument.Parse(GetText(parts, drawingRelsName), LoadOptions.PreserveWhitespace)
                : new XDocument(new XElement(Rel + "Relationships"));

            // 3. 逐张图：media 部件 + drawing 关系 + 锚点
            var nextMedia = 1;
            var nextShapeId = NextShapeId(drawing);
            var nextRelId = NextRelId(drawingRels);

            foreach (var picture in pictures)
            {
                if (picture.Data.Length == 0) continue;

                var (ext, contentType) = DetectFormat(picture.Data);

                string mediaName;
                do
                {
                    mediaName = "xl/media/remote_photo_" + nextMedia++ + "." + ext;
                } while (parts.ContainsKey(mediaName));

                parts[mediaName] = picture.Data;

                var relId = "rId" + nextRelId++;
                drawingRels.Root!.Add(new XElement(Rel + "Relationship",
                    new XAttribute("Id", relId),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image"),
                    new XAttribute("Target", "../media/" + Path.GetFileName(mediaName))));

                var (col, row) = ParseCell(picture.CellReference);
                var (cx, cy) = FitSize(picture.Data, MaxSidePx);
                var inset = Math.Round(4 * EmuPerPixel);

                drawing.Root!.Add(BuildAnchor(nextShapeId++, col, row, inset, cx, cy, relId));
                EnsureDefaultContentType(parts, ext, contentType);
            }

            EnsureDrawingContentType(parts, drawingName);

            // 4. 写回
            parts[sheetName] = ToBytes(sheet);
            parts[relsName] = ToBytes(rels);
            parts[drawingName] = ToBytes(drawing);
            if (drawingRels.Root!.Elements().Any())
                parts[drawingRelsName] = ToBytes(drawingRels);

            using var output = new MemoryStream();
            using (var target = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                foreach (var (name, data) in parts)
                {
                    var entry = target.CreateEntry(name, CompressionLevel.Optimal);
                    using var stream = entry.Open();
                    stream.Write(data, 0, data.Length);
                }
            }

            bytes = output.ToArray();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
            bytes = xlsx;
            return false;
        }
    }

    // ==================== 部件定位 ====================

    private static string RelsPathOf(string partName)
    {
        var dir = Path.GetDirectoryName(partName)?.Replace('\\', '/') ?? "";
        var file = Path.GetFileName(partName);
        return dir + "/_rels/" + file + ".rels";
    }

    private static string? EnsureDrawingPart(
        Dictionary<string, byte[]> parts,
        XDocument sheet,
        XDocument rels,
        string relsName,
        out string? rid)
    {
        rid = null;
        var root = sheet.Root;
        if (root is null) return null;

        var existing = root.Elements(Main + "drawing").FirstOrDefault();
        if (existing is not null)
        {
            var currentRid = (string?)existing.Attribute(R + "id");
            rid = currentRid;
            var found = rels.Root?.Elements()
                .FirstOrDefault(e => (string?)e.Attribute("Id") == currentRid);
            if (found is not null) return ResolveTarget((string?)found.Attribute("Target"));
        }

        var drawingName = "xl/drawings/drawing1.xml";
        var i = 1;
        while (parts.ContainsKey(drawingName))
            drawingName = "xl/drawings/drawing" + ++i + ".xml";

        rid = "rId" + NextRelId(rels);
        rels.Root!.Add(new XElement(Rel + "Relationship",
            new XAttribute("Id", rid),
            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing"),
            new XAttribute("Target", "../drawings/" + Path.GetFileName(drawingName))));

        // <drawing> 必须排在 legacyDrawing / legacyDrawingHF / controls / extLst 之前
        var drawingElement = new XElement(Main + "drawing", new XAttribute(R + "id", rid));
        var successors = new[] { "legacyDrawing", "legacyDrawingHF", "picture", "oleObjects", "controls", "webPublishItems", "tableParts", "extLst" };
        var before = root.Elements().FirstOrDefault(e => successors.Contains(e.Name.LocalName));
        if (before is not null) before.AddBeforeSelf(drawingElement);
        else root.Add(drawingElement);

        return drawingName.TrimStart('/');

        static string? ResolveTarget(string? target)
        {
            if (string.IsNullOrWhiteSpace(target)) return null;
            var value = target.Replace("../", "xl/").TrimStart('/');
            return value.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? value : "xl/" + value;
        }
    }

    // ==================== drawing XML ====================

    private static XElement BuildAnchor(int shapeId, int col, int row, double inset, long cx, long cy, string relId)
        => new(Xdr + "oneCellAnchor",
            new XElement(Xdr + "from",
                new XElement(Xdr + "col", col),
                new XElement(Xdr + "colOff", (long)inset),
                new XElement(Xdr + "row", row),
                new XElement(Xdr + "rowOff", (long)inset)),
            new XElement(Xdr + "ext", new XAttribute("cx", cx), new XAttribute("cy", cy)),
            new XElement(Xdr + "pic",
                new XElement(Xdr + "nvPicPr",
                    new XElement(Xdr + "cNvPr", new XAttribute("id", shapeId), new XAttribute("name", "RemotePhoto" + shapeId)),
                    new XElement(Xdr + "cNvPicPr",
                        new XElement(A + "picLocks", new XAttribute("noChangeAspect", "1")))),
                new XElement(Xdr + "blipFill",
                    new XElement(A + "blip", new XAttribute(R + "embed", relId)),
                    new XElement(A + "stretch", new XElement(A + "fillRect"))),
                new XElement(Xdr + "spPr",
                    new XElement(A + "xfrm",
                        new XElement(A + "off", new XAttribute("x", 0), new XAttribute("y", 0)),
                        new XElement(A + "ext", new XAttribute("cx", cx), new XAttribute("cy", cy))),
                    new XElement(A + "prstGeom", new XAttribute("prst", "rect"),
                        new XElement(A + "avLst")))),
            new XElement(Xdr + "clientData"));

    private static int NextShapeId(XDocument drawing)
    {
        var ids = drawing.Descendants()
            .Where(e => e.Name.LocalName == "cNvPr")
            .Select(e => int.TryParse((string?)e.Attribute("id"), out var v) ? v : 1)
            .DefaultIfEmpty(1)
            .ToList();
        return ids.Max() + 1;
    }

    private static int NextRelId(XDocument rels)
    {
        var ids = rels.Root?
            .Elements()
            .Select(e => (string?)e.Attribute("Id") ?? "")
            .Where(x => x.StartsWith("rId", StringComparison.OrdinalIgnoreCase))
            .Select(x => int.TryParse(x[3..], out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max() ?? 0;
        return ids + 1;
    }

    // ==================== content types ====================

    private static void EnsureDefaultContentType(Dictionary<string, byte[]> parts, string ext, string contentType)
    {
        if (!parts.TryGetValue("[Content_Types].xml", out _)) return;

        var doc = XDocument.Parse(GetText(parts, "[Content_Types].xml"), LoadOptions.PreserveWhitespace);
        var exists = doc.Root!.Elements(Ct + "Default")
            .Any(e => string.Equals((string?)e.Attribute("Extension"), ext, StringComparison.OrdinalIgnoreCase));
        if (!exists)
        {
            doc.Root.Add(new XElement(Ct + "Default",
                new XAttribute("Extension", ext),
                new XAttribute("ContentType", contentType)));
            parts["[Content_Types].xml"] = ToBytes(doc);
        }
    }

    private static void EnsureDrawingContentType(Dictionary<string, byte[]> parts, string drawingName)
    {
        if (!parts.TryGetValue("[Content_Types].xml", out _)) return;

        var doc = XDocument.Parse(GetText(parts, "[Content_Types].xml"), LoadOptions.PreserveWhitespace);
        var partName = "/" + drawingName.TrimStart('/');
        var exists = doc.Root!.Elements(Ct + "Override")
            .Any(e => string.Equals((string?)e.Attribute("PartName"), partName, StringComparison.OrdinalIgnoreCase));
        if (!exists)
        {
            doc.Root.Add(new XElement(Ct + "Override",
                new XAttribute("PartName", partName),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.drawing+xml")));
            parts["[Content_Types].xml"] = ToBytes(doc);
        }
    }

    // ==================== 图片格式与尺寸 ====================

    private static (string Ext, string ContentType) DetectFormat(byte[] data)
    {
        if (data.Length > 3 && data[0] == 0xFF && data[1] == 0xD8) return ("jpeg", "image/jpeg");
        if (data.Length > 3 && data[0] == 0x89 && data[1] == 0x50) return ("png", "image/png");
        if (data.Length > 3 && data[0] == 0x47 && data[1] == 0x49) return ("gif", "image/gif");
        if (data.Length > 2 && data[0] == 0x42 && data[1] == 0x4D) return ("bmp", "image/bmp");
        return ("png", "image/png");
    }

    private static (long Cx, long Cy) FitSize(byte[] data, int maxSidePx)
    {
        var (w, h) = ImageSize(data);
        if (w <= 0 || h <= 0) w = h = maxSidePx;

        var scale = Math.Min(1d * maxSidePx / w, 1d * maxSidePx / h);
        var cx = Math.Max(1, (long)Math.Round(w * scale * EmuPerPixel));
        var cy = Math.Max(1, (long)Math.Round(h * scale * EmuPerPixel));
        return (cx, cy);
    }

    /// <summary>读 PNG / JPEG / GIF / BMP 的像素尺寸，读不出返回 (0,0) 由调用方兜底。</summary>
    private static (int Width, int Height) ImageSize(byte[] d)
    {
        try
        {
            // PNG：IHDR 宽高在 16~23 字节，大端
            if (d.Length > 24 && d[0] == 0x89 && d[1] == 0x50)
            {
                var w = (d[16] << 24) | (d[17] << 16) | (d[18] << 8) | d[19];
                var h = (d[20] << 24) | (d[21] << 16) | (d[22] << 8) | d[23];
                return (w, h);
            }

            // GIF：6~9 字节，小端
            if (d.Length > 10 && d[0] == 0x47 && d[1] == 0x49)
            {
                var w = d[6] | (d[7] << 8);
                var h = d[8] | (d[9] << 8);
                return (w, h);
            }

            // BMP：18~25 字节，小端
            if (d.Length > 26 && d[0] == 0x42 && d[1] == 0x4D)
            {
                var w = BitConverter.ToInt32(d, 18);
                var h = Math.Abs(BitConverter.ToInt32(d, 22));
                return (w, h);
            }

            // JPEG：扫 SOF0/SOF1/SOF2 等段
            if (d.Length > 4 && d[0] == 0xFF && d[1] == 0xD8)
            {
                var i = 2;
                while (i + 9 < d.Length)
                {
                    if (d[i] != 0xFF) { i++; continue; }
                    var marker = d[i + 1];
                    if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB)
                    {
                        var h = (d[i + 5] << 8) | d[i + 6];
                        var w = (d[i + 7] << 8) | d[i + 8];
                        return (w, h);
                    }

                    var length = (d[i + 2] << 8) | d[i + 3];
                    if (length < 2) break;
                    i += 2 + length;
                }
            }
        }
        catch
        {
            // 尺寸读不出来就用默认值
        }

        return (0, 0);
    }

    // ==================== 其它 ====================

    private static (int Col, int Row) ParseCell(string reference)
    {
        var letters = 0;
        var col = 0;
        while (letters < reference.Length && char.IsLetter(reference[letters]))
        {
            col = col * 26 + (char.ToUpperInvariant(reference[letters]) - 'A' + 1);
            letters++;
        }

        var row = int.TryParse(reference[letters..], out var r) ? r : 1;
        return (Math.Max(0, col - 1), Math.Max(0, row - 1));
    }

    /// <summary>
    /// 取出 xml 部件的文本。模板里这些部件带 UTF-8 BOM，GetString 之后 BOM 会变成 U+FEFF 字符，
    /// 直接交给 XDocument.Parse 会抛 "Data at the root level is invalid"，所以先剥掉。
    /// </summary>
    private static string GetText(Dictionary<string, byte[]> parts, string name)
    {
        var text = System.Text.Encoding.UTF8.GetString(parts[name]);
        return text.Length > 0 && text[0] == '\uFEFF' ? text[1..] : text;
    }

    private static byte[] ToBytes(XDocument document)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 4096, true))
        {
            document.Save(writer, SaveOptions.DisableFormatting);
        }

        return stream.ToArray();
    }
}
