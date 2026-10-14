namespace ZYQDManager.Utilities;

/// <summary>
/// 合同基础「负责人」字段：多工号串的拆分/追加/删除。
/// 兼容 / 、, 、; 、| 、空白、中文顿号等常见分隔符；写回时尽量沿用原串主分隔符，默认 /。
/// </summary>
public static class CustomerManagerText
{
    public static readonly char[] Separators = ['/', ',', ';', '|', ' ', '\t', '、', '，', '；'];

    public static List<string> Split(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return raw
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool ContainsEmpNo(string? raw, string? empNo)
    {
        empNo = (empNo ?? "").Trim();
        if (empNo.Length == 0) return false;
        return Split(raw).Any(x => string.Equals(x, empNo, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>追加工号（已存在则原样返回）。</summary>
    public static string? AddEmpNo(string? raw, string? empNo)
    {
        empNo = (empNo ?? "").Trim();
        if (empNo.Length == 0) return NormalizeEmpty(raw);

        var list = Split(raw);
        if (list.Any(x => string.Equals(x, empNo, StringComparison.OrdinalIgnoreCase)))
            return Join(list, DetectSeparator(raw));

        list.Add(empNo);
        return Join(list, DetectSeparator(raw));
    }

    /// <summary>删除工号；删空则返回 null。</summary>
    public static string? RemoveEmpNo(string? raw, string? empNo)
    {
        empNo = (empNo ?? "").Trim();
        if (empNo.Length == 0) return NormalizeEmpty(raw);

        var list = Split(raw)
            .Where(x => !string.Equals(x, empNo, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (list.Count == 0) return null;
        return Join(list, DetectSeparator(raw));
    }

    public static char DetectSeparator(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return '/';
        var best = '/';
        var bestCount = 0;
        foreach (var sep in Separators)
        {
            if (char.IsWhiteSpace(sep)) continue;
            var n = 0;
            foreach (var ch in raw)
                if (ch == sep) n++;
            if (n > bestCount)
            {
                bestCount = n;
                best = sep;
            }
        }
        return bestCount > 0 ? best : '/';
    }

    public static string Join(IEnumerable<string> empNos, char separator = '/')
    {
        var list = empNos
            .Select(x => (x ?? "").Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return string.Join(separator.ToString(), list);
    }

    private static string? NormalizeEmpty(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
}
