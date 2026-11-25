using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 规格书 / 承认书的数据库读写。
///
/// 约定：
/// 1. 页面上无法用固定列承载的内容（参数表、配件、包装、操作说明、变更履历）
///    统一序列化到 ExtraJson 一列，读写都经过 <see cref="RemoteDocExtra"/>；
/// 2. 只有草稿状态可以保存内容，已提交审批的单据写库前会被拦下，
///    避免审批过程中内容被悄悄改掉；
/// 3. 客户 / 型号 / 品名 / 需求人 属于项目级字段，保存时同步回 T_ZYQD_Project，
///    保证项目列表和单据上看到的是同一份。
/// </summary>
public class RemoteDocService
{
    private readonly SqlServerDbContext _db;
    private readonly RemotePermissionService _perm;
    private readonly UserDirectoryService _people;

    public RemoteDocService(SqlServerDbContext db, RemotePermissionService perm, UserDirectoryService people)
    {
        _db = db;
        _perm = perm;
        _people = people;
    }

    /// <summary>保存时不由页面覆盖的字段（版本、状态、审批与签字信息由流程服务维护）。</summary>
    private static readonly HashSet<string> GgSkip = new(StringComparer.Ordinal)
    {
        nameof(RemoteGgModel.Id), nameof(RemoteGgModel.ProjectGuid), nameof(RemoteGgModel.DocGuid),
        nameof(RemoteGgModel.VersionNo), nameof(RemoteGgModel.IsDraft), nameof(RemoteGgModel.Status),
        nameof(RemoteGgModel.CreateTime), nameof(RemoteGgModel.CreatePersonCode),
        nameof(RemoteGgModel.UpdateTime), nameof(RemoteGgModel.UpdatePersonCode),
        nameof(RemoteGgModel.DeleteFlag), nameof(RemoteGgModel.DeleteTime), nameof(RemoteGgModel.DeletePersonCode),
        nameof(RemoteGgModel.ApproverCode), nameof(RemoteGgModel.ApproveTime),
        nameof(RemoteGgModel.SignFileId), nameof(RemoteGgModel.SignFileName),
        nameof(RemoteGgModel.SignUploadTime), nameof(RemoteGgModel.SignUploadPersonCode),
        nameof(RemoteGgModel.InheritedFromVersion), nameof(RemoteGgModel.ExtraJson)
    };

    private static readonly HashSet<string> CrSkip = new(StringComparer.Ordinal)
    {
        nameof(RemoteCrModel.Id), nameof(RemoteCrModel.ProjectGuid), nameof(RemoteCrModel.DocGuid),
        nameof(RemoteCrModel.VersionNo), nameof(RemoteCrModel.IsDraft), nameof(RemoteCrModel.Status),
        nameof(RemoteCrModel.CreateTime), nameof(RemoteCrModel.CreatePersonCode),
        nameof(RemoteCrModel.UpdateTime), nameof(RemoteCrModel.UpdatePersonCode),
        nameof(RemoteCrModel.DeleteFlag), nameof(RemoteCrModel.DeleteTime), nameof(RemoteCrModel.DeletePersonCode),
        nameof(RemoteCrModel.ManagerCode), nameof(RemoteCrModel.ManagerApproveTime),
        nameof(RemoteCrModel.JiangCode), nameof(RemoteCrModel.JiangApproveTime),
        nameof(RemoteCrModel.SignFileId), nameof(RemoteCrModel.SignFileName),
        nameof(RemoteCrModel.SignUploadTime), nameof(RemoteCrModel.SignUploadPersonCode),
        nameof(RemoteCrModel.InheritedFromVersion), nameof(RemoteCrModel.ExtraJson)
    };

    // ==================== 读取 ====================

    public Task<RemoteGgModel?> GetGgAsync(string? docGuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(docGuid)) return Task.FromResult<RemoteGgModel?>(null);
        return _db.RemoteGg.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocGuid == docGuid && x.DeleteFlag != "X", ct);
    }

    public Task<RemoteCrModel?> GetCrAsync(string? docGuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(docGuid)) return Task.FromResult<RemoteCrModel?>(null);
        return _db.RemoteCr.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocGuid == docGuid && x.DeleteFlag != "X", ct);
    }

    /// <summary>某项目某版本的规格书（同一版本只应有一份，取最新插入的那条容错）。</summary>
    public Task<RemoteGgModel?> GetGgByVersionAsync(string projectGuid, string versionNo, CancellationToken ct = default)
        => _db.RemoteGg.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

    public Task<RemoteCrModel?> GetCrByVersionAsync(string projectGuid, string versionNo, CancellationToken ct = default)
        => _db.RemoteCr.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

    /// <summary>项目主表（页面要读客户/型号/负责人等）。</summary>
    public Task<ProjectModel?> GetProjectAsync(string? projectGuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectGuid)) return Task.FromResult<ProjectModel?>(null);
        return _db.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.ProjectGuid == projectGuid, ct);
    }

    /// <summary>
    /// 一次查出某项目某版本下三份单据的 DocGuid 与状态，
    /// 供页面左侧时间线判断「第几步已生成、能不能点、点了跳哪」。
    /// 同版本理论上各一份，仍按 Id 倒序取最新一条容错。
    /// </summary>
    public async Task<RemoteVersionDocs> GetVersionDocsAsync(
        string? projectGuid, string? versionNo, CancellationToken ct = default)
    {
        var result = new RemoteVersionDocs();
        if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(versionNo))
            return result;

        var pg = projectGuid.Trim();
        var vn = versionNo.Trim();

        var lx = await _db.RemoteLx.AsNoTracking()
            .Where(x => x.ProjectGuid == pg && x.VersionNo == vn && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .Select(x => new { x.DocGuid, x.Status, x.EngineerCode })
            .FirstOrDefaultAsync(ct);

        var gg = await _db.RemoteGg.AsNoTracking()
            .Where(x => x.ProjectGuid == pg && x.VersionNo == vn && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .Select(x => new { x.DocGuid, x.Status })
            .FirstOrDefaultAsync(ct);

        var cr = await _db.RemoteCr.AsNoTracking()
            .Where(x => x.ProjectGuid == pg && x.VersionNo == vn && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .Select(x => new { x.DocGuid, x.Status })
            .FirstOrDefaultAsync(ct);

        result.LxDocGuid = lx?.DocGuid;
        result.LxStatus = lx?.Status;
        result.GgDocGuid = gg?.DocGuid;
        result.GgStatus = gg?.Status;
        result.CrDocGuid = cr?.DocGuid;
        result.CrStatus = cr?.Status;

        // 流程节点只在版本表上，时间线高亮直接用它，不再由三张单据的状态反推；
        // 这里整行取回，七个流程的 21 列留痕一并带出给「流程记录」用
        var version = await _db.ProjectVersions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProjectGuid == pg && x.VersionNo == vn, ct);

        result.CurrentNode = version?.CurrentNode ?? 0;
        result.ManagerEmpNo = version?.ManagerEmpNo;
        result.VersionStatus = version?.Status;
        result.Version = version;

        // 流程记录按版本分组展示整条历史：本项目所有版本行一次取回（含 21 列留痕）
        result.Versions = await _db.ProjectVersions.AsNoTracking()
            .Where(x => x.ProjectGuid == pg)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        // 权限信息：业务员（改立项单/承认书、回传签字版、归档）+ 研发工程师（改规格书）
        var project = await _db.Projects.AsNoTracking()
            .Where(x => x.ProjectGuid == pg)
            .Select(x => new { x.OwnerEmpNo })
            .FirstOrDefaultAsync(ct);

        result.OwnerEmpNo = project?.OwnerEmpNo;
        result.EngineerEmpNo = lx?.EngineerCode;

        // 展示用姓名统一从员工表批量取（业务表只存工号）
        var names = await LoadEmpNamesAsync(
            new[] { result.OwnerEmpNo, result.EngineerEmpNo, result.ManagerEmpNo }, ct);
        result.OwnerName = Lookup(names, result.OwnerEmpNo);
        result.EngineerName = Lookup(names, result.EngineerEmpNo);
        result.ManagerName = Lookup(names, result.ManagerEmpNo);

        await FillFlowExtrasAsync(result, version, pg, ct);

        return result;
    }

    /// <summary>
    /// 按单据 DocGuid 反查它所属的项目与版本号。
    /// 单据页只拿得到 URL 上的 DocGuid，需要先定位到「项目 + 版本」才能取整屏数据。
    /// </summary>
    public async Task<(string? ProjectGuid, string? VersionNo)> LocateAsync(
        string? kind, string? docGuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(docGuid)) return (null, null);

        var g = docGuid.Trim();
        var k = (kind ?? "lx").Trim().ToLowerInvariant();

        if (k == "gg")
        {
            var gg = await _db.RemoteGg.AsNoTracking()
                .Where(x => x.DocGuid == g && x.DeleteFlag != "X")
                .Select(x => new { x.ProjectGuid, x.VersionNo })
                .FirstOrDefaultAsync(ct);
            return (gg?.ProjectGuid, gg?.VersionNo);
        }

        if (k == "cr")
        {
            var cr = await _db.RemoteCr.AsNoTracking()
                .Where(x => x.DocGuid == g && x.DeleteFlag != "X")
                .Select(x => new { x.ProjectGuid, x.VersionNo })
                .FirstOrDefaultAsync(ct);
            return (cr?.ProjectGuid, cr?.VersionNo);
        }

        var lx = await _db.RemoteLx.AsNoTracking()
            .Where(x => x.DocGuid == g && x.DeleteFlag != "X")
            .Select(x => new { x.ProjectGuid, x.VersionNo })
            .FirstOrDefaultAsync(ct);
        return (lx?.ProjectGuid, lx?.VersionNo);
    }

    /// <summary>
    /// 一屏三单：一次取齐三张单据整行 + 版本行 + 项目行 + 版本下拉。
    /// 共 6 条查询（串行；同一个 DbContext 不允许并发），
    /// 替代原来三个页面各自查 9~10 条、合计约 29 条的重复开销。
    /// </summary>
    public async Task<RemoteDocBundle> GetBundleAsync(
        string? projectGuid, string? versionNo, CancellationToken ct = default)
    {
        var bundle = new RemoteDocBundle
        {
            ProjectGuid = projectGuid?.Trim(),
            VersionNo = versionNo?.Trim()
        };

        if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(versionNo))
            return bundle;

        var pg = projectGuid.Trim();
        var vn = versionNo.Trim();

        // 1~3. 三张单据整行（同版本理论上各一份，仍按 Id 倒序取最新一条容错）
        bundle.Lx = await _db.RemoteLx.AsNoTracking()
            .Where(x => x.ProjectGuid == pg && x.VersionNo == vn && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        bundle.Gg = await _db.RemoteGg.AsNoTracking()
            .Where(x => x.ProjectGuid == pg && x.VersionNo == vn && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        bundle.Cr = await _db.RemoteCr.AsNoTracking()
            .Where(x => x.ProjectGuid == pg && x.VersionNo == vn && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        // 4. 版本行：全部版本一次取回，当前版本从里面挑，省掉单独一次 FirstOrDefault
        var versions = await _db.ProjectVersions.AsNoTracking()
            .Where(x => x.ProjectGuid == pg)
            .OrderByDescending(x => x.Id)
            .ToListAsync(ct);
        bundle.Versions = versions;

        // 4.1 本版本最近一次退回原因：三个子组件共用同一份，避免并发查同一个 DbContext
        bundle.LatestReject = await _db.ApprovalFlows.AsNoTracking()
            .Where(x => x.ProjectGuid == pg && x.DocVersion == vn && x.Action == RemoteFlow.ActionReject)
            .OrderByDescending(x => x.ActionTime)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        var version = versions.FirstOrDefault(x => RemoteFlow.Same(x.VersionNo, vn));

        // 5. 项目行：当前版本号（下拉标注）+ 业务员（改单/归档）+ 研发工程师（改规格书）
        var project = await _db.Projects.AsNoTracking()
            .Where(x => x.ProjectGuid == pg)
            .Select(x => new
            {
                x.CurrentVersionNo,
                x.OwnerEmpNo
            })
            .FirstOrDefaultAsync(ct);

        // 6. 版本号列表：按立项单口径（V1 不一定写版本表），版本表只做并集补齐
        var lxVersions = await _db.RemoteLx.AsNoTracking()
            .Where(x => x.ProjectGuid == pg && x.DeleteFlag != "X")
            .Select(x => new { x.VersionNo, x.Id })
            .ToListAsync(ct);

        var nos = lxVersions
            .OrderByDescending(x => x.Id)
            .Select(x => (x.VersionNo ?? "").Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var v in versions)
        {
            var no = (v.VersionNo ?? "").Trim();
            if (no.Length == 0) continue;
            if (!nos.Any(x => RemoteFlow.Same(x, no))) nos.Add(no);
        }

        bundle.VersionNos = nos;
        bundle.CurrentVersionNo = project?.CurrentVersionNo;

        bundle.Docs = new RemoteVersionDocs
        {
            LxDocGuid = bundle.Lx?.DocGuid,
            LxStatus = bundle.Lx?.Status,
            GgDocGuid = bundle.Gg?.DocGuid,
            GgStatus = bundle.Gg?.Status,
            CrDocGuid = bundle.Cr?.DocGuid,
            CrStatus = bundle.Cr?.Status,
            CurrentNode = version?.CurrentNode ?? 0,
            VersionStatus = version?.Status,
            OwnerEmpNo = project?.OwnerEmpNo,
            EngineerEmpNo = bundle.Lx?.EngineerCode,
            ManagerEmpNo = version?.ManagerEmpNo,
            Version = version,
            Versions = versions
        };

        // 展示用姓名统一从员工表批量取（业务表只存工号）
        var docNames = await LoadEmpNamesAsync(
            new[] { bundle.Docs.OwnerEmpNo, bundle.Docs.EngineerEmpNo, bundle.Docs.ManagerEmpNo }, ct);
        bundle.Docs.OwnerName = Lookup(docNames, bundle.Docs.OwnerEmpNo);
        bundle.Docs.EngineerName = Lookup(docNames, bundle.Docs.EngineerEmpNo);
        bundle.Docs.ManagerName = Lookup(docNames, bundle.Docs.ManagerEmpNo);

        // 4.2 / 4.3 必须在 Docs 构造完之后填，否则会被新对象覆盖掉
        await FillFlowExtrasAsync(bundle.Docs, version, pg, ct);

        return bundle;
    }

    /// <summary>
    /// 流程记录表要用的姓名表和流转留痕，bundle 与单据单独加载两条路径都走这里。
    /// 流程记录按版本分组展示整条历史，所以姓名表和流转记录都按「项目所有版本」取。
    /// </summary>
    private async Task FillFlowExtrasAsync(
        RemoteVersionDocs docs, ProjectVersionModel? version, string pg, CancellationToken ct)
    {
        var versions = new List<ProjectVersionModel>();
        if (docs.Versions.Count > 0) versions.AddRange(docs.Versions);
        else if (version is not null) versions.Add(version);

        var empNos = versions
            .SelectMany(v => Enumerable.Range(RemoteNode.LxCreate, RemoteNode.LastNode - RemoteNode.LxCreate + 1)
                .Select(n => v.NodeRecord(n).EmpNo))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (empNos.Count > 0)
        {
            var cards = await _people.GetManyAsync(empNos, ct);
            foreach (var no in empNos)
            {
                if (cards.TryGetValue(no!, out var card) && !string.IsNullOrWhiteSpace(card.Name))
                    docs.NodeNames[no!] = card.Name.Trim();
            }
        }

        // 按时间还原每一步（提交/审批/退回/回传/归档）：版本内的多轮退回也要看得见，
        // 所以整项目的流转记录一次取回，由页面按 DocVersion 分组、翻译成业务节点名。
        // 取最新 500 条（历史项目版本多，老到被截断的也只是更早的版本），页面内再按版本排序。
        docs.ExtraFlows = await _db.ApprovalFlows.AsNoTracking()
            .Where(x => x.ProjectGuid == pg)
            .OrderByDescending(x => x.ActionTime)
            .ThenByDescending(x => x.Id)
            .Take(500)
            .ToListAsync(ct);

        // 流程日志：整项目一次取回，按版本 + 序号排好，页面直接渲染。
        // 这份是「权威日志」，不再需要从流水倒推节点、操作人、相关人。
        var logs = await _db.ProjectFlowLogs.AsNoTracking()
            .Where(x => x.ProjectGuid == pg)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        docs.FlowLogs = logs
            .OrderBy(x => x.VersionNo, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Seq)
            .ThenBy(x => x.Id)
            .ToList();

        // 日志里出现的操作人 / 相关人也一并取到姓名，表格直接显示
        var logEmps = docs.FlowLogs
            .SelectMany(x => new[] { x.OperatorCode, x.RelatedCode })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => !docs.NodeNames.ContainsKey(x))
            .ToList();

        if (logEmps.Count > 0)
        {
            var logCards = await _people.GetManyAsync(logEmps, ct);
            foreach (var no in logEmps)
            {
                if (logCards.TryGetValue(no, out var card) && !string.IsNullOrWhiteSpace(card.Name))
                    docs.NodeNames[no] = card.Name.Trim();
            }
        }
    }

    // ==================== 保存 ====================

    /// <summary>
    /// 保存规格书草稿。DocGuid 为空则新建（补 ProjectGuid / VersionNo / 草稿状态）。
    /// </summary>
    public async Task<RemoteSheetSaveResult> SaveGgAsync(
        RemoteGgModel input, RemoteDocExtra extra,
        string? empNo, string? empName,
        string fallbackVersionNo = RemoteLxService.FirstVersionNo,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var deny = await _perm.CheckEditorAsync(RemoteFlow.DocGg, input.ProjectGuid, empNo, ct);
        if (deny is not null) throw new InvalidOperationException(deny);

        var now = DateTime.Now;

        if (string.IsNullOrWhiteSpace(input.DocGuid))
        {
            var projectGuid = string.IsNullOrWhiteSpace(input.ProjectGuid)
                ? Guid.NewGuid().ToString("N")
                : input.ProjectGuid;

            var row = new RemoteGgModel
            {
                ProjectGuid = projectGuid,
                DocGuid = Guid.NewGuid().ToString("N"),
                VersionNo = string.IsNullOrWhiteSpace(input.VersionNo) ? fallbackVersionNo : input.VersionNo,
                IsDraft = true,
                Status = RemoteFlow.GgDraft,
                SourceLxDocGuid = input.SourceLxDocGuid,
                ExtraJson = RemoteDocExtra.Normalize(extra).ToJson(),
                CreateTime = now,
                CreatePersonCode = empNo,
                UpdateTime = now,
                UpdatePersonCode = empNo,
                DeleteFlag = ""
            };
            CopyBusiness(input, row, GgSkip);
            ApplyExtraToColumns(row, extra);

            _db.RemoteGg.Add(row);
            var ggNewVersion = await _db.ProjectVersions
                .FirstOrDefaultAsync(x => x.ProjectGuid == row.ProjectGuid && x.VersionNo == row.VersionNo, ct);
            ggNewVersion?.TouchNodeTime(RemoteNode.GgWrite, now, empNo);
            await SyncProjectAsync(row.ProjectGuid, extra, row.VersionNo, empNo, empName, ct);
            await _db.SaveChangesAsync(ct);

            input.ProjectGuid = row.ProjectGuid;
            input.DocGuid = row.DocGuid;
            input.VersionNo = row.VersionNo;
            return new RemoteSheetSaveResult
            {
                DocGuid = row.DocGuid,
                ProjectGuid = row.ProjectGuid,
                VersionNo = row.VersionNo,
                Created = true
            };
        }

        var existing = await _db.RemoteGg
            .FirstOrDefaultAsync(x => x.DocGuid == input.DocGuid && x.DeleteFlag != "X", ct)
            ?? throw new InvalidOperationException("未找到规格书，可能已被删除");

        if (!CanEdit(existing.Status))
            throw new InvalidOperationException($"规格书当前状态为「{RemoteFlow.StatusName(existing.Status)}」，已提交审批，不能再修改");

        CopyBusiness(input, existing, GgSkip);
        existing.ExtraJson = RemoteDocExtra.Normalize(extra).ToJson();
        ApplyExtraToColumns(existing, extra);
        existing.UpdateTime = now;
        existing.UpdatePersonCode = empNo;

        // 「编写规格书」提交前每次保存都刷新时间，操作人就是保存人
        var ggVersion = await _db.ProjectVersions
            .FirstOrDefaultAsync(x => x.ProjectGuid == existing.ProjectGuid && x.VersionNo == existing.VersionNo, ct);
        ggVersion?.TouchNodeTime(RemoteNode.GgWrite, now, empNo);

        await SyncProjectAsync(existing.ProjectGuid, extra, existing.VersionNo, empNo, empName, ct);
        await _db.SaveChangesAsync(ct);

        input.ProjectGuid = existing.ProjectGuid;
        input.VersionNo = existing.VersionNo;
        return new RemoteSheetSaveResult
        {
            DocGuid = existing.DocGuid,
            ProjectGuid = existing.ProjectGuid,
            VersionNo = existing.VersionNo,
            Created = false
        };
    }

    /// <summary>保存承认书草稿，规则同规格书。</summary>
    public async Task<RemoteSheetSaveResult> SaveCrAsync(
        RemoteCrModel input, RemoteDocExtra extra,
        string? empNo, string? empName,
        string fallbackVersionNo = RemoteLxService.FirstVersionNo,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var deny = await _perm.CheckEditorAsync(RemoteFlow.DocCr, input.ProjectGuid, empNo, ct);
        if (deny is not null) throw new InvalidOperationException(deny);

        var now = DateTime.Now;

        if (string.IsNullOrWhiteSpace(input.DocGuid))
        {
            var projectGuid = string.IsNullOrWhiteSpace(input.ProjectGuid)
                ? Guid.NewGuid().ToString("N")
                : input.ProjectGuid;

            var row = new RemoteCrModel
            {
                ProjectGuid = projectGuid,
                DocGuid = Guid.NewGuid().ToString("N"),
                VersionNo = string.IsNullOrWhiteSpace(input.VersionNo) ? fallbackVersionNo : input.VersionNo,
                IsDraft = true,
                Status = RemoteFlow.CrDraft,
                SourceGgDocGuid = input.SourceGgDocGuid,
                SourceLxDocGuid = input.SourceLxDocGuid,
                ExtraJson = RemoteDocExtra.Normalize(extra).ToJson(),
                CreateTime = now,
                CreatePersonCode = empNo,
                UpdateTime = now,
                UpdatePersonCode = empNo,
                DeleteFlag = ""
            };
            CopyBusiness(input, row, CrSkip);
            ApplyExtraToColumns(row, extra);

            _db.RemoteCr.Add(row);
            var crNewVersion = await _db.ProjectVersions
                .FirstOrDefaultAsync(x => x.ProjectGuid == row.ProjectGuid && x.VersionNo == row.VersionNo, ct);
            crNewVersion?.TouchNodeTime(RemoteNode.CrWrite, now, empNo);
            await SyncProjectAsync(row.ProjectGuid, extra, row.VersionNo, empNo, empName, ct);
            await _db.SaveChangesAsync(ct);

            input.ProjectGuid = row.ProjectGuid;
            input.DocGuid = row.DocGuid;
            input.VersionNo = row.VersionNo;
            return new RemoteSheetSaveResult
            {
                DocGuid = row.DocGuid,
                ProjectGuid = row.ProjectGuid,
                VersionNo = row.VersionNo,
                Created = true
            };
        }

        var existing = await _db.RemoteCr
            .FirstOrDefaultAsync(x => x.DocGuid == input.DocGuid && x.DeleteFlag != "X", ct)
            ?? throw new InvalidOperationException("未找到承认书，可能已被删除");

        if (!CanEdit(existing.Status))
            throw new InvalidOperationException($"承认书当前状态为「{RemoteFlow.StatusName(existing.Status)}」，已提交审批，不能再修改");

        CopyBusiness(input, existing, CrSkip);
        existing.ExtraJson = RemoteDocExtra.Normalize(extra).ToJson();
        ApplyExtraToColumns(existing, extra);
        existing.UpdateTime = now;
        existing.UpdatePersonCode = empNo;

        // 「编写承认书」提交前每次保存都刷新时间，操作人就是保存人
        var crVersion = await _db.ProjectVersions
            .FirstOrDefaultAsync(x => x.ProjectGuid == existing.ProjectGuid && x.VersionNo == existing.VersionNo, ct);
        crVersion?.TouchNodeTime(RemoteNode.CrWrite, now, empNo);

        await SyncProjectAsync(existing.ProjectGuid, extra, existing.VersionNo, empNo, empName, ct);
        await _db.SaveChangesAsync(ct);

        input.ProjectGuid = existing.ProjectGuid;
        input.VersionNo = existing.VersionNo;
        return new RemoteSheetSaveResult
        {
            DocGuid = existing.DocGuid,
            ProjectGuid = existing.ProjectGuid,
            VersionNo = existing.VersionNo,
            Created = false
        };
    }

    // ==================== 内部 ====================

    /// <summary>
    /// 批量取「工号 → 姓名」，用于给 DTO 填展示用姓名。
    /// 业务表只存工号，姓名统一从 T_ZYQD_Employee 取，员工改名不用回刷业务数据。
    /// </summary>
    private async Task<Dictionary<string, string>> LoadEmpNamesAsync(
        IEnumerable<string?> empNos, CancellationToken ct)
    {
        var keys = empNos
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (keys.Count == 0) return dict;

        var rows = await _db.Employees.AsNoTracking()
            .Where(x => keys.Contains(x.EmpNo))
            .Select(x => new { x.EmpNo, x.EmpName })
            .ToListAsync(ct);

        foreach (var r in rows)
        {
            if (!string.IsNullOrWhiteSpace(r.EmpName))
                dict[r.EmpNo] = r.EmpName!.Trim();
        }
        return dict;
    }

    private static string? Lookup(Dictionary<string, string> names, string? empNo)
        => !string.IsNullOrWhiteSpace(empNo) && names.TryGetValue(empNo.Trim(), out var n) ? n : null;

    /// <summary>只有草稿（或状态为空的历史数据）允许编辑内容。</summary>
    private static bool CanEdit(string? status)
        => string.IsNullOrWhiteSpace(status)
           || string.Equals(status.Trim(), RemoteFlow.GgDraft, StringComparison.OrdinalIgnoreCase)
           || string.Equals(status.Trim(), RemoteFlow.CrDraft, StringComparison.OrdinalIgnoreCase)
           || string.Equals(status.Trim(), RemoteFlow.LxDraft, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 页面上的主字段同步进单据表的老列，打印/导出仍按这些列取值。
    /// 扩展里没有的键不覆盖，避免把历史数据原有的列值清空。
    /// </summary>
    private static void ApplyExtraToColumns(RemoteGgModel row, RemoteDocExtra extra)
    {
        row.Customer = Field(extra, RemoteDocKeys.Customer) ?? row.Customer;
        row.MotorModelSpec = Field(extra, RemoteDocKeys.PartCode) ?? row.MotorModelSpec;
        var name = Field(extra, RemoteDocKeys.ProductName);
        if (name is not null)
        {
            row.Description = name;
            row.ProductDescription = name;
        }
    }

    private static void ApplyExtraToColumns(RemoteCrModel row, RemoteDocExtra extra)
    {
        row.Customer = Field(extra, RemoteDocKeys.Customer) ?? row.Customer;
        row.MotorModelSpec = Field(extra, RemoteDocKeys.PartCode) ?? row.MotorModelSpec;
        var name = Field(extra, RemoteDocKeys.ProductName);
        if (name is not null)
        {
            row.Description = name;
            row.ProductDescription = name;
        }
    }

    private static string? Field(RemoteDocExtra extra, string key)
        => extra.Fields.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

    /// <summary>
    /// 把单据上的项目级字段刷回项目主表（项目表是列表、权限的唯一来源）。
    /// 单据上有值就以单据为准；单据留空时保留项目表原值，避免误清空。
    /// </summary>
    private async Task SyncProjectAsync(
        string projectGuid, RemoteDocExtra extra, string versionNo,
        string? empNo, string? empName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectGuid)) return;

        var project = await _db.Projects.FirstOrDefaultAsync(x => x.ProjectGuid == projectGuid, ct);
        if (project is null) return;

        var changed = false;

        var customer = Field(extra, RemoteDocKeys.Customer);
        if (customer is not null && !Same(project.Customer, customer))
        {
            project.Customer = customer;
            changed = true;
        }

        var partCode = Field(extra, RemoteDocKeys.PartCode);
        if (partCode is not null && !Same(project.MotorModelSpec, partCode))
        {
            project.MotorModelSpec = partCode;
            changed = true;
        }

        var productName = Field(extra, RemoteDocKeys.ProductName);
        if (productName is not null && !Same(project.Description, productName))
        {
            project.Description = productName;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(project.CurrentVersionNo) && !string.IsNullOrWhiteSpace(versionNo))
        {
            project.CurrentVersionNo = versionNo;
            changed = true;
        }

        if (!changed) return;

        project.UpdateTime = DateTime.Now;
        project.UpdatePersonCode = empNo;
    }

    private static bool Same(string? a, string? b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.Ordinal);

    /// <summary>按属性名把源对象的业务字段拷到目标对象，跳过流程/审计字段。</summary>
    private static void CopyBusiness<T>(T src, T dest, HashSet<string> skip)
    {
        foreach (var p in typeof(T).GetProperties())
        {
            if (!p.CanRead || !p.CanWrite) continue;
            if (skip.Contains(p.Name)) continue;

            var t = p.PropertyType;
            var isScalar = t == typeof(string) || t == typeof(DateTime) || t == typeof(DateTime?)
                           || t == typeof(bool) || t == typeof(bool?);
            if (!isScalar) continue;

            p.SetValue(dest, p.GetValue(src));
        }
    }
}


