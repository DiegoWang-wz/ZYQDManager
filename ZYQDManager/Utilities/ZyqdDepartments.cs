namespace ZYQDManager.Utilities;

/// <summary>
/// 遮阳系统部门选项（自助注册 / 个人信息 / 人员管理 / 立项单选研发工程师共用）。
/// 人员档案里存的是这里的原文，改名字要连带改库里的 department 值。
/// </summary>
public static class ZyqdDepartments
{
    public const string Sales = "遮阳销售部";
    public const string RdOne = "遮阳研发一部";
    public const string RdTwo = "遮阳研发二部";
    public const string Engineering = "遮阳工程部";

    /// <summary>研发条线的部门：提交立项单时只能从这两个部门里选研发工程师。</summary>
    public static readonly string[] Rd =
    [
        RdOne,
        RdTwo,
    ];

    public static readonly string[] All =
    [
        Sales,
        RdOne,
        RdTwo,
        Engineering,
    ];

    /// <summary>是否研发条线部门。</summary>
    public static bool IsRd(string? department)
        => Rd.Any(d => string.Equals(d, (department ?? "").Trim(), StringComparison.Ordinal));

    /// <summary>自助注册 / 新建人员默认角色：研发部门检测员，销售部及其它业务员。</summary>
    public static string DefaultRole(string? department)
        => IsRd(department) ? AppRoles.Inspector : AppRoles.Operator;

    /// <summary>
    /// 下拉选项：固定项在前；当前值不在列表里时一并带上，
    /// 避免历史数据（比如已撤销的「遮阳研发部」）一打开编辑就被清空。
    /// </summary>
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
