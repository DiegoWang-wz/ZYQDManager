namespace ZYQDManager.Utilities;

/// <summary>遮阳系统部门选项（注册 / 个人信息 / 人员管理共用）。</summary>
public static class ZyqdDepartments
{
    public const string Sales = "遮阳销售部";
    public const string Rd = "遮阳研发部";
    public const string Engineering = "遮阳工程部";

    public static readonly string[] All =
    [
        Sales,
        Rd,
        Engineering,
    ];

    /// <summary>自助注册 / 新建人员默认角色：研发部检测员，销售部及其它业务员。</summary>
    public static string DefaultRole(string? department)
    {
        var d = (department ?? "").Trim();
        if (string.Equals(d, Rd, StringComparison.Ordinal))
            return AppRoles.Inspector;
        return AppRoles.Operator;
    }

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
