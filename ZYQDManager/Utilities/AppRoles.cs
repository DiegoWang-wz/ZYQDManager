namespace ZYQDManager.Utilities;

/// <summary>本系统角色常量与判断。</summary>
public static class AppRoles
{
    public const string Admin = "admin";
    public const string Operator = "operator";
    public const string Inspector = "inspector";
    /// <summary>研发经理：项目管理 + 检测报告。</summary>
    public const string RdManager = "rd_manager";
    /// <summary>研发工程师：项目管理 + 检测报告。</summary>
    public const string RdEngineer = "rd_engineer";
    /// <summary>
    /// 销售经理：项目管理 + 合同/报价/开票等完整业务菜单。
    /// 目前不参与任何流程审批，只影响菜单可见范围与角色显示。
    /// </summary>
    public const string SalesManager = "sales_manager";

    /// <summary>
    /// 管理员级权限：管理员角色、固定超管工号 JcAdmin，以及销售经理。
    /// 销售经理按「权限等同管理员」处理（人员管理、全部项目、任意单据可改），
    /// 但流程审批不跟着放大 —— 审批仍按研发经理角色 / 蒋经理固定工号走。
    /// </summary>
    public static bool IsAdmin(string? role, string? empNo = null)
        => string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase)
           || string.Equals(empNo, "JcAdmin", StringComparison.OrdinalIgnoreCase)
           || IsSalesManager(role);

    /// <summary>
    /// 超级管理员：只认固定工号 JcAdmin。
    /// 项目流程单据的查看/修改只有他能全局放行；普通 admin 角色和销售经理都不在此列，
    /// 必须按「项目负责人 / 被指派工程师 / 被指派审批人」的口径判定。
    /// </summary>
    public static bool IsStrictAdmin(string? role, string? empNo = null)
        => string.Equals(empNo, "JcAdmin", StringComparison.OrdinalIgnoreCase);

    public static bool IsInspector(string? role)
        => string.Equals(role, Inspector, StringComparison.OrdinalIgnoreCase);

    public static bool IsRdManager(string? role)
        => string.Equals(role, RdManager, StringComparison.OrdinalIgnoreCase);

    public static bool IsRdEngineer(string? role)
        => string.Equals(role, RdEngineer, StringComparison.OrdinalIgnoreCase);

    /// <summary>研发经理或研发工程师。</summary>
    public static bool IsRdRole(string? role)
        => IsRdManager(role) || IsRdEngineer(role);

    public static bool IsSalesManager(string? role)
        => string.Equals(role, SalesManager, StringComparison.OrdinalIgnoreCase);

    /// <summary>菜单/数据范围受限角色：检测员、研发经理、研发工程师。</summary>
    public static bool IsLimitedModuleRole(string? role)
        => IsInspector(role) || IsRdRole(role);

    public static bool IsOperator(string? role)
        => string.Equals(role, Operator, StringComparison.OrdinalIgnoreCase)
           || string.IsNullOrWhiteSpace(role);

    /// <summary>合同/报价/开票/付款方式等完整业务菜单。</summary>
    public static bool CanSeeFullBusinessModules(string? role)
        => !IsLimitedModuleRole(role);

    /// <summary>项目管理：业务员/管理员/研发角色可见，检测员不可见。</summary>
    public static bool CanSeeProject(string? role)
        => !IsInspector(role);

    public static string Display(string? role, string? empNo = null)
    {
        if (string.Equals(empNo, "JcAdmin", StringComparison.OrdinalIgnoreCase))
            return "超级管理员";
        // 销售经理排在管理员之前：它虽然拥有管理员级权限，但角色名要显示成「销售经理」
        if (IsSalesManager(role))
            return "销售经理";
        if (IsAdmin(role, empNo))
            return "管理员";
        if (IsInspector(role))
            return "检测员";
        if (IsRdManager(role))
            return "研发经理";
        if (IsRdEngineer(role))
            return "研发工程师";
        return "业务员";
    }

    /// <summary>登录后默认首页（门户入口页）。</summary>
    public static string HomePath(string? role) => "/";

    /// <summary>
    /// 注册/新建时默认 Controls：
    /// 检测员仅检测报告；研发角色为项目管理+检测报告；
    /// 业务员 / 销售经理不写 Controls（即不受页面级限制，菜单由主导航的角色规则决定）。
    /// </summary>
    public static Dictionary<string, DataDtos.ControlPermissionApiDto>? DefaultJoinControls(string? role)
    {
        if (IsInspector(role))
        {
            return new Dictionary<string, DataDtos.ControlPermissionApiDto>(StringComparer.OrdinalIgnoreCase)
            {
                [DataDtos.CentralApiOptions.SirRoute] = VisibleControl()
            };
        }

        if (IsRdRole(role))
        {
            return new Dictionary<string, DataDtos.ControlPermissionApiDto>(StringComparer.OrdinalIgnoreCase)
            {
                [DataDtos.CentralApiOptions.ProjectRoute] = VisibleControl(),
                [DataDtos.CentralApiOptions.SirRoute] = VisibleControl()
            };
        }

        return null;
    }

    private static DataDtos.ControlPermissionApiDto VisibleControl()
        => new() { Visible = true, Disabled = false };
}
