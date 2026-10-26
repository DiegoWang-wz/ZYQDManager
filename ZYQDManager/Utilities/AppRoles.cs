namespace ZYQDManager.Utilities;

/// <summary>本系统角色常量与判断。</summary>
public static class AppRoles
{
    public const string Admin = "admin";
    public const string Operator = "operator";
    public const string Inspector = "inspector";

    public static bool IsAdmin(string? role, string? empNo = null)
        => string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase)
           || string.Equals(empNo, "JcAdmin", StringComparison.OrdinalIgnoreCase);

    public static bool IsInspector(string? role)
        => string.Equals(role, Inspector, StringComparison.OrdinalIgnoreCase);

    public static bool IsOperator(string? role)
        => string.Equals(role, Operator, StringComparison.OrdinalIgnoreCase)
           || string.IsNullOrWhiteSpace(role);

    public static string Display(string? role, string? empNo = null)
    {
        if (string.Equals(empNo, "JcAdmin", StringComparison.OrdinalIgnoreCase))
            return "超级管理员";
        if (IsAdmin(role, empNo))
            return "管理员";
        if (IsInspector(role))
            return "检测员";
        return "业务员";
    }

    /// <summary>登录后默认首页（门户入口页）。</summary>
    public static string HomePath(string? role) => "/";

    /// <summary>检测员仅开放检测报告；业务员注册时不写 Controls（沿用现有默认菜单）。</summary>
    public static Dictionary<string, DataDtos.ControlPermissionApiDto>? DefaultJoinControls(string? role)
    {
        if (!IsInspector(role))
            return null;
        return new Dictionary<string, DataDtos.ControlPermissionApiDto>(StringComparer.OrdinalIgnoreCase)
        {
            [DataDtos.CentralApiOptions.SirRoute] = new DataDtos.ControlPermissionApiDto
            {
                Visible = true,
                Disabled = false
            }
        };
    }
}
