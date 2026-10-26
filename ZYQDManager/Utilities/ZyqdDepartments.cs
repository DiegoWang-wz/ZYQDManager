namespace ZYQDManager.Utilities;

/// <summary>遮阳系统部门选项（注册 / 个人信息 / 人员管理共用）。</summary>
public static class ZyqdDepartments
{
    public static readonly string[] All =
    [
        "遮阳销售部",
        "遮阳研发部",
        "遮阳工程部",
    ];

    /// <summary>下拉选项：固定三项；当前值不在列表时一并带上，避免历史数据被清空。</summary>
    public static IEnumerable<string> Options(string? current)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in All)
        {
            if (seen.Add(name))
                yield return name;
        }

        var extra = (current ?? "").Trim();
        if (extra.Length > 0 && seen.Add(extra))
            yield return extra;
    }
}
