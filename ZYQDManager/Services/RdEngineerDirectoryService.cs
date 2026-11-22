using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 研发工程师名录：直接读 JcCommonApi 的账号库（同服务器 CommonApi.dbo.CentralUsers）。
///
/// 为什么不走 HTTP：
/// JcCommonApi 的 GET users 列表需要 CentralUsersAdmin 权限，业务员账号拿不到；
/// 而且列表接口只返回工号/姓名，不含 SystemPermissions，取不到角色，只能逐个查详情（N+1）。
/// 所以这里按项目现有习惯（sa 直连同实例）跨库读一次，在 C# 里按角色 + 部门过滤。
///
/// 判定规则：
/// 1. 账号未停用（IsActive = 1）；
/// 2. SystemPermissions 里 ZYQDManager 块 Role = rd_engineer 且 Enabled 不为 false；
/// 3. 部门取 UserInformation 里 ZYQDManager 块的 department，与所选部门做宽松匹配
///    （「研发一部」与「遮阳研发一部」视为同一个部门）。
/// </summary>
public class RdEngineerDirectoryService
{
    /// <summary>提交立项单时可选的部门：与人员管理里的研发部门共用同一份定义。</summary>
    public static IReadOnlyList<string> FixedDepartments => ZyqdDepartments.Rd;

    /// <summary>
    /// 按角色取人：Role 由参数传入（rd_engineer / rd_manager），
    /// 角色码写在 SQL 参数里，避免为每个角色复制一份语句。
    /// </summary>
    private const string Sql = """
        SELECT u.EmpNo,
               ISNULL(u.DisplayName, '') AS DisplayName,
               ISNULL(JSON_VALUE(info.value, '$.department'), '') AS Department
        FROM CommonApi.dbo.CentralUsers AS u
        OUTER APPLY (
            SELECT TOP (1) j.[value]
            FROM OPENJSON(u.SystemPermissions) AS j
            WHERE j.[key] COLLATE Chinese_PRC_CI_AS = 'ZYQDManager'
        ) AS perm
        OUTER APPLY (
            SELECT TOP (1) j.[value]
            FROM OPENJSON(u.UserInformation) AS j
            WHERE j.[key] COLLATE Chinese_PRC_CI_AS = 'ZYQDManager'
        ) AS info
        WHERE u.IsActive = 1
          AND perm.[value] IS NOT NULL
          AND LOWER(ISNULL(JSON_VALUE(perm.[value], '$.Role'), '')) = LOWER(@role)
          AND LOWER(ISNULL(JSON_VALUE(perm.[value], '$.Enabled'), 'true')) <> 'false'
        ORDER BY Department, u.DisplayName;
        """;

    private readonly SqlServerDbContext _db;
    private readonly ILogger<RdEngineerDirectoryService> _logger;

    public RdEngineerDirectoryService(SqlServerDbContext db, ILogger<RdEngineerDirectoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 部门选项：固定「遮阳研发一部 / 遮阳研发二部」，与人员管理页的部门下拉同源。
    /// </summary>
    public Task<(bool Ok, List<string> Departments, string? Message)> GetDepartmentsAsync(
        CancellationToken ct = default)
        => Task.FromResult<(bool, List<string>, string?)>((true, FixedDepartments.ToList(), null));

    /// <summary>按部门取研发工程师（Role = rd_engineer 且启用）。</summary>
    public Task<(bool Ok, List<RdEngineerOption> List, string? Message)> QueryEngineersAsync(
        string? department, CancellationToken ct = default)
        => QueryByRoleAsync(AppRoles.RdEngineer, department, ct);

    /// <summary>按角色取人，部门为空则不筛部门（研发经理走这个）。</summary>
    public async Task<(bool Ok, List<RdEngineerOption> List, string? Message)> QueryByRoleAsync(
        string roleCode, string? department, CancellationToken ct = default)
    {
        var (ok, list, msg) = await LoadAllAsync(roleCode, ct);
        if (!ok) return (false, [], msg);

        var dept = (department ?? "").Trim();
        if (dept.Length == 0) return (true, list, null);

        return (true, list.Where(x => SameDepartment(x.Department, dept)).ToList(), null);
    }

    /// <summary>
    /// 取研发经理（Role = rd_manager 且启用）。
    /// 不按部门过滤：经理可能挂在任意部门下，甚至没维护部门，按部门筛会把可选人筛空。
    /// </summary>
    public async Task<(bool Ok, List<RdEngineerOption> List, string? Message)> QueryManagersAsync(
        CancellationToken ct = default)
        => await LoadAllAsync(AppRoles.RdManager, ct);

    /// <summary>按工号在研发角色名录里查人（提交审批时校验被指派人）。</summary>
    public async Task<RdEngineerOption?> FindAsync(string? empNo, string roleCode, CancellationToken ct = default)
    {
        var code = (empNo ?? "").Trim();
        if (code.Length == 0) return null;

        var (ok, list, _) = await LoadAllAsync(roleCode, ct);
        if (!ok) return null;

        return list.FirstOrDefault(x => string.Equals(x.EmpNo, code, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<(bool Ok, List<RdEngineerOption> List, string? Message)> LoadAllAsync(
        string roleCode, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        var openedHere = false;

        try
        {
            if (conn.State != ConnectionState.Open)
            {
                await _db.Database.OpenConnectionAsync(ct);
                openedHere = true;
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = Sql;
            cmd.CommandTimeout = 15;

            var p = cmd.CreateParameter();
            p.ParameterName = "@role";
            p.DbType = DbType.String;
            p.Value = roleCode;
            cmd.Parameters.Add(p);

            var list = new List<RdEngineerOption>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new RdEngineerOption
                {
                    EmpNo = ReadString(reader, 0),
                    Name = ReadString(reader, 1),
                    Department = ReadString(reader, 2)
                });
            }

            return (true, list, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取研发人员名录失败（CommonApi.dbo.CentralUsers, role={Role}）", roleCode);
            return (false, [], "读取账号中心失败：" + ex.Message);
        }
        finally
        {
            if (openedHere) await _db.Database.CloseConnectionAsync();
        }
    }

    private static string ReadString(DbDataReader reader, int index)
        => reader.IsDBNull(index) ? "" : (reader.GetValue(index)?.ToString() ?? "").Trim();

    /// <summary>部门宽松匹配：「研发一部」与「遮阳研发一部」视为同一个部门。</summary>
    public static bool SameDepartment(string? a, string? b)
    {
        var x = Normalize(a);
        var y = Normalize(b);
        if (x.Length == 0 || y.Length == 0) return false;
        if (x == y) return true;
        return x.Contains(y, StringComparison.OrdinalIgnoreCase)
               || y.Contains(x, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? dept)
    {
        var s = (dept ?? "").Trim().Replace(" ", "").Replace("　", "");
        if (s.StartsWith("遮阳", StringComparison.Ordinal)) s = s[2..];
        return s;
    }
}
