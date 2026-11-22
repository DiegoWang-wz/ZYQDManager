namespace ZYQDManager.Utilities;

/// <summary>
/// 产品类型：项目表 T_ZYQD_Project.ProductType 的取值。
/// 目前只有电机跑通了全流程，其余三类先预留，共用同一张项目表。
/// </summary>
public static class ProjectTypes
{
    public const string Motor = "Motor";
    public const string Remote = "Remote";
    public const string ControlBox = "ControlBox";
    public const string Actuator = "Actuator";

    public static readonly IReadOnlyList<(string Code, string Name)> All =
    [
        (Motor, "电机"),
        (Remote, "遥控器"),
        (ControlBox, "控制盒"),
        (Actuator, "推杆")
    ];

    public static string Name(string? code)
    {
        foreach (var (c, n) in All)
        {
            if (string.Equals(c, code, StringComparison.OrdinalIgnoreCase)) return n;
        }
        return code ?? "";
    }

    /// <summary>中文名反查代码，找不到时回落到电机。</summary>
    public static string FromName(string? name)
    {
        foreach (var (c, n) in All)
        {
            if (string.Equals(n, name, StringComparison.Ordinal)) return c;
        }
        return Motor;
    }
}

/// <summary>项目状态：T_ZYQD_Project.Status</summary>
public static class ProjectStatus
{
    /// <summary>正常进行（含草稿；暂不区分草稿，列表不过滤）。</summary>
    public const string Active = "Active";

    /// <summary>作废：立项单被删除且项目下已无有效版本。</summary>
    public const string Void = "Void";
}
