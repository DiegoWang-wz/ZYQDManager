namespace ZYQDManager.Utilities;

/// <summary>
/// 老表单系统图片仍在 http://192.168.3.240:3788 ，库里存的是 /Img/... 相对路径。
/// </summary>
public static class LegacyFileUrl
{
    public const string DefaultBase = "http://192.168.3.240:3788";

    public static bool IsBlankOrPlaceholder(string? path)
    {
        var p = (path ?? "").Trim();
        return p.Length == 0
               || p.Equals("N/A", StringComparison.OrdinalIgnoreCase)
               || p.Equals("-", StringComparison.OrdinalIgnoreCase)
               || p.Equals("NULL", StringComparison.OrdinalIgnoreCase);
    }

    public static string Resolve(string? path, string? baseUrl = null)
    {
        var p = (path ?? "").Trim();
        if (IsBlankOrPlaceholder(p)) return "";
        if (p.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return p;

        var host = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBase : baseUrl.Trim().TrimEnd('/');
        if (p.StartsWith("~/")) p = p[1..];
        if (!p.StartsWith('/')) p = "/" + p;
        return host + p;
    }
}
