using ZYQDManager.DataDtos;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 角色判定：唯一来源是 JcCommonApi（用户的 ZYQDManager 系统权限里的 Role），
/// 不再维护本地 T_ZYQD_ApprovalRole 成员表。
///
/// 取舍说明：
/// 1. 当前登录人直接读登录时缓存进 localStorage 的 Role，零网络请求；
/// 2. 只有判断「别人」时才回调 JcCommonApi 的 users/{empNo}，这类调用只发生在
///    离职移交、选工程师校验等低频操作上；
/// 3. 承认书终审已改为按人指定（版本表 FinalEmpNo），不再有写死工号的「蒋经理」。
///
/// JcCommonApi 没有「按角色反查成员」的接口（users 列表不含角色，逐个查是 N+1），
/// 所以待办只按角色码流转、界面上显示角色名，不显示具体人名。
/// </summary>
public class AppRoleService
{
    private readonly CentralAuthService _auth;
    private readonly CentralUserManagementService _users;
    private readonly ILogger<AppRoleService> _logger;

    public AppRoleService(
        CentralAuthService auth,
        CentralUserManagementService users,
        ILogger<AppRoleService> logger)
    {
        _auth = auth;
        _users = users;
        _logger = logger;
    }

    /// <summary>
    /// 把流程里用的角色码归一成 JcCommonApi 的角色码。
    /// 老数据（审批流水 ToRole）存的是 RdManager，这里做兼容映射。
    /// </summary>
    public static string ToApiRole(string? roleCode)
    {
        var code = (roleCode ?? "").Trim();
        if (code.Length == 0) return "";

        // 历史角色码 JiangManager（写死工号的那位蒋经理）统一归到销售经理
        if (Same(code, MotorFlow.LegacyJiangRole))
            return AppRoles.SalesManager;

        if (Same(code, AppRoles.RdManager) || Same(code, "RdManager"))
            return AppRoles.RdManager;

        if (Same(code, AppRoles.RdEngineer) || Same(code, "RdEngineer"))
            return AppRoles.RdEngineer;

        return code;
    }

    /// <summary>角色码是否等同于某个 JcCommonApi 角色（忽略大小写与历史别名）。</summary>
    public static bool RoleMatches(string? roleCode, string? apiRole)
        => Same(ToApiRole(roleCode), ToApiRole(apiRole));

    /// <summary>当前登录人是否具备某个审批角色。</summary>
    public async Task<bool> CurrentUserHasRoleAsync(string roleCode, CancellationToken ct = default)
    {
        var auth = await _auth.GetStoredAsync(ct);
        if (auth is null) return false;
        return await HasRoleAsync(roleCode, auth.EmpNo, ct);
    }

    /// <summary>指定工号是否具备某个审批角色。查不到人就当没有权限，不再「没配人放行」。</summary>
    public async Task<bool> HasRoleAsync(string roleCode, string? empNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(empNo)) return false;

        var apiRole = ToApiRole(roleCode);
        if (apiRole.Length == 0) return false;

        var (ok, role, enabled) = await ResolveRoleAsync(empNo, ct);
        return ok && enabled && RoleMatches(apiRole, role);
    }

    /// <summary>
    /// 取某工号在 ZYQDManager 系统下的角色。
    /// 当前登录人优先用本地缓存；角色为空时远程刷新一次再读。
    /// </summary>
    public async Task<(bool Ok, string Role, bool Enabled)> ResolveRoleAsync(
        string? empNo, CancellationToken ct = default)
    {
        var code = (empNo ?? "").Trim();
        if (code.Length == 0) return (false, "", false);

        var auth = await _auth.GetStoredAsync(ct);
        if (auth is not null && Same(auth.EmpNo, code))
        {
            if (!string.IsNullOrWhiteSpace(auth.Role))
                return (true, auth.Role.Trim(), true);

            // 本地没有角色：可能是旧版本登录态，刷新一次
            await _auth.RefreshPermissionsAsync(ct);
            auth = await _auth.GetStoredAsync(ct);
            if (auth is not null && !string.IsNullOrWhiteSpace(auth.Role))
                return (true, auth.Role.Trim(), true);
        }

        return await FetchRemoteRoleAsync(code, ct);
    }

    /// <summary>
    /// 按工号查人（立项单选研发工程师用）：返回姓名、角色、本系统是否启用。
    /// 工号不存在、未开通本系统、已停用都会返回 Ok=false 并给出可读原因。
    /// </summary>
    public async Task<(bool Ok, string Name, string Role, string? Message)> LookupAsync(
        string? empNo, CancellationToken ct = default)
    {
        var code = (empNo ?? "").Trim();
        if (code.Length == 0) return (false, "", "", "请填写工号");

        var auth = await _auth.GetStoredAsync(ct);
        if (auth is not null && Same(auth.EmpNo, code))
        {
            var role = (auth.Role ?? "").Trim();
            if (role.Length == 0)
            {
                await _auth.RefreshPermissionsAsync(ct);
                auth = await _auth.GetStoredAsync(ct);
                role = (auth?.Role ?? "").Trim();
            }
            // 登录态里没有姓名，返回空名让调用方保留原值
            return (true, "", role, null);
        }

        var (ok, detail, msg) = await _users.GetUserAsync(code, ct);
        if (!ok || detail is null)
            return (false, "", "", msg ?? "查不到该工号");

        if (!detail.IsActive)
            return (false, detail.DisplayName ?? code, "", "该账号已停用");

        var entry = FindEntry(detail);
        if (entry is null || string.IsNullOrWhiteSpace(entry.Role))
            return (false, detail.DisplayName ?? code, "", "该工号未开通遮阳系统权限");

        if (!entry.Enabled)
            return (false, detail.DisplayName ?? code, entry.Role.Trim(), "该工号在遮阳系统里已停用");

        return (true, detail.DisplayName ?? code, entry.Role.Trim(), null);
    }

    private async Task<(bool Ok, string Role, bool Enabled)> FetchRemoteRoleAsync(
        string empNo, CancellationToken ct)
    {
        var (ok, detail, msg) = await _users.GetUserAsync(empNo, ct);
        if (!ok || detail is null)
        {
            _logger.LogWarning("读取工号 {EmpNo} 的角色失败：{Message}", empNo, msg);
            return (false, "", false);
        }

        if (!detail.IsActive) return (false, "", false);

        var entry = FindEntry(detail);
        if (entry is null) return (false, "", false);

        return (true, (entry.Role ?? "").Trim(), entry.Enabled);
    }

    /// <summary>SystemPermissions 的键由 JcCommonApi 返回，大小写不保证，这里忽略大小写查。</summary>
    private static SystemPermissionEntryApiDto? FindEntry(UserDetailApiDto detail)
    {
        if (detail.SystemPermissions is null) return null;

        foreach (var kv in detail.SystemPermissions)
        {
            if (kv.Key.Equals(CentralApiOptions.AppSystemKey, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }

        return null;
    }

    private static bool Same(string? a, string? b)
        => string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
}
