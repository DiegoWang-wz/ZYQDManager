using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

public class MotorLxService
{
    /// <summary>首个版本号。新建项目时项目表与立项单都写这个值。</summary>
    public const string FirstVersionNo = "V1";

    private readonly SqlServerDbContext _db;
    private readonly MotorPermissionService _perm;

    public MotorLxService(SqlServerDbContext db, MotorPermissionService perm)
    {
        _db = db;
        _perm = perm;
    }

    public Task<MotorLxModel?> GetByDocGuidAsync(string docGuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(docGuid))
            return Task.FromResult<MotorLxModel?>(null);

        return _db.MotorLx.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocGuid == docGuid && x.DeleteFlag != "X", ct);
    }

    /// <summary>项目表上的当前版本号；项目不存在时返回 null。</summary>
    public Task<string?> GetCurrentVersionNoAsync(string projectGuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectGuid))
            return Task.FromResult<string?>(null);

        return _db.Projects.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.DeleteFlag != "X")
            .Select(x => x.CurrentVersionNo)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>指定版本立项单的 DocGuid；该版本没有立项单时返回 null。</summary>
    public Task<string?> GetLxDocGuidAsync(string projectGuid, string versionNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(versionNo))
            return Task.FromResult<string?>(null);

        return _db.MotorLx.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid
                        && x.VersionNo == versionNo
                        && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .Select(x => x.DocGuid)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// 项目下实际存在的版本号（按立项单推，最新在前）。
    /// 之所以不用 T_ZYQD_ProjectVersion：V1 是保存立项单时自动建的，从来不写版本记录，
    /// 只查版本表会导致「只有一个项目版本的项目」下拉框空着。
    /// </summary>
    public async Task<List<string>> GetVersionNosAsync(string projectGuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectGuid)) return [];

        var rows = await _db.MotorLx.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.DeleteFlag != "X")
            .Select(x => new { x.VersionNo, x.Id })
            .ToListAsync(ct);

        return rows.OrderByDescending(x => x.Id)
            .Select(x => (x.VersionNo ?? "").Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// 单据页「版本」下拉用的一整份数据：版本号、版本行、项目当前版本号。
    /// 版本号以立项单为准，版本表只补归档等状态；两边并集，避免老数据缺行导致下拉漏项。
    /// 三张单据页共用同一份口径，下拉框长得一样、切过去的行为也一样。
    /// </summary>
    public async Task<(List<string> Nos, List<ProjectVersionModel> Versions, string? CurrentVersionNo)>
        GetVersionOptionsAsync(string? projectGuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectGuid)) return ([], [], null);

        var current = await GetCurrentVersionNoAsync(projectGuid!, ct);
        var nos = await GetVersionNosAsync(projectGuid!, ct);

        var versions = await _db.ProjectVersions.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid)
            .OrderByDescending(x => x.Id)
            .ToListAsync(ct);

        foreach (var v in versions)
        {
            var no = (v.VersionNo ?? "").Trim();
            if (no.Length == 0) continue;
            if (!nos.Any(x => MotorFlow.Same(x, no))) nos.Add(no);
        }

        return (nos, versions, current);
    }

    /// <summary>
    /// 新建：首次保存才生成 ProjectGuid/DocGuid，同时生成项目主表记录（同一个事务）。
    /// 更新：按 DocGuid 更新草稿内容，并同步项目表上的身份字段。
    /// </summary>
    public async Task<(string ProjectGuid, string DocGuid, bool Created)> SaveDraftAsync(
        MotorLxModel input,
        string? empNo,
        string? empName,
        CancellationToken ct = default)
    {
        var deny = await _perm.CheckEditorAsync(MotorFlow.DocLx, input.ProjectGuid, empNo, ct);
        if (deny is not null) throw new InvalidOperationException(deny);

        var now = DateTime.Now;
        var isNew = string.IsNullOrWhiteSpace(input.DocGuid) || string.IsNullOrWhiteSpace(input.ProjectGuid);

        if (isNew)
        {
            var projectGuid = Guid.NewGuid().ToString("N");
            var row = new MotorLxModel
            {
                ProjectGuid = projectGuid,
                DocGuid = Guid.NewGuid().ToString("N"),
                VersionNo = FirstVersionNo,
                IsDraft = true,
                Status = MotorFlow.LxDraft,
                CreateTime = now,
                UpdateTime = now,
                CreatePersonCode = empNo,
                UpdatePersonCode = empNo,
                DeleteFlag = ""
            };
            CopyBusinessFields(input, row);
            _db.MotorLx.Add(row);

            // 项目主表与立项单同批写入，避免出现「有单据没有项目」的孤儿数据
            _db.Projects.Add(new ProjectModel
            {
                ProjectGuid = projectGuid,
                ProductType = ProjectTypes.Motor,
                Customer = row.Customer,
                CustomerPartCode = row.CustomerPartCode,
                MotorModelSpec = row.MotorModelSpec,
                Description = row.Description,
                DemandPerson = row.DemandPerson,
                OwnerEmpNo = empNo,
                CurrentVersionNo = FirstVersionNo,
                Status = ProjectStatus.Active,
                CreateTime = now,
                CreatePersonCode = empNo,
                UpdateTime = now,
                UpdatePersonCode = empNo,
                DeleteFlag = ""
            });

            // 版本行同批建立：节点是流程的唯一权威，没有这一行节点 1 就没地方落
            var newVersion = new ProjectVersionModel
            {
                ProjectGuid = projectGuid,
                VersionNo = FirstVersionNo,
                Status = MotorFlow.VersionInProgress,
                CurrentNode = MotorNode.LxCreate,
                PendingEmpNo = empNo,
                NodeEnterTime = now,
                NodeUpdateEmpNo = empNo,
                CreateTime = now,
                CreatePersonCode = empNo
            };
            // 「创建立项单」在提交前每次保存都刷新时间，操作人就是保存人
            newVersion.TouchNodeTime(MotorNode.LxCreate, now, empNo);
            _db.ProjectVersions.Add(newVersion);

            await _db.SaveChangesAsync(ct);
            return (row.ProjectGuid, row.DocGuid, true);
        }

        var existing = await _db.MotorLx
            .FirstOrDefaultAsync(x => x.DocGuid == input.DocGuid && x.DeleteFlag != "X", ct)
            ?? throw new InvalidOperationException("未找到立项单，可能已删除");

        CopyBusinessFields(input, existing);
        existing.UpdateTime = now;
        existing.UpdatePersonCode = empNo;
        if (string.IsNullOrWhiteSpace(existing.Status))
            existing.Status = MotorFlow.LxDraft;

        // 客户 / 型号这些项目级字段改在这里，项目表跟着一起变
        var project = await _db.Projects
            .FirstOrDefaultAsync(x => x.ProjectGuid == existing.ProjectGuid && x.DeleteFlag != "X", ct);
        if (project is null)
        {
            // 兼容项目表上线前的老单据
            _db.Projects.Add(new ProjectModel
            {
                ProjectGuid = existing.ProjectGuid,
                ProductType = ProjectTypes.Motor,
                Customer = existing.Customer,
                CustomerPartCode = existing.CustomerPartCode,
                MotorModelSpec = existing.MotorModelSpec,
                Description = existing.Description,
                DemandPerson = existing.DemandPerson,
                OwnerEmpNo = existing.CreatePersonCode ?? empNo,
                CurrentVersionNo = existing.VersionNo,
                Status = ProjectStatus.Active,
                CreateTime = existing.CreateTime ?? now,
                CreatePersonCode = existing.CreatePersonCode,
                UpdateTime = now,
                UpdatePersonCode = empNo,
                DeleteFlag = ""
            });
        }
        else
        {
            project.Customer = existing.Customer;
            project.CustomerPartCode = existing.CustomerPartCode;
            project.MotorModelSpec = existing.MotorModelSpec;
            project.Description = existing.Description;
            project.DemandPerson = existing.DemandPerson;
            project.UpdateTime = now;
            project.UpdatePersonCode = empNo;
        }

        var lxVersionNo = string.IsNullOrWhiteSpace(existing.VersionNo) ? FirstVersionNo : existing.VersionNo!;
        await EnsureVersionRowAsync(
            existing.ProjectGuid, lxVersionNo,
            project?.OwnerEmpNo ?? existing.CreatePersonCode ?? empNo,
            empNo, empName, now, ct);
        // 每次保存刷新「创建立项单」时间
        var lxVersion = await _db.ProjectVersions
            .FirstOrDefaultAsync(x => x.ProjectGuid == existing.ProjectGuid && x.VersionNo == lxVersionNo, ct);
        lxVersion?.TouchNodeTime(MotorNode.LxCreate, now, empNo);

        await _db.SaveChangesAsync(ct);
        return (existing.ProjectGuid, existing.DocGuid, false);
    }

    /// <summary>
    /// 修改立项单：把页面上的编辑结果写进「新建版本」克隆出来的那张立项单，
    /// 同时把修改说明（ChangeReason）与逐字段修改点（ChangeDiff）记到版本表上。
    ///
    /// 只改业务字段，不碰状态 / 提交时间 / 工程师指派 —— 那些由流程服务决定；
    /// 新版本此刻是草稿，业务员确认后照常点提交。
    /// </summary>
    public async Task<(bool Ok, string Message, string? DocGuid)> SaveRevisionAsync(
        string projectGuid,
        string versionNo,
        MotorLxModel edited,
        string changeReason,
        string changeDiffJson,
        string? empNo,
        string? empName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(versionNo))
            return (false, "缺少项目或版本号", null);

        var deny = await _perm.CheckEditorAsync(MotorFlow.DocLx, projectGuid, empNo, ct);
        if (deny is not null) return (false, deny, null);

        var now = DateTime.Now;

        var row = await _db.MotorLx
            .FirstOrDefaultAsync(x => x.ProjectGuid == projectGuid
                                      && x.VersionNo == versionNo
                                      && x.DeleteFlag != "X", ct);
        if (row is null) return (false, $"未找到版本 {versionNo} 的立项单", null);

        CopyBusinessFields(edited, row);
        row.UpdateTime = now;
        row.UpdatePersonCode = empNo;

        // 客户 / 型号这些项目级字段跟着最新版走，列表上看到的才是改后的值
        var project = await _db.Projects
            .FirstOrDefaultAsync(x => x.ProjectGuid == projectGuid && x.DeleteFlag != "X", ct);
        if (project is not null)
        {
            project.Customer = row.Customer;
            project.CustomerPartCode = row.CustomerPartCode;
            project.MotorModelSpec = row.MotorModelSpec;
            project.Description = row.Description;
            project.DemandPerson = row.DemandPerson;
            project.UpdateTime = now;
            project.UpdatePersonCode = empNo;
        }

        var version = await _db.ProjectVersions
            .FirstOrDefaultAsync(x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo, ct);
        if (version is not null)
        {
            version.ChangeReason = changeReason;
            version.ChangeDiff = changeDiffJson;
            version.NodeUpdateEmpNo = empNo;
            // 修改立项单（新版本）每次保存同样刷新「创建立项单」时间
            version.TouchNodeTime(MotorNode.LxCreate, now, empNo);
        }

        await _db.SaveChangesAsync(ct);
        return (true, "立项单修改已保存", row.DocGuid);
    }

    /// <summary>
    /// 确保版本行存在（老数据 / 只建了立项单没建版本行的情况）。
    /// 节点落到「立项单编制中」，待办人是项目负责人。
    /// </summary>
    private async Task EnsureVersionRowAsync(
        string projectGuid, string versionNo, string? ownerEmpNo,
        string? empNo, string? empName, DateTime now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(versionNo)) return;

        var exists = await _db.ProjectVersions
            .AnyAsync(x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo, ct);
        if (exists) return;

        _db.ProjectVersions.Add(new ProjectVersionModel
        {
            ProjectGuid = projectGuid,
            VersionNo = versionNo,
            Status = MotorFlow.VersionInProgress,
            CurrentNode = MotorNode.LxCreate,
            PendingEmpNo = string.IsNullOrWhiteSpace(ownerEmpNo) ? null : ownerEmpNo.Trim(),
            NodeEnterTime = now,
            NodeUpdateEmpNo = empNo,
            CreateTime = now,
            CreatePersonCode = empNo
        });
    }

    /// <summary>
    /// 项目列表分页查询：按「项目」而不是按「立项单」分页。
    ///
    /// 一行 = 一个项目 + 它的当前版本，所以同一项目有 V1/V2 时也只出一行。
    /// 过滤与排序走项目表（客户/型号/负责人等都在这里），
    /// 当前版本单据的 DocGuid / 状态再用一次批量查询补齐，避免 N+1。
    /// </summary>
    public async Task<(List<MotorLxListDto> Rows, int Total)> QueryPageAsync(
        MotorLxQueryDto query,
        int skip,
        int take,
        string? sortLabel,
        bool sortDescending,
        CancellationToken ct = default,
        string? productType = null,
        string? myEmpNo = null,
        string? myRole = null)
    {
        var type = string.IsNullOrWhiteSpace(productType) ? ProjectTypes.Motor : productType;

        var q = _db.Projects.AsNoTracking()
            .Where(x => x.DeleteFlag != "X"
                        && x.Status != ProjectStatus.Void
                        && x.ProductType == type);

        var keyword = (query.Keyword ?? "").Trim();
        if (keyword.Length > 0)
            q = ApplyProjectKeyword(q, query.FilterColumn, keyword);

        var total = await q.CountAsync(ct);

        var page = await ApplyProjectSort(q, sortLabel, sortDescending)
            .Skip(Math.Max(0, skip))
            .Take(take <= 0 ? 20 : take)
            .ToListAsync(ct);

        var rows = await BuildListRowsAsync(page, ct);
        await FillStageAsync(rows, myEmpNo, myRole, ct);

        return (rows, total);
    }

    /// <summary>
    /// 统计「我的待办」项目数（跨分页）。
    ///
    /// 这里以前是「先把项目表读回来，再把这些项目的**所有版本行**读回来，在内存里分组挑当前版本」，
    /// 版本多了以后一次要拉几千行，列表页要等好几秒才出徽标。
    /// 现在改成一条 SQL：每个项目用相关子查询只带出「当前版本」的四个字段，
    /// 判断仍然走 <see cref="MotorNode.IsMine"/>，口径不变但只拉 N 行。
    /// </summary>
    public async Task<int> CountMyTodoAsync(
        string? myEmpNo,
        string? myRole,
        string? productType = null,
        CancellationToken ct = default)
    {
        var me = (myEmpNo ?? "").Trim();
        if (me.Length == 0) return 0;

        var type = string.IsNullOrWhiteSpace(productType) ? ProjectTypes.Motor : productType;

        // 当前版本优先取项目表上的 CurrentVersionNo，取不到就按版本号倒序取最新，
        // 等价于原来的 SameVersion(...) ?? OrderByDescending(VersionNo).First()。
        var rows = await _db.Projects.AsNoTracking()
            .Where(x => x.DeleteFlag != "X"
                        && x.Status != ProjectStatus.Void
                        && x.ProductType == type)
            .Select(x => new
            {
                x.OwnerEmpNo,
                // 研发工程师不再冗余在项目表：取当前版本立项单上登记的那位
                EngineerCode = _db.MotorLx
                    .Where(l => l.ProjectGuid == x.ProjectGuid && l.DeleteFlag != "X")
                    .OrderBy(l => l.VersionNo == x.CurrentVersionNo ? 0 : 1)
                    .ThenByDescending(l => l.VersionNo)
                    .Select(l => l.EngineerCode)
                    .FirstOrDefault(),
                Ver = _db.ProjectVersions
                    .Where(v => v.ProjectGuid == x.ProjectGuid)
                    .OrderBy(v => v.VersionNo == x.CurrentVersionNo ? 0 : 1)
                    .ThenByDescending(v => v.VersionNo)
                    .Select(v => new { v.Status, v.CurrentNode, v.PendingEmpNo, v.PendingRole, v.FinalEmpNo })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var todo = 0;
        foreach (var p in rows)
        {
            var ver = p.Ver;

            var node = MotorNode.Normalize(ver?.CurrentNode ?? MotorNode.None);
            if (ver is not null
                && string.Equals(ver.Status, MotorFlow.VersionArchived, StringComparison.OrdinalIgnoreCase))
                node = MotorNode.Archived;

            var pendingEmpNo = ver?.PendingEmpNo;
            var pendingRole = ver?.PendingRole;
            if (string.IsNullOrWhiteSpace(pendingEmpNo) && string.IsNullOrWhiteSpace(pendingRole))
            {
                var (emp, role) = MotorNode.PendingOf(
                    node, p.OwnerEmpNo, p.EngineerCode, p.Ver?.FinalEmpNo);
                pendingEmpNo = emp;
                pendingRole = role;
            }

            if (MotorNode.IsMine(node, pendingEmpNo, pendingRole, me, myRole)) todo++;
        }

        return todo;
    }

    /// <summary>
    /// 把项目行投影成列表行，并补上当前版本的立项单信息
    /// （DocGuid 用于「编辑」跳转，Status 用于阶段与状态列）。
    /// </summary>
    private async Task<List<MotorLxListDto>> BuildListRowsAsync(
        List<ProjectModel> page, CancellationToken ct)
    {
        if (page.Count == 0) return [];

        var guids = page.Select(x => x.ProjectGuid).Distinct().ToList();

        // 只取定位当前版本需要的列，不把 120 多个业务字段拉回来
        var lxRows = await _db.MotorLx.AsNoTracking()
            .Where(x => x.DeleteFlag != "X" && guids.Contains(x.ProjectGuid))
            .Select(x => new MotorLxListDto
            {
                Id = x.Id,
                ProjectGuid = x.ProjectGuid,
                DocGuid = x.DocGuid,
                VersionNo = x.VersionNo,
                Status = x.Status,
                IsDraft = x.IsDraft,
                EngineerEmpNo = x.EngineerCode
            })
            .ToListAsync(ct);

        var byProject = lxRows
            .GroupBy(x => x.ProjectGuid)
            .ToDictionary(g => g.Key, g => g.ToList());

        var list = new List<MotorLxListDto>(page.Count);
        foreach (var p in page)
        {
            byProject.TryGetValue(p.ProjectGuid, out var docs);

            // 优先匹配项目表上的 CurrentVersionNo，匹配不到时回落到最新一条
            var lx = docs is null || docs.Count == 0
                ? null
                : docs.FirstOrDefault(x => SameVersion(x.VersionNo, p.CurrentVersionNo))
                  ?? docs.OrderByDescending(x => x.Id).First();

            list.Add(new MotorLxListDto
            {
                ProjectGuid = p.ProjectGuid,
                DocGuid = lx?.DocGuid ?? "",
                VersionNo = lx?.VersionNo ?? p.CurrentVersionNo ?? "",
                Customer = p.Customer,
                DemandPerson = p.DemandPerson,
                CustomerPartCode = p.CustomerPartCode,
                MotorModelSpec = p.MotorModelSpec,
                Description = p.Description,
                OwnerEmpNo = p.OwnerEmpNo,
                EngineerEmpNo = lx?.EngineerEmpNo,
                Status = lx?.Status,
                IsDraft = lx?.IsDraft ?? true,
                UpdatePersonCode = p.UpdatePersonCode,
                UpdateTime = p.UpdateTime,
                CreatePersonCode = p.CreatePersonCode,
                CreateTime = p.CreateTime
            });
        }

        // 展示用姓名统一从员工表批量取（业务表只存工号，员工改名不用回刷业务数据）
        var nameKeys = list
            .SelectMany(x => new[] { x.OwnerEmpNo, x.EngineerEmpNo, x.UpdatePersonCode, x.CreatePersonCode })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (nameKeys.Count > 0)
        {
            var empRows = await _db.Employees.AsNoTracking()
                .Where(x => nameKeys.Contains(x.EmpNo))
                .Select(x => new { x.EmpNo, x.EmpName })
                .ToListAsync(ct);

            var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in empRows)
            {
                if (!string.IsNullOrWhiteSpace(e.EmpName))
                    nameMap[e.EmpNo] = e.EmpName!.Trim();
            }

            foreach (var r in list)
            {
                r.OwnerName = NameOf(nameMap, r.OwnerEmpNo);
                r.EngineerName = NameOf(nameMap, r.EngineerEmpNo);
                r.UpdatePersonName = NameOf(nameMap, r.UpdatePersonCode);
                r.CreatePersonName = NameOf(nameMap, r.CreatePersonCode);
            }
        }

        return list;
    }

    private static bool SameVersion(string? a, string? b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string? NameOf(Dictionary<string, string> names, string? empNo)
        => !string.IsNullOrWhiteSpace(empNo) && names.TryGetValue(empNo.Trim(), out var n) ? n : null;

    /// <summary>
    /// 批量补齐当前页各项目的节点、阶段与待办。
    ///
    /// 流程只认版本表上的 CurrentNode —— 这是「现在走到第几步、在等谁」的唯一权威；
    /// 三张单据在这只用来补 DocGuid（入口跳转）、步骤完成标记和悬停时间，不再参与阶段推导。
    /// 节点没回填（0）时按节点 1 处理，待办人按节点规则从项目表推。
    /// </summary>
    private async Task FillStageAsync(
        List<MotorLxListDto> rows, string? myEmpNo, string? myRole, CancellationToken ct)
    {
        if (rows.Count == 0) return;

        var guids = rows.Select(x => (x.ProjectGuid ?? "").Trim())
                        .Where(x => x.Length > 0)
                        .Distinct()
                        .ToList();
        if (guids.Count == 0) return;

        // 规格书 / 承认书按「项目 + 版本」取最新一条，旧数据没有版本号时会落到同一个 key 上
        var ggRows = await _db.MotorGg.AsNoTracking()
            .Where(x => x.DeleteFlag != "X" && guids.Contains(x.ProjectGuid))
            .Select(x => new { x.ProjectGuid, x.VersionNo, x.Status, x.Id, x.UpdateTime, x.DocGuid })
            .ToListAsync(ct);

        var crRows = await _db.MotorCr.AsNoTracking()
            .Where(x => x.DeleteFlag != "X" && guids.Contains(x.ProjectGuid))
            .Select(x => new { x.ProjectGuid, x.VersionNo, x.Status, x.Id, x.UpdateTime, x.DocGuid })
            .ToListAsync(ct);

        var verRows = await _db.ProjectVersions.AsNoTracking()
            .Where(x => guids.Contains(x.ProjectGuid))
            .Select(x => new
            {
                x.ProjectGuid,
                x.VersionNo,
                x.Status,
                x.CurrentNode,
                x.PendingEmpNo,
                x.PendingRole,
                x.ManagerEmpNo,
                x.FinalEmpNo
            })
            .ToListAsync(ct);

        var ggMap = ggRows
            .GroupBy(x => VerKey(x.ProjectGuid, x.VersionNo))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

        var crMap = crRows
            .GroupBy(x => VerKey(x.ProjectGuid, x.VersionNo))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

        var verMap = verRows
            .GroupBy(x => VerKey(x.ProjectGuid, x.VersionNo))
            .ToDictionary(g => g.Key, g => g.First());

        // 审批节点的显示名（研发经理 / 终审人）也从员工表取
        var approverKeys = verRows
            .SelectMany(x => new[] { x.ManagerEmpNo, x.FinalEmpNo })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var approverNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (approverKeys.Count > 0)
        {
            var apRows = await _db.Employees.AsNoTracking()
                .Where(x => approverKeys.Contains(x.EmpNo))
                .Select(x => new { x.EmpNo, x.EmpName })
                .ToListAsync(ct);

            foreach (var a in apRows)
            {
                if (!string.IsNullOrWhiteSpace(a.EmpName))
                    approverNames[a.EmpNo] = a.EmpName!.Trim();
            }
        }

        foreach (var row in rows)
        {
            var key = VerKey(row.ProjectGuid, row.VersionNo);
            ggMap.TryGetValue(key, out var gg);
            crMap.TryGetValue(key, out var cr);
            verMap.TryGetValue(key, out var ver);

            var node = MotorNode.Normalize(ver?.CurrentNode ?? MotorNode.None);
            var archived = node == MotorNode.Archived
                           || string.Equals(ver?.Status, MotorFlow.VersionArchived, StringComparison.OrdinalIgnoreCase);
            if (archived) node = MotorNode.Archived;

            row.Node = node;
            row.NodeText = MotorNode.Title(node);
            row.Stage = MotorNode.Stage(node);
            // 节点 6 还在回传签字版；签字版已上传时直接挪到承认书阶段
            if (node == MotorNode.GgSign && MotorFlow.IsGgDone(gg?.Status))
                row.Stage = MotorFlow.StageCr;
            row.StageText = row.NodeText;

            row.StepLxDone = MotorFlow.IsLxDone(row.Status);
            row.StepGgDone = MotorFlow.IsGgDone(gg?.Status);
            row.StepCrDone = MotorFlow.IsCrDone(cr?.Status);
            row.StepArchived = archived;

            row.GgDocGuid = gg?.DocGuid ?? "";
            row.CrDocGuid = cr?.DocGuid ?? "";
            row.GgStatus = gg?.Status;
            row.CrStatus = cr?.Status;

            row.StageTime = cr?.UpdateTime ?? gg?.UpdateTime ?? row.UpdateTime;

            // 版本表还没回填待办人时（老数据），按节点规则从项目表临时推一个
            var pendingEmpNo = ver?.PendingEmpNo;
            var pendingRole = ver?.PendingRole;
            if (string.IsNullOrWhiteSpace(pendingEmpNo) && string.IsNullOrWhiteSpace(pendingRole))
            {
                var (emp, role) = MotorNode.PendingOf(
                    node, row.OwnerEmpNo, row.EngineerEmpNo, ver?.FinalEmpNo);
                pendingEmpNo = emp;
                pendingRole = role;
            }

            row.PendingEmpNo = pendingEmpNo;
            row.PendingRole = pendingRole;
            row.PendingName = MotorNode.PendingName(
                node, pendingEmpNo, pendingRole,
                row.OwnerName, row.OwnerEmpNo,
                row.EngineerName, row.EngineerEmpNo,
                ver is null ? null : NameOf(approverNames, ver.ManagerEmpNo),
                ver is null ? null : NameOf(approverNames, ver.FinalEmpNo));

            row.IsMyTodo = MotorNode.IsMine(node, pendingEmpNo, pendingRole, myEmpNo, myRole);
        }
    }

    private static string VerKey(string? projectGuid, string? versionNo)
        => (projectGuid ?? "").Trim() + "|" + (versionNo ?? "").Trim();

    /// <summary>
    /// 删除项目（软删除）：项目行标 X + 状态置为作废，
    /// 同时把该项目下所有版本的立项单 / 规格书 / 承认书一并标删，
    /// 避免详情页按 DocGuid 打开时还能看到已删项目。
    /// 返回受影响的项目数。
    /// </summary>
    public async Task<int> SoftDeleteAsync(IEnumerable<string> docGuids, string? empNo, CancellationToken ct = default)
    {
        var guids = docGuids
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (guids.Count == 0) return 0;

        // 列表给的是当前版本立项单的 DocGuid，先反查项目
        var projectGuids = await _db.MotorLx.AsNoTracking()
            .Where(x => guids.Contains(x.DocGuid) && x.DeleteFlag != "X")
            .Select(x => x.ProjectGuid)
            .Distinct()
            .ToListAsync(ct);
        if (projectGuids.Count == 0) return 0;

        var now = DateTime.Now;

        var projects = await _db.Projects
            .Where(x => projectGuids.Contains(x.ProjectGuid) && x.DeleteFlag != "X")
            .ToListAsync(ct);

        var lxs = await _db.MotorLx
            .Where(x => projectGuids.Contains(x.ProjectGuid) && x.DeleteFlag != "X")
            .ToListAsync(ct);

        var ggs = await _db.MotorGg
            .Where(x => projectGuids.Contains(x.ProjectGuid) && x.DeleteFlag != "X")
            .ToListAsync(ct);

        var crs = await _db.MotorCr
            .Where(x => projectGuids.Contains(x.ProjectGuid) && x.DeleteFlag != "X")
            .ToListAsync(ct);

        foreach (var row in lxs)
        {
            row.DeleteFlag = "X";
            row.DeleteTime = now;
            row.DeletePersonCode = empNo;
        }

        foreach (var row in ggs)
        {
            row.DeleteFlag = "X";
            row.DeleteTime = now;
            row.DeletePersonCode = empNo;
        }

        foreach (var row in crs)
        {
            row.DeleteFlag = "X";
            row.DeleteTime = now;
            row.DeletePersonCode = empNo;
        }

        foreach (var p in projects)
        {
            p.DeleteFlag = "X";
            p.DeleteTime = now;
            p.DeletePersonCode = empNo;
            p.Status = ProjectStatus.Void;
            p.UpdateTime = now;
            p.UpdatePersonCode = empNo;
        }

        await _db.SaveChangesAsync(ct);

        // 项目行缺失时（老数据）至少回报单据数，避免页面提示「没有可删除的记录」
        return projects.Count > 0 ? projects.Count : lxs.Count;
    }

    private static IQueryable<ProjectModel> ApplyProjectKeyword(
        IQueryable<ProjectModel> q, string? filterColumn, string keyword)
    {
        return filterColumn switch
        {
            MotorLxFilterColumns.DemandPerson =>
                q.Where(x => x.DemandPerson != null && x.DemandPerson.Contains(keyword)),
            MotorLxFilterColumns.CustomerPartCode =>
                q.Where(x => x.CustomerPartCode != null && x.CustomerPartCode.Contains(keyword)),
            MotorLxFilterColumns.MotorModelSpec =>
                q.Where(x => x.MotorModelSpec != null && x.MotorModelSpec.Contains(keyword)),
            MotorLxFilterColumns.Description =>
                q.Where(x => x.Description != null && x.Description.Contains(keyword)),
            MotorLxFilterColumns.Status =>
                q.Where(x => x.Status.Contains(keyword)),
            _ => q.Where(x => x.Customer != null && x.Customer.Contains(keyword))
        };
    }

    /// <summary>排序列白名单；统一追加 Id 作为并列时的次序，保证翻页结果稳定。</summary>
    private static IQueryable<ProjectModel> ApplyProjectSort(
        IQueryable<ProjectModel> q, string? sortLabel, bool descending)
    {
        return sortLabel switch
        {
            "Customer" => descending
                ? q.OrderByDescending(x => x.Customer).ThenByDescending(x => x.Id)
                : q.OrderBy(x => x.Customer).ThenBy(x => x.Id),
            "DemandPerson" => descending
                ? q.OrderByDescending(x => x.DemandPerson).ThenByDescending(x => x.Id)
                : q.OrderBy(x => x.DemandPerson).ThenBy(x => x.Id),
            "CustomerPartCode" => descending
                ? q.OrderByDescending(x => x.CustomerPartCode).ThenByDescending(x => x.Id)
                : q.OrderBy(x => x.CustomerPartCode).ThenBy(x => x.Id),
            "MotorModelSpec" => descending
                ? q.OrderByDescending(x => x.MotorModelSpec).ThenByDescending(x => x.Id)
                : q.OrderBy(x => x.MotorModelSpec).ThenBy(x => x.Id),
            "Status" => descending
                ? q.OrderByDescending(x => x.Status).ThenByDescending(x => x.Id)
                : q.OrderBy(x => x.Status).ThenBy(x => x.Id),
            "UpdateTime" => descending
                ? q.OrderByDescending(x => x.UpdateTime).ThenByDescending(x => x.Id)
                : q.OrderBy(x => x.UpdateTime).ThenBy(x => x.Id),
            _ => descending
                ? q.OrderByDescending(x => x.CreateTime).ThenByDescending(x => x.Id)
                : q.OrderBy(x => x.CreateTime).ThenBy(x => x.Id)
        };
    }

    private static void CopyBusinessFields(MotorLxModel src, MotorLxModel dest)
    {
        dest.Customer = src.Customer;
        dest.DemandPerson = src.DemandPerson;
        dest.CustomerPartCode = src.CustomerPartCode;
        dest.MotorModelSpec = src.MotorModelSpec;
        dest.Description = src.Description;
        dest.TimeToRequest = src.TimeToRequest;
        dest.RequiredCompletionTime = src.RequiredCompletionTime;

        dest.InputVoltage = src.InputVoltage;
        dest.InputVoltageOther = src.InputVoltageOther;
        dest.RatedCurrentNoLoad = src.RatedCurrentNoLoad;
        dest.RatedCurrentFullLoad = src.RatedCurrentFullLoad;
        dest.RatedPower = src.RatedPower;
        dest.RatedTorque = src.RatedTorque;
        dest.RatedLevelSpeed = src.RatedLevelSpeed;
        dest.DefaultRunningSpeed = src.DefaultRunningSpeed;
        dest.OperatingNoise = src.OperatingNoise;
        dest.FastCharge = src.FastCharge;
        dest.MaxRunningTime = src.MaxRunningTime;
        dest.MotorHead = src.MotorHead;
        dest.Shaft = src.Shaft;
        dest.OuterTubeDiameter = src.OuterTubeDiameter;
        dest.OuterTubeColor = src.OuterTubeColor;
        dest.OuterTubeLengthLimit = src.OuterTubeLengthLimit;
        dest.MotorWeight = src.MotorWeight;
        dest.MotorLength = src.MotorLength;
        dest.OperationMethod = src.OperationMethod;
        dest.ProgramCode = src.ProgramCode;
        dest.CrownAndDrive = src.CrownAndDrive;
        dest.RemoteController = src.RemoteController;
        dest.ChargingMethod = src.ChargingMethod;
        dest.ChargingMethodNo = src.ChargingMethodNo;
        dest.JstPlugV = src.JstPlugV;
        dest.JstPlugA = src.JstPlugA;

        dest.LeadWireSpecification = src.LeadWireSpecification;
        dest.CordCertified = src.CordCertified;
        dest.CordColor = src.CordColor;
        dest.CordColorOther = src.CordColorOther;
        dest.CordType = src.CordType;
        dest.CordTypeOther = src.CordTypeOther;
        dest.CordPlug = src.CordPlug;
        dest.CordLength = src.CordLength;
        dest.CordOtherRequirements = src.CordOtherRequirements;
        dest.WireLength = src.WireLength;

        dest.RemoteFrequency = src.RemoteFrequency;
        dest.RemoteFrequencyOther = src.RemoteFrequencyOther;
        dest.MotorFunction = src.MotorFunction;
        dest.RemoteDistance = src.RemoteDistance;
        dest.RemoteTechnology = src.RemoteTechnology;
        dest.RemoteProtocol = src.RemoteProtocol;
        dest.RemoteProtocolOther = src.RemoteProtocolOther;
        dest.SmartModule = src.SmartModule;
        dest.SmartModuleOther = src.SmartModuleOther;
        dest.SignalReceivingDistance = src.SignalReceivingDistance;
        dest.SignalReceivingProtocol = src.SignalReceivingProtocol;

        dest.BuiltInLithiumWhether = src.BuiltInLithiumWhether;
        dest.BuiltInLithiumBatteryCell = src.BuiltInLithiumBatteryCell;
        dest.BuiltInLithiumBattery2 = src.BuiltInLithiumBattery2;
        dest.InputPower = src.InputPower;
        dest.IpLevel = src.IpLevel;
        dest.IpLevelOther = src.IpLevelOther;
        dest.UsageEnv = src.UsageEnv;
        dest.UsageEnvOther = src.UsageEnvOther;

        dest.Application1 = src.Application1;
        dest.Application2 = src.Application2;
        dest.Application3 = src.Application3;
        dest.Application4 = src.Application4;
        dest.Application5 = src.Application5;
        dest.Application6 = src.Application6;
        dest.Application7 = src.Application7;
        dest.Application8 = src.Application8;
        dest.Application9 = src.Application9;
        dest.Application10 = src.Application10;

        dest.MaxWidth = src.MaxWidth;
        dest.MaxHeight = src.MaxHeight;
        dest.PayloadWeight = src.PayloadWeight;
        dest.MarketDemandYear = src.MarketDemandYear;
        dest.MarketDemandQuantity = src.MarketDemandQuantity;
        dest.IsPrototypeNeeded = src.IsPrototypeNeeded;
        dest.PrototypeProvidedTime = src.PrototypeProvidedTime;
        dest.PrototypeQuantity = src.PrototypeQuantity;
        dest.ExpectedPrototypeConfirmTime = src.ExpectedPrototypeConfirmTime;

        dest.SCRequirementUL = src.SCRequirementUL;
        dest.SCRequirementCE = src.SCRequirementCE;
        dest.SCRequirementFCC = src.SCRequirementFCC;
        dest.SCRequirementROHS = src.SCRequirementROHS;
        dest.SCRequirementOtherC = src.SCRequirementOtherC;
        dest.SCRequirementOther = src.SCRequirementOther;

        dest.SpecialFunction = src.SpecialFunction;
        dest.PackageMaxSize = src.PackageMaxSize;
        dest.PackageMaxWeight = src.PackageMaxWeight;
        dest.PackageOther = src.PackageOther;
        dest.OtherRequirement = src.OtherRequirement;
        dest.DemandChange = src.DemandChange;
        dest.Remark = src.Remark;
    }
}
