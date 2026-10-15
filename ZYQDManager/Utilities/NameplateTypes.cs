namespace ZYQDManager.Utilities;

/// <summary>合同基础铭牌类型（可空；非空时仅允许下列值）。</summary>
public static class NameplateTypes
{
    public const string Laser = "镭雕铭牌";
    public const string Sticker = "打印贴纸";
    public const string Outsourced = "外购铭牌";

    public static readonly string[] All = [Laser, Sticker, Outsourced];

    public static bool IsAllowedOrEmpty(string? value)
    {
        var s = (value ?? "").Trim();
        return s.Length == 0 || All.Contains(s);
    }

    public static string? Normalize(string? value)
    {
        var s = (value ?? "").Trim();
        return s.Length == 0 ? null : s;
    }
}
