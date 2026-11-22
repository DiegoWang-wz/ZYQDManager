using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 人员名片：按工号查出来的姓名 / 部门 / 岗位，界面上用来做「鼠标悬停看详情」。
/// </summary>
public sealed class UserDirectoryCard
{
    public string EmpNo { get; init; } = "";
    public string Name { get; init; } = "";

    /// <summary>账号中心里维护的部门原文，可能为空。</summary>
    public string Department { get; init; } = "";

    /// <summary>本系统角色码（operator / inspector / rd_manager / rd_engineer / admin）。</summary>
    public string Role { get; init; } = "";

    public bool IsActive { get; init; } = true;

    /// <summary>岗位：账号中心没有单独的「岗位」字段，取本系统角色中文名。</summary>
    public string Position => AppRoles.Display(Role, EmpNo);
}

/// <summary>
/// 通用人员查询：给一批工号，返回姓名 / 部门 / 岗位。
///
/// 数据源与研发名录一致 —— 跨库读 JcCommonApi 的账号库（CommonApi.dbo.CentralUsers），
/// 不走 HTTP：JcCommonApi 的用户列表接口要 CentralUsersAdmin 权限，业务员账号拿不到，
/// 而且列表不含角色，逐个查详情就是 N+1。
///
/// 列表页一次只查当前页出现的工号（一般十几个），结果按工号缓存 10 分钟，
/// 翻页、来回切换基本不再打库。
/// </summary>
public class UserDirectoryService
{
    /// <summary>一次批量查完，避免按行查库。</summary>
    private const string Sql = """
        SELECT u.EmpNo,
               ISNULL(u.DisplayName, '') AS DisplayName,
               ISNULL(JSON_VALUE(info.value, '$.department'), '') AS Department,
               ISNULL(JSON_VALUE(perm.value, '$.Role'), '') AS Role,
               CASE WHEN ISNULL(u.IsActive, 1) = 0 THEN 0 ELSE 1 END AS IsActive
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
        WHERE u.EmpNo IN (SELECT [value] FROM STRING_SPLIT(@empNos, N','));
        """;

    private static readonly ConcurrentDictionary<string, (UserDirectoryCard Card, DateTime ExpireAt)> Cache
        = new(StringComparer.OrdinalIgnoreCase);

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    /// <summary>缓存上限：账号数量再多也不会把内存吃穿。</summary>
    private const int CacheLimit = 5000;

    private readonly SqlServerDbContext _db;
    private readonly ILogger<UserDirectoryService> _logger;

    public UserDirectoryService(SqlServerDbContext db, ILogger<UserDirectoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>单个工号查名片，查不到返回 null（不会抛异常）。</summary>
    public async Task<UserDirectoryCard?> GetAsync(string? empNo, CancellationToken ct = default)
    {
        var key = Normalize(empNo);
        if (key.Length == 0) return null;

        var map = await GetManyAsync([key], ct);
        return map.TryGetValue(key, out var card) ? card : null;
    }

    /// <summary>
    /// 批量查名片：先吃缓存，剩下的合成一条 SQL 查回来。
    /// 返回的字典以工号为键（忽略大小写），查不到的工号不会出现在结果里。
    /// </summary>
    public async Task<Dictionary<string, UserDirectoryCard>> GetManyAsync(
        IEnumerable<string?> empNos, CancellationToken ct = default)
    {
        var result = new Dictionary<string, UserDirectoryCard>(StringComparer.OrdinalIgnoreCase);

        var wanted = new List<string>();
        foreach (var raw in empNos)
        {
            var empNo = Normalize(raw);
            if (empNo.Length == 0) continue;
            if (wanted.Contains(empNo, StringComparer.OrdinalIgnoreCase)) continue;
            wanted.Add(empNo);
        }

        if (wanted.Count == 0) return result;

        var now = DateTime.Now;
        var missing = new List<string>();

        foreach (var empNo in wanted)
        {
            if (Cache.TryGetValue(empNo, out var hit) && hit.ExpireAt > now)
            {
                result[empNo] = hit.Card;
                continue;
            }

            missing.Add(empNo);
        }

        if (missing.Count == 0) return result;

        foreach (var card in await QueryAsync(missing, ct))
        {
            if (Cache.Count >= CacheLimit) Cache.Clear();
            Cache[card.EmpNo] = (card, now.Add(CacheTtl));
            result[card.EmpNo] = card;
        }

        return result;
    }

    private async Task<List<UserDirectoryCard>> QueryAsync(List<string> empNos, CancellationToken ct)
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
            p.ParameterName = "@empNos";
            p.DbType = DbType.String;
            p.Value = string.Join(",", empNos.Where(x => !x.Contains(',')));
            cmd.Parameters.Add(p);

            var list = new List<UserDirectoryCard>(empNos.Count);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                list.Add(new UserDirectoryCard
                {
                    EmpNo = ReadString(reader, 0),
                    Name = ReadString(reader, 1),
                    Department = ReadString(reader, 2),
                    Role = ReadString(reader, 3),
                    IsActive = reader.IsDBNull(4) || Convert.ToInt32(reader.GetValue(4)) != 0
                });
            }

            return list;
        }
        catch (Exception ex)
        {
            // 查不到人只是缺一个悬停提示，不应该把列表拖垮
            _logger.LogError(ex, "按工号查人员名片失败，count={Count}", empNos.Count);
            return [];
        }
        finally
        {
            if (openedHere) await _db.Database.CloseConnectionAsync();
        }
    }

    private static string Normalize(string? empNo) => (empNo ?? "").Trim();

    private static string ReadString(DbDataReader reader, int index)
        => reader.IsDBNull(index) ? "" : (reader.GetValue(index)?.ToString() ?? "").Trim();
}
