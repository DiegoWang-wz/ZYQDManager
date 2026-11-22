using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 电机流程的服务端权限护栏：只认数据库里当前登记的负责人，不信页面上按钮的显隐。
/// 规则：立项单/承认书归业务员，规格书归研发工程师，审批归版本上被指派的人。
/// 管理员（admin / JcAdmin）可兜底维护单据；销售经理不算这里的「管理员」，只有被指派终审时才可审批。
/// </summary>
public class MotorPermissionService
{
    private readonly SqlServerDbContext _db;
    private readonly AppRoleService _roles;

    public MotorPermissionService(SqlServerDbContext db, AppRoleService roles)
    {
        _db = db;
        _roles = roles;
    }

    /// <summary>检查当前账号能否编辑某类单据：lx/cr 看项目负责人，gg 看项目研发工程师。</summary>
    public async Task<string?> CheckEditorAsync(
        string docKind, string? projectGuid, string? empNo, CancellationToken ct = default)
    {
        if (await IsStrictAdminAsync(empNo, ct)) return null;

        var isGg = string.Equals(docKind, MotorFlow.DocGg, StringComparison.OrdinalIgnoreCase);
        var role = isGg ? AppRoles.RdEngineer : AppRoles.Operator;

        // 还没建项目的新单据：只能由对应角色创建，管理员除外
        if (string.IsNullOrWhiteSpace(projectGuid))
        {
            return await _roles.HasRoleAsync(role, empNo, ct)
                ? null
                : (isGg ? "只有研发工程师可以编写规格书" : "只有项目业务员可以编制立项单/承认书");
        }

        var project = await _db.Projects.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid.Trim())
            .Select(x => new { x.OwnerEmpNo, x.CurrentVersionNo })
            .FirstOrDefaultAsync(ct);

        if (project is null) return "未找到项目，无法校验修改权限";

        string? responsible;
        if (isGg)
        {
            // 规格书归当前版本的研发工程师：工程师只存在立项单上（项目表不再冗余）
            var curVer = (project.CurrentVersionNo ?? "").Trim();
            responsible = await _db.MotorLx.AsNoTracking()
                .Where(x => x.ProjectGuid == projectGuid.Trim()
                            && x.DeleteFlag != "X"
                            && (curVer == "" || x.VersionNo == curVer))
                .OrderByDescending(x => x.Id)
                .Select(x => x.EngineerCode)
                .FirstOrDefaultAsync(ct);
        }
        else
        {
            responsible = project.OwnerEmpNo;
        }

        if (string.IsNullOrWhiteSpace(responsible))
            return isGg ? "该项目还没有指定研发工程师" : "该项目还没有指定负责人";

        if (Same(responsible, empNo)) return null;

        return isGg
            ? "规格书只有被指派的研发工程师可以修改"
            : "只有项目负责人本人可以修改立项单/承认书";
    }

    /// <summary>检查审批人是不是版本上被指派的那个人（final=true 查终审人，否则查研发经理）。</summary>
    public async Task<string?> CheckAssignedApproverAsync(
        string? projectGuid, string? versionNo, string? empNo, bool final, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(versionNo))
            return "缺少版本信息，无法校验审批权限";

        var version = await _db.ProjectVersions.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid.Trim() && x.VersionNo == versionNo.Trim())
            .Select(x => new { x.ManagerEmpNo, x.FinalEmpNo })
            .FirstOrDefaultAsync(ct);

        if (version is null) return "未找到版本信息，无法校验审批权限";

        var assigned = final ? version.FinalEmpNo : version.ManagerEmpNo;
        var label = final ? "终审人" : "研发经理";

        if (string.IsNullOrWhiteSpace(assigned))
            return $"该版本还没有指定{label}，不能审批";

        if (!Same(assigned, empNo))
            return $"本单已指派给 {assigned} 审批，你只能查看";

        return null;
    }

    /// <summary>超级管理员只有固定工号 JcAdmin；普通 admin 角色不放大项目单据权限。</summary>
    private Task<bool> IsStrictAdminAsync(string? empNo, CancellationToken ct)
        => Task.FromResult(string.Equals(empNo?.Trim(), "JcAdmin", StringComparison.OrdinalIgnoreCase));

    private static bool Same(string? a, string? b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}
