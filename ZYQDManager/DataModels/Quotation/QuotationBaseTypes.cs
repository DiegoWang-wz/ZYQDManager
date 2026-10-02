namespace ZYQDManager.DataModels.Quotation;

/// <summary>报价基础品类（由表区分，不存 Type 列）。</summary>
public static class QuotationBaseTypes
{
    public const string Motor = "Motor";
    public const string Remote = "Remote";
    public const string Accessory = "Accessory";
    public const string CB = "CB";
    public const string PR = "PR";

    public static readonly (string Key, string Label)[] All =
    [
        (Motor, "电机"),
        (Remote, "遥控器"),
        (CB, "控制盒"),
        (PR, "推杆"),
        (Accessory, "配件"),
    ];

    public static string Normalize(string? key)
    {
        var k = (key ?? "").Trim();
        if (All.Any(x => x.Key.Equals(k, StringComparison.OrdinalIgnoreCase)))
            return All.First(x => x.Key.Equals(k, StringComparison.OrdinalIgnoreCase)).Key;

        // 兼容中文 / 菜单入口
        return k switch
        {
            "电机" or "电机类" => Motor,
            "遥控器" => Remote,
            "配件" or "其他配件" => Accessory,
            "控制盒" => CB,
            "推杆" or "推杆类" => PR,
            _ => Motor,
        };
    }

    public static string LabelOf(string? key)
        => All.FirstOrDefault(x => x.Key == Normalize(key)).Label ?? "电机";
}
