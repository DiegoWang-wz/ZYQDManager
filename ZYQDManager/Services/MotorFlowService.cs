using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 电机项目流转服务：立项单 → 规格书 → 承认书 → 归档，以及版本管理。
///
/// 约定：
/// 1. 所有状态变更都写 <see cref="MotorFlow"/> 里的状态码，不再用中文字面量；
/// 2. 每次流转同时往 T_ZYQD_ApprovalFlow 落一条记录（历史 + 待办）；
/// 3. 方法统一返回 (Ok, Message)，调用方直接拿去弹提示，不抛异常给页面。
/// </summary>
public class MotorFlowService
{
    private readonly SqlServerDbContext _db;
    private readonly AppRoleService _roles;
    private readonly MotorPermissionService _perm;
    private readonly RdEngineerDirectoryService _directory;

    public MotorFlowService(
        SqlServerDbContext db,
        AppRoleService roles,
        MotorPermissionService perm,
        RdEngineerDirectoryService directory)
    {
        _db = db;
        _roles = roles;
        _perm = perm;
        _directory = directory;
    }

    // ==================== 立项单 ====================

    /// <summary>
    /// 业务员提交立项单：指定研发工程师，立项单步骤到此完成，
    /// 同时生成该版本的规格书草稿并推给工程师。
    /// </summary>
    public async Task<(bool Ok, string Message)> SubmitLxAsync(
        string lxDocGuid,
        string engineerCode,
        string? engineerName,
        string? empNo,
        string? empName,
        CancellationToken ct = default,
        string? comment = null)
    {
        if (string.IsNullOrWhiteSpace(lxDocGuid))
            return (false, "缺少立项单");
        if (string.IsNullOrWhiteSpace(engineerCode))
            return (false, "请先选择研发工程师");

        var lx = await _db.MotorLx.FirstOrDefaultAsync(x => x.DocGuid == lxDocGuid && x.DeleteFlag != "X", ct);
        if (lx is null) return (false, "未找到立项单，可能已被删除");
        if (MotorFlow.IsLxDone(lx.Status)) return (false, "立项单已经提交过了");

        var deny = await _perm.CheckEditorAsync(MotorFlow.DocLx, lx.ProjectGuid, empNo, ct);
        if (deny is not null) return (false, deny);

        var now = DateTime.Now;
        lx.Status = MotorFlow.LxSubmitted;
        lx.IsDraft = false;
        lx.SubmitTime = now;
        lx.EngineerCode = engineerCode;
        lx.UpdateTime = now;
        lx.UpdatePersonCode = empNo;

        var versionLx = await EnsureVersionAsync(lx.ProjectGuid, lx.VersionNo, empNo, ct);
        // 「创建立项单」的时间取最后一次保存时间（保存草稿时已由 TouchNodeTime 写入）
        if (versionLx.NodeRecord(MotorNode.LxCreate).DoneTime is null)
            versionLx.TouchNodeTime(MotorNode.LxCreate, now, empNo);

        // 项目表记录当前版本号（工程师不存项目表，从立项单取）
        var project = await EnsureProjectAsync(lx.ProjectGuid, empNo, ct);
        if (project is not null)
        {
            project.CurrentVersionNo = lx.VersionNo;
            project.UpdateTime = now;
            project.UpdatePersonCode = empNo;
        }

        // 生成规格书草稿（同一版本内只建一份）
        var gg = await FindGgAsync(lx.ProjectGuid, lx.VersionNo, ct);
        if (gg is null)
        {
            gg = new MotorGgModel
            {
                ProjectGuid = lx.ProjectGuid,
                DocGuid = Guid.NewGuid().ToString("N"),
                VersionNo = lx.VersionNo,
                IsDraft = true,
                Status = MotorFlow.GgDraft,
                SourceLxDocGuid = lx.DocGuid,
                Customer = lx.Customer,
                DemandPerson = lx.DemandPerson,
                CustomerPartCode = lx.CustomerPartCode,
                MotorModelSpec = lx.MotorModelSpec,
                Description = lx.Description,
                CreateTime = now,
                UpdateTime = now,
                CreatePersonCode = empNo,
                DeleteFlag = ""
            };

            // 立项单上业务员已经填过的内容整体搬进规格书 —— 客户、型号、电压、扭矩、
            // 转速、功率、遥控参数、认证要求等全部预填，研发打开就能改，不用再点一次「重新带入」。
            // 映射只有一份（MotorGgFromLx），与规格书页的「重新带入立项单」完全同源。
            var extra = MotorDocExtra.CreateDefault();
            MotorGgFromLx.FillHead(gg, lx);
            MotorGgFromLx.Fill(extra, lx);
            extra.ChangeLog.Add(new MotorChangeLogRow { Date = now.ToString("yyyy-MM-dd"), Rev = "A" });
            gg.ExtraJson = extra.ToJson();

            _db.MotorGg.Add(gg);
        }
        else
        {
            gg.Status = MotorFlow.GgDraft;
            gg.UpdateTime = now;
            gg.UpdatePersonCode = empNo;

            // 已有草稿（例如退回后重新提交）：把立项单里还空着的字段补齐，不覆盖研发已填的内容
            var extra = MotorDocExtra.FromJson(gg.ExtraJson);
            MotorGgFromLx.Fill(extra, lx);
            gg.ExtraJson = extra.ToJson();
        }

        // 上一轮被退回（规格书退回立项单）留下的待办已经处理完：重填提交时先收掉，
        // 否则业务员的待办列表里会一直挂着一条指向规格书的过期提醒。
        await CloseTodosAsync(lx.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, MotorFlow.TodoDone, empNo, ct);

        AddFlow(lx.ProjectGuid, MotorFlow.DocLx, lx.DocGuid, lx.VersionNo, MotorFlow.ActionSubmit,
            MotorFlow.LxSubmitted, empNo, engineerCode, null,
            comment ?? "提交立项单", MotorFlow.TodoDone, now);

        AddFlow(lx.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, gg.VersionNo, MotorFlow.ActionSubmit,
            MotorFlow.GgDraft, empNo, engineerCode, null,
            comment ?? "请填写规格书", MotorFlow.TodoPending, now);

        // 节点 3：等研发工程师写规格书（节点 1、2 随本次提交一并完成）。
        // 工程师以本次提交选的为准，项目表上还没落库，所以这里显式覆盖待办人。
        await MoveNodeAsync(lx.ProjectGuid, lx.VersionNo, MotorNode.GgWrite, now, empNo, ct,
            pendingEmpNoOverride: engineerCode,
            engineerEmpNoOverride: engineerCode,
            comment: comment);

        await _db.SaveChangesAsync(ct);
        return (true, $"已提交给 {engineerName ?? engineerCode}，规格书已推送");
    }

    // ==================== 规格书 ====================

    /// <summary>
    /// 研发工程师填完规格书，指定研发经理后提交审批。
    /// 指定的经理记到版本上（ManagerEmpNo），承认书审批（节点 6）沿用同一个人，不再重复指派。
    /// </summary>
    public async Task<(bool Ok, string Message)> SubmitGgAsync(
        string ggDocGuid,
        string managerEmpNo,
        string? managerName,
        string? empNo,
        string? empName,
        CancellationToken ct = default,
        string? comment = null)
    {
        var gg = await _db.MotorGg.FirstOrDefaultAsync(x => x.DocGuid == ggDocGuid && x.DeleteFlag != "X", ct);
        if (gg is null) return (false, "未找到规格书");
        if (!Same(gg.Status, MotorFlow.GgDraft) && !string.IsNullOrWhiteSpace(gg.Status))
            return (false, "当前状态不能提交审批");

        var deny = await _perm.CheckEditorAsync(MotorFlow.DocGg, gg.ProjectGuid, empNo, ct);
        if (deny is not null) return (false, deny);

        // 流程已经退回立项单（节点 1）：规格书这时是留档状态，要等业务员重填并重新提交
        if (await IsWaitingLxAsync(gg.ProjectGuid, gg.VersionNo, ct))
            return (false, "规格书已退回立项单，等业务员重新提交后再提交审批");

        if (string.IsNullOrWhiteSpace(managerEmpNo))
            return (false, "请先选择研发经理");

        // 指派前先确认这人确实是研发经理（与选人弹窗同源，直读账号库，避免普通业务员调接口被拒）
        if (!await HasDirectoryRoleAsync(MotorFlow.RoleRdManager, managerEmpNo, ct))
            return (false, "所选人员不是研发经理，请确认其账号角色维护正确");

        var now = DateTime.Now;
        gg.Status = MotorFlow.GgPendingManager;
        gg.IsDraft = false;
        gg.UpdateTime = now;
        gg.UpdatePersonCode = empNo;

        await CloseTodosAsync(gg.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, MotorFlow.TodoDone, empNo, ct);

        AddFlow(gg.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, gg.VersionNo, MotorFlow.ActionSubmit,
            MotorFlow.GgPendingManager, empNo,
            managerEmpNo.Trim(), MotorFlow.RoleRdManager,
            comment ?? "规格书提交审批", MotorFlow.TodoPending, now);

        // 先落研发经理：节点 4 的「相关人」要读这一列才写得对
        var version = await EnsureVersionAsync(
            gg.ProjectGuid, gg.VersionNo, empNo, ct);
        version.ManagerEmpNo = managerEmpNo.Trim();

        // 节点 5：等指派的研发经理审批规格书（指定人 + 保留角色，便于显示和兜底）
        await MoveNodeAsync(gg.ProjectGuid, gg.VersionNo, MotorNode.GgApprove, now, empNo, ct,
            pendingEmpNoOverride: managerEmpNo,
            pendingRoleOverride: MotorFlow.RoleRdManager,
            comment: comment);

        await _db.SaveChangesAsync(ct);
        return (true, $"已提交 {managerEmpNo} 审批");
    }

    /// <summary>研发经理审批通过规格书：转入待业务员回传签字版。</summary>
    public async Task<(bool Ok, string Message)> ApproveGgAsync(
        string ggDocGuid,
        string? empNo,
        string? empName,
        string? comment,
        CancellationToken ct = default)
    {
        var gg = await _db.MotorGg.FirstOrDefaultAsync(x => x.DocGuid == ggDocGuid && x.DeleteFlag != "X", ct);
        if (gg is null) return (false, "未找到规格书");
        if (!Same(gg.Status, MotorFlow.GgPendingManager))
            return (false, "当前不是待研发经理审批状态");

        if (!await HasRoleAsync(MotorFlow.RoleRdManager, empNo, ct))
            return (false, "您没有研发经理审批权限");

        var assignError = await _perm.CheckAssignedApproverAsync(
            gg.ProjectGuid, gg.VersionNo, empNo, final: false, ct);
        if (assignError is not null) return (false, assignError);

        var now = DateTime.Now;
        gg.Status = MotorFlow.GgPendingSign;
        gg.ApproverCode = empNo;
        gg.ApproveTime = now;
        gg.UpdateTime = now;
        gg.UpdatePersonCode = empNo;

        await CloseTodosAsync(gg.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, MotorFlow.TodoDone, empNo, ct);

        var lx = await ResolveLxAsync(gg.SourceLxDocGuid, gg.ProjectGuid, gg.VersionNo, ct);
        AddFlow(gg.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, gg.VersionNo, MotorFlow.ActionApprove,
            MotorFlow.GgPendingSign, empNo, lx?.CreatePersonCode, null,
            comment ?? "审批通过，待回传签字版", MotorFlow.TodoPending, now);

        // 节点 6：等业务员回传签字版规格书
        await MoveNodeAsync(gg.ProjectGuid, gg.VersionNo, MotorNode.GgSign, now, empNo, ct,
            comment: comment);

        await _db.SaveChangesAsync(ct);
        return (true, "审批通过，已通知业务员回传签字版");
    }

    /// <summary>
    /// 退回规格书。两种场景：
    ///   1) 研发经理审批阶段：退回工程师重填（GgDraft），或一直退回立项单（LxDraft）；
    ///   2) 工程师编写阶段：发现立项单内容有问题（参数、电压等），只能退回立项单，由业务员重填。
    /// </summary>
    public async Task<(bool Ok, string Message)> RejectGgAsync(
        string ggDocGuid,
        string targetStatus,
        string? empNo,
        string? empName,
        string comment,
        CancellationToken ct = default)
    {
        var gg = await _db.MotorGg.FirstOrDefaultAsync(x => x.DocGuid == ggDocGuid && x.DeleteFlag != "X", ct);
        if (gg is null) return (false, "未找到规格书");

        if (Same(gg.Status, MotorFlow.GgPendingManager))
        {
            if (!await HasRoleAsync(MotorFlow.RoleRdManager, empNo, ct))
                return (false, "您没有研发经理审批权限");

            var assignError = await _perm.CheckAssignedApproverAsync(
                gg.ProjectGuid, gg.VersionNo, empNo, final: false, ct);
            if (assignError is not null) return (false, assignError);

            if (!Same(targetStatus, MotorFlow.GgDraft) && !Same(targetStatus, MotorFlow.LxDraft))
                return (false, "退回目标不正确");
        }
        else if (Same(gg.Status, MotorFlow.GgDraft))
        {
            // 编写阶段的退回：工程师本人操作，且只能退到立项单 —— 规格书填不下去基本是立项单的信息不对
            var deny = await _perm.CheckEditorAsync(MotorFlow.DocGg, gg.ProjectGuid, empNo, ct);
            if (deny is not null) return (false, "只有本项目的研发工程师可以退回立项单");

            targetStatus = MotorFlow.LxDraft;
        }
        else
        {
            return (false, "当前状态不能退回");
        }

        var now = DateTime.Now;
        var fromStatus = gg.Status;   // 退回发生时的节点，写进 PrevStepCode
        var lx = await ResolveLxAsync(gg.SourceLxDocGuid, gg.ProjectGuid, gg.VersionNo, ct);

        // 找不到立项单就别退：否则节点回到 1，单据却还停在已提交，业务员登进来既进不去也改不了
        if (Same(targetStatus, MotorFlow.LxDraft) && lx is null)
            return (false, "未找到对应的立项单，无法退回立项单");

        // 立项单已经是草稿状态，说明上一次退回还没处理完，别重复写记录和待办
        if (Same(targetStatus, MotorFlow.LxDraft) && Same(lx?.Status, MotorFlow.LxDraft))
            return (false, "立项单已经退回给业务员，等待重填后重新提交");

        // ResolveLxAsync 内部全是 AsNoTracking，上面拿到的 lx 是游离副本：
        // 直接改它的 Status 不会写库，结果就是节点退回 1、单据却还是 LxSubmitted，
        // 业务员进来看到「立项单已完成」被锁死。退回立项单必须换成受跟踪的行再改。
        var trackedLx = lx is null
            ? null
            : await _db.MotorLx.FirstOrDefaultAsync(x => x.Id == lx.Id, ct);

        if (Same(targetStatus, MotorFlow.LxDraft))
        {
            // 退回立项单：立项单重新变草稿，规格书连同一起作废重走
            if (trackedLx is not null)
            {
                trackedLx.Status = MotorFlow.LxDraft;
                trackedLx.IsDraft = true;
                trackedLx.SubmitTime = null;
                trackedLx.UpdateTime = now;
                trackedLx.UpdatePersonCode = empNo;
            }
            gg.Status = MotorFlow.GgDraft;
        }
        else
        {
            gg.Status = MotorFlow.GgDraft;
        }

        gg.ApproverCode = empNo;
        gg.ApproveTime = now;
        gg.UpdateTime = now;
        gg.UpdatePersonCode = empNo;


        await CloseTodosAsync(gg.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, MotorFlow.TodoCanceled, empNo, ct);

        // 接手人 = 被退回到的那个节点的责任人（节点 1 业务员 / 节点 2 工程师）
        var (toEmpNo, toName, toRole) = await ResolveNodeOwnerAsync(
            gg.ProjectGuid, gg.VersionNo, trackedLx ?? lx, targetStatus, ct);

        // StepCode 记「退回到哪」而不是规格书自身的新状态：退回立项单时两者不同，
        // 记成 GgDraft 会让流程记录显示成「退回至 编写规格书」，立项单页也认不出这条退回。
        AddFlow(gg.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, gg.VersionNo, MotorFlow.ActionReject,
            targetStatus, empNo, toEmpNo, toRole,
            comment, MotorFlow.TodoPending, now, prevStepCode: fromStatus);

        // 退回就是节点回退：退回工程师 = 节点 3，退到立项单 = 节点 1
        var rejectTo = Same(targetStatus, MotorFlow.LxDraft) ? MotorNode.LxCreate : MotorNode.GgWrite;
        await MoveNodeAsync(gg.ProjectGuid, gg.VersionNo, rejectTo, now, empNo, ct);

        // 退回本身也记一行日志（状态=退回，节点名=「发生退回的节点 → 退回到的节点」）
        var fromNodeGg = Same(fromStatus, MotorFlow.GgPendingManager)
            ? MotorNode.GgApprove : MotorNode.GgWrite;
        await WriteRejectLogAsync(
            gg.ProjectGuid, gg.VersionNo, fromNodeGg, rejectTo, empNo, toEmpNo, comment, now, ct);

        await _db.SaveChangesAsync(ct);
        return (true, Same(targetStatus, MotorFlow.LxDraft) ? "已退回到立项单" : "已退回给工程师");
    }

    // ==================== 承认书 ====================

    /// <summary>
    /// 业务员编制完承认书，提交研发经理审批（第一级）。
    /// 默认沿用版本上已指派的研发经理（规格书审批时定的那个人）；
    /// 终审人（销售经理）在提交时自动取账号库里当前那位并写入版本，研发经理通过后自动流转过去，
    /// 不需要业务员在提交时再选一次。
    /// </summary>
    public async Task<(bool Ok, string Message)> SubmitCrAsync(
        string crDocGuid,
        string? empNo,
        string? empName,
        CancellationToken ct = default,
        string? managerEmpNo = null,
        string? managerName = null,
        string? finalEmpNo = null,
        string? finalName = null,
        string? comment = null)
    {
        var cr = await _db.MotorCr.FirstOrDefaultAsync(x => x.DocGuid == crDocGuid && x.DeleteFlag != "X", ct);
        if (cr is null) return (false, "未找到承认书");
        if (!Same(cr.Status, MotorFlow.CrDraft) && !string.IsNullOrWhiteSpace(cr.Status))
            return (false, "当前状态不能提交审批");

        var deny = await _perm.CheckEditorAsync(MotorFlow.DocCr, cr.ProjectGuid, empNo, ct);
        if (deny is not null) return (false, deny);

        var version = await EnsureVersionAsync(cr.ProjectGuid, cr.VersionNo, empNo, ct);

        // 传进来的经理优先，其次用版本上已经指派的那位
        var manager = !string.IsNullOrWhiteSpace(managerEmpNo)
            ? managerEmpNo.Trim()
            : (version.ManagerEmpNo ?? "").Trim();

        if (manager.Length > 0 && !string.IsNullOrWhiteSpace(managerEmpNo))
        {
            if (!await HasDirectoryRoleAsync(MotorFlow.RoleRdManager, manager, ct))
                return (false, "所选人员不是研发经理，请确认其账号角色维护正确");

            version.ManagerEmpNo = manager;
        }

        if (manager.Length == 0)
            return (false, "该版本还没有指派研发经理，请先在规格书提交审批时选定");

        // 终审人（销售经理）：提交时自动取当前唯一一位，研发经理通过后自动流转到他那里
        var final = (version.FinalEmpNo ?? "").Trim();
        if (final.Length == 0)
        {
            if (!string.IsNullOrWhiteSpace(finalEmpNo))
                final = finalEmpNo.Trim();

            if (final.Length == 0)
            {
                var (_, finalCandidates, _) = await _directory.QueryByRoleAsync(
                    MotorFlow.RoleFinalApprover, null, ct);
                var auto = finalCandidates.FirstOrDefault();
                if (auto is null)
                    return (false, "没有可用的销售经理账号，无法提交承认书审批");

                final = auto.EmpNo;
            }
            else
            {
                // finalName 已废弃，只存工号
            }

            if (!await HasDirectoryRoleAsync(MotorFlow.RoleFinalApprover, final, ct))
                return (false, "所选人员不是销售经理，请确认其账号角色维护正确");

            version.FinalEmpNo = final;
        }

        var now = DateTime.Now;
        cr.Status = MotorFlow.CrPendingManager;
        cr.IsDraft = false;
        cr.UpdateTime = now;
        cr.UpdatePersonCode = empNo;

        await CloseTodosAsync(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, MotorFlow.TodoDone, empNo, ct);

        AddFlow(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, cr.VersionNo, MotorFlow.ActionSubmit,
            MotorFlow.CrPendingManager, empNo,
            manager.Length == 0 ? null : manager,
            MotorFlow.RoleRdManager,
            comment ?? "承认书提交审批", MotorFlow.TodoPending, now);

        // 节点 9：等研发经理审批承认书（已指派就等指定人，没指派保持角色待办）
        await MoveNodeAsync(cr.ProjectGuid, cr.VersionNo, MotorNode.CrApprove, now, empNo, ct,
            pendingEmpNoOverride: manager.Length == 0 ? null : manager,
            pendingRoleOverride: MotorFlow.RoleRdManager,
            comment: comment);

        await _db.SaveChangesAsync(ct);
        return (true, "已提交研发经理审批");
    }

    /// <summary>审批承认书。第一级通过转蒋经理，第二级通过转业务员回传签字版。</summary>
    public async Task<(bool Ok, string Message)> ApproveCrAsync(
        string crDocGuid,
        string? empNo,
        string? empName,
        string? comment,
        CancellationToken ct = default)
    {
        var cr = await _db.MotorCr.FirstOrDefaultAsync(x => x.DocGuid == crDocGuid && x.DeleteFlag != "X", ct);
        if (cr is null) return (false, "未找到承认书");

        var now = DateTime.Now;
        var lx = await ResolveLxAsync(cr.SourceLxDocGuid, cr.ProjectGuid, cr.VersionNo, ct);

        if (Same(cr.Status, MotorFlow.CrPendingManager))
        {
            if (!await HasRoleAsync(MotorFlow.RoleRdManager, empNo, ct))
                return (false, "您没有研发经理审批权限");

            var assignError = await _perm.CheckAssignedApproverAsync(
                cr.ProjectGuid, cr.VersionNo, empNo, final: false, ct);
            if (assignError is not null) return (false, assignError);

            // 本版终审人由业务员提交承认书时选定，没指定就别往下放
            var versionForFinal = await EnsureVersionAsync(cr.ProjectGuid, cr.VersionNo, empNo, ct);
            var finalEmpNo = (versionForFinal.FinalEmpNo ?? "").Trim();
            if (finalEmpNo.Length == 0)
                return (false, "该版本还没有指定终审人，请退回业务员重新提交时选定");

            cr.Status = MotorFlow.CrPendingJiang;
            cr.ManagerCode = empNo;
            cr.ManagerApproveTime = now;

            await CloseTodosAsync(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, MotorFlow.TodoDone, empNo, ct);
            AddFlow(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, cr.VersionNo, MotorFlow.ActionApprove,
                MotorFlow.CrPendingJiang, empNo,
                finalEmpNo, null,
                comment ?? "研发经理通过（承认书）", MotorFlow.TodoPending, now);
        }
        else if (Same(cr.Status, MotorFlow.CrPendingJiang))
        {
            // 终审只认版本上被指派的销售经理，未指派一律不放行
            if (!await HasRoleAsync(MotorFlow.RoleFinalApprover, empNo, ct))
                return (false, "您没有销售经理终审权限");

            var assignError = await _perm.CheckAssignedApproverAsync(
                cr.ProjectGuid, cr.VersionNo, empNo, final: true, ct);
            if (assignError is not null) return (false, assignError);

            cr.Status = MotorFlow.CrPendingSign;
            cr.JiangCode = empNo;
            cr.JiangApproveTime = now;

            await CloseTodosAsync(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, MotorFlow.TodoDone, empNo, ct);
            AddFlow(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, cr.VersionNo, MotorFlow.ActionApprove,
                MotorFlow.CrPendingSign, empNo, lx?.CreatePersonCode, null,
                comment ?? "审批通过，待回传签字版", MotorFlow.TodoPending, now);
        }
        else
        {
            return (false, "当前状态不在审批环节");
        }

        cr.UpdateTime = now;
        cr.UpdatePersonCode = empNo;

        // 节点：研发经理通过 → 10 销售经理终审；终审通过 → 11 回传签字版
        await MoveNodeAsync(cr.ProjectGuid, cr.VersionNo,
            Same(cr.Status, MotorFlow.CrPendingJiang) ? MotorNode.CrFinal : MotorNode.CrSign,
            now, empNo, ct, comment: comment);

        await _db.SaveChangesAsync(ct);
        return (true, Same(cr.Status, MotorFlow.CrPendingJiang) ? "已通过，转销售经理终审" : "审批通过，已通知业务员回传签字版");
    }

    /// <summary>
    /// 退回承认书。退回节点由审批人选择：
    /// CrDraft = 退回业务员重编；CrPendingManager = 退回到研发经理那一级。
    /// </summary>
    public async Task<(bool Ok, string Message)> RejectCrAsync(
        string crDocGuid,
        string targetStatus,
        string? empNo,
        string? empName,
        string comment,
        CancellationToken ct = default)
    {
        var cr = await _db.MotorCr.FirstOrDefaultAsync(x => x.DocGuid == crDocGuid && x.DeleteFlag != "X", ct);
        if (cr is null) return (false, "未找到承认书");
        if (!Same(cr.Status, MotorFlow.CrPendingManager) && !Same(cr.Status, MotorFlow.CrPendingJiang))
            return (false, "当前状态不在审批环节");

        // 退回也是审批动作：角色 + 被指派的人，两个条件缺一不可
        var isFinalStage = Same(cr.Status, MotorFlow.CrPendingJiang);
        if (!await HasRoleAsync(isFinalStage ? MotorFlow.RoleFinalApprover : MotorFlow.RoleRdManager, empNo, ct))
            return (false, isFinalStage ? "您没有销售经理终审权限" : "您没有研发经理审批权限");

        var rejectDeny = await _perm.CheckAssignedApproverAsync(
            cr.ProjectGuid, cr.VersionNo, empNo, final: isFinalStage, ct);
        if (rejectDeny is not null) return (false, rejectDeny);

        var now = DateTime.Now;
        var fromStatus = cr.Status;   // 退回发生时的节点（研发经理审批 / 销售经理终审）
        var lx = await ResolveLxAsync(cr.SourceLxDocGuid, cr.ProjectGuid, cr.VersionNo, ct);

        // 接手人 = 被退回到的那个节点的责任人（节点 6 研发经理 / 节点 5 业务员）
        var (toEmpNo, toName, toRole) = await ResolveNodeOwnerAsync(
            cr.ProjectGuid, cr.VersionNo, lx, targetStatus, ct);

        if (Same(targetStatus, MotorFlow.CrPendingManager))
        {
            cr.Status = MotorFlow.CrPendingManager;
            AddFlow(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, cr.VersionNo, MotorFlow.ActionReject,
                MotorFlow.CrPendingManager, empNo, toEmpNo, toRole,
                comment, MotorFlow.TodoPending, now, prevStepCode: fromStatus);
        }
        else
        {
            cr.Status = MotorFlow.CrDraft;
            AddFlow(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, cr.VersionNo, MotorFlow.ActionReject,
                MotorFlow.CrDraft, empNo, toEmpNo, toRole,
                comment, MotorFlow.TodoPending, now, prevStepCode: fromStatus);
        }

        await CloseTodosAsync(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, MotorFlow.TodoCanceled, empNo, ct);

        // 同理，退回即节点回退：退回研发经理 = 节点 9，退回业务员重编 = 节点 7
        var rejectToCr = Same(targetStatus, MotorFlow.CrPendingManager) ? MotorNode.CrApprove : MotorNode.CrWrite;
        await MoveNodeAsync(cr.ProjectGuid, cr.VersionNo, rejectToCr, now, empNo, ct);

        // 退回本身也记一行日志（节点名带「从哪退到哪」）
        var fromNodeCr = Same(fromStatus, MotorFlow.CrPendingJiang)
            ? MotorNode.CrFinal : MotorNode.CrApprove;
        await WriteRejectLogAsync(
            cr.ProjectGuid, cr.VersionNo, fromNodeCr, rejectToCr, empNo, toEmpNo, comment, now, ct);

        cr.UpdateTime = now;
        cr.UpdatePersonCode = empNo;

        await _db.SaveChangesAsync(ct);
        return (true, Same(targetStatus, MotorFlow.CrPendingManager) ? "已退回研发经理" : "已退回业务员重编");
    }

    /// <summary>
    /// 通用退回：退回到当前节点之前的任意节点（例如工程师编写规格书时退回到「指派研发工程师」重选人）。
    /// 权限：只能是当前节点的待办人本人；审批节点还要有对应角色。
    /// 退回会重置目标节点之后的所有单据状态与审批痕迹，重走时从目标节点继续。
    /// </summary>
    public async Task<(bool Ok, string Message)> RejectToNodeAsync(
        string projectGuid,
        string versionNo,
        int toNode,
        string? empNo,
        string? empName,
        string? comment,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(versionNo))
            return (false, "缺少版本信息");
        if (!MotorNode.IsValid(toNode) || toNode == MotorNode.Archived)
            return (false, "退回目标节点无效");

        var version = await EnsureVersionAsync(projectGuid, versionNo, empNo, ct);
        if (Same(version.Status, MotorFlow.VersionArchived) || version.CurrentNode == MotorNode.Archived)
            return (false, "版本已归档，不能退回");

        var fromNode = MotorNode.Normalize(version.CurrentNode);
        if (toNode >= fromNode)
            return (false, "只能退回到当前节点之前的节点");

        // 权限：当前节点待办人本人；角色节点还要有对应角色，避免别人替审批人乱退
        var me = (empNo ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(version.PendingEmpNo) && !Same(version.PendingEmpNo, me))
            return (false, $"当前节点由 {version.PendingEmpNo} 处理，只有他能退回");
        if (string.IsNullOrWhiteSpace(version.PendingEmpNo)
            && !string.IsNullOrWhiteSpace(version.PendingRole)
            && !await HasRoleAsync(
                Same(version.PendingRole, MotorFlow.RoleFinalApprover)
                    ? MotorFlow.RoleFinalApprover
                    : MotorFlow.RoleRdManager,
                empNo, ct))
            return (false, "你没有当前节点的审批角色，不能退回");

        var lx = await _db.MotorLx.FirstOrDefaultAsync(
            x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo && x.DeleteFlag != "X", ct);
        var gg = await _db.MotorGg.FirstOrDefaultAsync(
            x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo && x.DeleteFlag != "X", ct);
        var cr = await _db.MotorCr.FirstOrDefaultAsync(
            x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo && x.DeleteFlag != "X", ct);
        var project = await _db.Projects.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid)
            .Select(x => new { x.OwnerEmpNo })
            .FirstOrDefaultAsync(ct);

        var now = DateTime.Now;
        var fromStatus = MotorNode.DocKind(fromNode) switch
        {
            var k when Same(k, MotorFlow.DocLx) => lx?.Status,
            var k when Same(k, MotorFlow.DocGg) => gg?.Status,
            _ => cr?.Status
        };

        ResetDocsForReject(toNode, lx, gg, cr, now, empNo);

        // 目标节点的接手人：工程师节点用立项单上的人，审批节点沿用版本已指派的人
        var (pendingEmp, pendingRole) = MotorNode.PendingOf(
            toNode, project?.OwnerEmpNo, lx?.EngineerCode, version.FinalEmpNo);
        if (toNode is MotorNode.GgApprove or MotorNode.CrApprove
            && !string.IsNullOrWhiteSpace(version.ManagerEmpNo))
        {
            pendingEmp = version.ManagerEmpNo.Trim();
            pendingRole = MotorFlow.RoleRdManager;
        }
        if (toNode == MotorNode.CrFinal && !string.IsNullOrWhiteSpace(version.FinalEmpNo))
            pendingEmp = version.FinalEmpNo.Trim();

        if (lx is not null)
            await CloseTodosAsync(projectGuid, MotorFlow.DocLx, lx.DocGuid, MotorFlow.TodoCanceled, empNo, ct);
        if (gg is not null)
            await CloseTodosAsync(projectGuid, MotorFlow.DocGg, gg.DocGuid, MotorFlow.TodoCanceled, empNo, ct);
        if (cr is not null)
            await CloseTodosAsync(projectGuid, MotorFlow.DocCr, cr.DocGuid, MotorFlow.TodoCanceled, empNo, ct);

        var fromDocGuid = MotorNode.DocKind(fromNode) switch
        {
            var k when Same(k, MotorFlow.DocLx) => lx?.DocGuid,
            var k when Same(k, MotorFlow.DocGg) => gg?.DocGuid,
            _ => cr?.DocGuid
        };
        AddFlow(projectGuid, MotorNode.DocKind(fromNode), fromDocGuid ?? "", versionNo,
            MotorFlow.ActionReject, RejectStepCode(toNode), empNo, pendingEmp, pendingRole,
            comment, MotorFlow.TodoPending, now, prevStepCode: fromStatus);

        await MoveNodeAsync(projectGuid, versionNo, toNode, now, empNo, ct,
            pendingEmpNoOverride: pendingEmp,
            pendingRoleOverride: pendingRole,
            engineerEmpNoOverride: lx?.EngineerCode);

        await WriteRejectLogAsync(
            projectGuid, versionNo, fromNode, toNode, empNo, pendingEmp, comment, now, ct);

        await _db.SaveChangesAsync(ct);
        return (true, $"已退回到「{MotorNode.RejectTitle(toNode)}」");
    }

    /// <summary>退回目标节点对应的单据状态，用来给立项单页识别「这一步刚被退回」。</summary>
    private static string RejectStepCode(int node) => node switch
    {
        <= MotorNode.AssignEngineer => MotorFlow.LxDraft,
        <= MotorNode.GgSubmit => MotorFlow.GgDraft,
        MotorNode.GgApprove => MotorFlow.GgPendingManager,
        MotorNode.GgSign => MotorFlow.GgPendingSign,
        <= MotorNode.CrSubmit => MotorFlow.CrDraft,
        MotorNode.CrApprove => MotorFlow.CrPendingManager,
        MotorNode.CrFinal => MotorFlow.CrPendingJiang,
        _ => MotorFlow.CrPendingSign
    };

    /// <summary>
    /// 按退回目标重置三张单据：目标节点之后的审批、签字全部作废，
    /// 让重走时从目标节点重新走一遍，不会带着旧的审批痕迹。
    /// </summary>
    private static void ResetDocsForReject(
        int toNode, MotorLxModel? lx, MotorGgModel? gg, MotorCrModel? cr,
        DateTime now, string? empNo)
    {
        if (toNode <= MotorNode.AssignEngineer && lx is not null)
        {
            lx.Status = MotorFlow.LxDraft;
            lx.IsDraft = true;
            lx.SubmitTime = null;
            lx.UpdateTime = now;
            lx.UpdatePersonCode = empNo;
        }

        if (toNode <= MotorNode.GgSign && gg is not null)
        {
            if (toNode <= MotorNode.GgWrite)
            {
                gg.Status = MotorFlow.GgDraft;
                gg.IsDraft = true;
            }
            else if (toNode == MotorNode.GgApprove)
            {
                gg.Status = MotorFlow.GgPendingManager;
                gg.IsDraft = false;
                gg.ApproverCode = null;
                gg.ApproveTime = null;
            }
            else
            {
                gg.Status = MotorFlow.GgPendingSign;
                gg.IsDraft = false;
            }
            if (toNode <= MotorNode.GgApprove)
            {
                gg.SignFileId = null;
                gg.SignUploadTime = null;
                gg.SignUploadPersonCode = null;
            }
            gg.UpdateTime = now;
            gg.UpdatePersonCode = empNo;
        }

        if (cr is null) return;
        if (toNode <= MotorNode.GgSign || toNode <= MotorNode.CrSubmit)
        {
            cr.Status = MotorFlow.CrDraft;
            cr.IsDraft = true;
            cr.ManagerCode = null;
            cr.ManagerApproveTime = null;
            cr.JiangCode = null;
            cr.JiangApproveTime = null;
            cr.SignFileId = null;
            cr.SignUploadTime = null;
            cr.SignUploadPersonCode = null;
        }
        else if (toNode == MotorNode.CrApprove)
        {
            cr.Status = MotorFlow.CrPendingManager;
            cr.IsDraft = false;
            cr.ManagerCode = null;
            cr.ManagerApproveTime = null;
            cr.JiangCode = null;
            cr.JiangApproveTime = null;
            cr.SignFileId = null;
            cr.SignUploadTime = null;
            cr.SignUploadPersonCode = null;
        }
        else if (toNode == MotorNode.CrFinal)
        {
            cr.Status = MotorFlow.CrPendingJiang;
            cr.IsDraft = false;
            cr.JiangCode = null;
            cr.JiangApproveTime = null;
            cr.SignFileId = null;
            cr.SignUploadTime = null;
            cr.SignUploadPersonCode = null;
        }
        else
        {
            cr.Status = MotorFlow.CrPendingSign;
            cr.IsDraft = false;
            cr.SignFileId = null;
            cr.SignUploadTime = null;
            cr.SignUploadPersonCode = null;
        }
        cr.UpdateTime = now;
        cr.UpdatePersonCode = empNo;
    }

    /// <summary>
    /// 「退回到哪个节点，就由谁接手」的唯一出处。
    ///
    /// 退回产生的待办人、流程记录里的相关人，一律从这里取，不允许在调用处写死某个岗位 ——
    /// 规格书退回给工程师时曾写死业务员，结果相关人和待办都挂错了人。
    /// 返回值：(工号, 姓名, 角色)。按角色找人的节点（研发经理 / 销售经理）同时给角色和指定人。
    /// </summary>
    private async Task<(string? Code, string? Name, string? Role)> ResolveNodeOwnerAsync(
        string projectGuid, string versionNo, MotorLxModel? lx, string? status, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(status)) return (null, null, null);

        var project = await _db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProjectGuid == projectGuid, ct);

        // 节点 1 / 4 / 5 / 8：业务员 —— 编写立项单、回传签字版、编制承认书、归档确认
        if (Same(status, MotorFlow.LxDraft)
            || Same(status, MotorFlow.CrDraft)
            || Same(status, MotorFlow.GgPendingSign)
            || Same(status, MotorFlow.CrPendingSign)
            || Same(status, MotorFlow.GgCompleted)
            || Same(status, MotorFlow.CrCompleted)
            || Same(status, MotorFlow.VersionReadyToArchive))
        {
            return (lx?.CreatePersonCode ?? project?.OwnerEmpNo,
                    null,
                    null);
        }

        // 节点 2：研发工程师 —— 编写 / 重填规格书（工程师只存在立项单上）
        if (Same(status, MotorFlow.LxSubmitted) || Same(status, MotorFlow.GgDraft))
        {
            return (lx?.EngineerCode,
                    null,
                    null);
        }

        var version = await _db.ProjectVersions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo, ct);

        // 节点 6：研发经理 —— 本版本指定的那位；没指定就只给角色
        if (Same(status, MotorFlow.GgPendingManager) || Same(status, MotorFlow.CrPendingManager))
        {
            if (!string.IsNullOrWhiteSpace(version?.ManagerEmpNo))
                return (version.ManagerEmpNo, null, MotorFlow.RoleRdManager);
            return (null, null, MotorFlow.RoleRdManager);
        }

        // 节点 7：销售经理终审
        if (Same(status, MotorFlow.CrPendingJiang))
        {
            if (!string.IsNullOrWhiteSpace(version?.FinalEmpNo))
                return (version.FinalEmpNo, null, MotorFlow.RoleFinalApprover);
            return (null, null, MotorFlow.RoleFinalApprover);
        }

        return (null, null, null);
    }

    // ==================== 签字版与归档 ====================

    /// <summary>
    /// 业务员回传签字版。规格书/承认书都走这里，上传完成即该步骤 complete。
    /// </summary>
    public async Task<(bool Ok, string Message)> UploadSignAsync(
        string docType,
        string docGuid,
        string fileId,
        string? fileName,
        string? empNo,
        string? empName,
        CancellationToken ct = default,
        string? comment = null)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return (false, "请先选择签字版文件");

        var now = DateTime.Now;

        if (Same(docType, MotorFlow.DocGg))
        {
            var gg = await _db.MotorGg.FirstOrDefaultAsync(x => x.DocGuid == docGuid && x.DeleteFlag != "X", ct);
            if (gg is null) return (false, "未找到规格书");

            // 已完成（已回传过）的允许业务员重新上传覆盖；其余状态不放行
            var isReplace = Same(gg.Status, MotorFlow.GgCompleted);
            if (!Same(gg.Status, MotorFlow.GgPendingSign) && !isReplace)
                return (false, "当前不是待回传签字版状态");

            var deny = await _perm.CheckEditorAsync(MotorFlow.DocLx, gg.ProjectGuid, empNo, ct);
            if (deny is not null) return (false, deny);

            var version = await EnsureVersionAsync(gg.ProjectGuid, gg.VersionNo, empNo, ct);
            if (Same(version.Status, MotorFlow.VersionArchived))
                return (false, "该版本已归档，签字版不能再更换");

            gg.SignFileId = fileId;
            gg.SignFileName = fileName;
            gg.SignUploadTime = now;
            gg.SignUploadPersonCode = empNo;
            gg.Status = MotorFlow.GgCompleted;
            gg.IsDraft = false;
            gg.UpdateTime = now;
            gg.UpdatePersonCode = empNo;


            if (isReplace)
            {
                // 重新上传：状态不变、待办早已关闭，只留一条替换记录方便追溯
                AddFlow(gg.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, gg.VersionNo, MotorFlow.ActionUploadSign,
                    MotorFlow.GgCompleted, empNo, "", null,
                    comment ?? "签字版已重新上传", MotorFlow.TodoDone, now);
                await _db.SaveChangesAsync(ct);
                return (true, "签字版已重新上传");
            }

            await CloseTodosAsync(gg.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, MotorFlow.TodoDone, empNo, ct);
            AddFlow(gg.ProjectGuid, MotorFlow.DocGg, gg.DocGuid, gg.VersionNo, MotorFlow.ActionUploadSign,
                MotorFlow.GgCompleted, empNo, "", null,
                comment ?? "签字版已回传", MotorFlow.TodoDone, now);

            // 节点 6 完成 → 节点 7：业务员开始编写承认书
            await MoveNodeAsync(gg.ProjectGuid, gg.VersionNo, MotorNode.CrWrite, now, empNo, ct,
                comment: comment);

            await TryMarkReadyToArchiveAsync(gg.ProjectGuid, gg.VersionNo, now, ct);
            await _db.SaveChangesAsync(ct);
            return (true, "签字版已上传，规格书步骤完成");
        }

        if (Same(docType, MotorFlow.DocCr))
        {
            var cr = await _db.MotorCr.FirstOrDefaultAsync(x => x.DocGuid == docGuid && x.DeleteFlag != "X", ct);
            if (cr is null) return (false, "未找到承认书");

            // 已完成（已回传过）的允许业务员重新上传覆盖；其余状态不放行
            var isReplace = Same(cr.Status, MotorFlow.CrCompleted);
            if (!Same(cr.Status, MotorFlow.CrPendingSign) && !isReplace)
                return (false, "当前不是待回传签字版状态");

            var deny = await _perm.CheckEditorAsync(MotorFlow.DocLx, cr.ProjectGuid, empNo, ct);
            if (deny is not null) return (false, deny);

            var version = await EnsureVersionAsync(cr.ProjectGuid, cr.VersionNo, empNo, ct);
            if (Same(version.Status, MotorFlow.VersionArchived))
                return (false, "该版本已归档，签字版不能再更换");

            cr.SignFileId = fileId;
            cr.SignFileName = fileName;
            cr.SignUploadTime = now;
            cr.SignUploadPersonCode = empNo;
            cr.Status = MotorFlow.CrCompleted;
            cr.IsDraft = false;
            cr.UpdateTime = now;
            cr.UpdatePersonCode = empNo;

            if (isReplace)
            {
                // 重新上传：状态不变、待办早已关闭，只留一条替换记录方便追溯
                AddFlow(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, cr.VersionNo, MotorFlow.ActionUploadSign,
                    MotorFlow.CrCompleted, empNo, "", null,
                    comment ?? "签字版已重新上传", MotorFlow.TodoDone, now);
                await _db.SaveChangesAsync(ct);
                return (true, "签字版已重新上传");
            }

            await CloseTodosAsync(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, MotorFlow.TodoDone, empNo, ct);
            AddFlow(cr.ProjectGuid, MotorFlow.DocCr, cr.DocGuid, cr.VersionNo, MotorFlow.ActionUploadSign,
                MotorFlow.CrCompleted, empNo, "", null,
                comment ?? "签字版已回传", MotorFlow.TodoDone, now);

            // 节点 11 完成 → 节点 12（待归档）
            await MoveNodeAsync(cr.ProjectGuid, cr.VersionNo, MotorNode.Archive, now, empNo, ct,
                comment: comment);

            await TryMarkReadyToArchiveAsync(cr.ProjectGuid, cr.VersionNo, now, ct);
            await _db.SaveChangesAsync(ct);
            return (true, "签字版已上传，承认书步骤完成");
        }

        return (false, "不支持的单据类型");
    }

    /// <summary>三份单据都完成后，业务员点归档：整版冻结。</summary>
    public async Task<(bool Ok, string Message)> ArchiveAsync(
        string projectGuid,
        string versionNo,
        string? empNo,
        string? empName,
        CancellationToken ct = default,
        string? comment = null)
    {
        var deny = await _perm.CheckEditorAsync(MotorFlow.DocLx, projectGuid, empNo, ct);
        if (deny is not null) return (false, deny);

        var lx = await _db.MotorLx.FirstOrDefaultAsync(
            x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo && x.DeleteFlag != "X", ct);
        var gg = await FindGgAsync(projectGuid, versionNo, ct);
        var cr = await FindCrAsync(projectGuid, versionNo, ct);

        if (lx is null) return (false, "未找到该版本的立项单");
        if (!MotorFlow.IsLxDone(lx.Status) || !MotorFlow.IsGgDone(gg?.Status) || !MotorFlow.IsCrDone(cr?.Status))
            return (false, "三份单据尚未全部完成，不能归档");

        var now = DateTime.Now;
        var version = await EnsureVersionAsync(projectGuid, versionNo, empNo, ct);
        if (Same(version.Status, MotorFlow.VersionArchived))
            return (false, "该版本已经归档");

        version.ArchivedTime = now;
        version.ArchivePersonCode = empNo;

        // 推进到终态（13）：节点 12（归档）置完成、写流程日志、待办清空、版本状态转已归档
        await MoveNodeAsync(projectGuid, versionNo, MotorNode.Archived, now, empNo, ct,
            comment: comment);

        AddFlow(projectGuid, MotorFlow.DocLx, lx.DocGuid, versionNo, MotorFlow.ActionArchive,
            MotorFlow.VersionArchived, empNo, "", null,
            comment ?? "项目归档", MotorFlow.TodoDone, now);

        await _db.SaveChangesAsync(ct);
        return (true, $"版本 {versionNo} 已归档，项目完成");
    }

    // ==================== 版本管理 ====================

    /// <summary>
    /// 新建版本：复制上一版本的三份单据，按 <paramref name="changeScope"/> 决定哪些重走。
    /// 不在范围里的单据直接沿用（状态保持完成），并在 InheritedFromVersion 记录来源版本。
    /// </summary>
    public async Task<(bool Ok, string Message, string? NewVersionNo)> CreateVersionAsync(
        string projectGuid,
        string baseVersionNo,
        string changeReason,
        IReadOnlyCollection<string> changeScope,
        string? empNo,
        string? empName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectGuid)) return (false, "缺少项目标识", null);
        if (changeScope.Count == 0) return (false, "请至少勾选一项要修改的单据", null);

        var deny = await _perm.CheckEditorAsync(MotorFlow.DocLx, projectGuid, empNo, ct);
        if (deny is not null) return (false, deny, null);

        var baseLx = await _db.MotorLx.FirstOrDefaultAsync(
            x => x.ProjectGuid == projectGuid && x.VersionNo == baseVersionNo && x.DeleteFlag != "X", ct);
        if (baseLx is null) return (false, "未找到基准版本", null);

        // 派生新版本就是重新走一遍流程，不必管基准版本当前停在哪个节点 / 是否还有在途审批：
        // 新版本是独立的一套单据与待办，原版本的在途事项留在原版本上，不阻塞这次修改。
        var baseGg = await FindGgAsync(projectGuid, baseVersionNo, ct);
        var baseCr = await FindCrAsync(projectGuid, baseVersionNo, ct);

        var versions = await _db.ProjectVersions.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid)
            .Select(x => x.VersionNo)
            .ToListAsync(ct);

        var next = NextVersionNo(versions);
        var now = DateTime.Now;
        var scope = changeScope.Select(x => x.Trim().ToUpperInvariant()).ToHashSet();

        var lxInScope = scope.Contains(MotorFlow.DocLx);
        var ggInScope = scope.Contains(MotorFlow.DocGg);
        var crInScope = scope.Contains(MotorFlow.DocCr);

        // 立项单：重走则回到草稿；否则沿用完成态
        var newLx = CloneLx(baseLx, next, now, empNo, empName);
        newLx.Status = lxInScope ? MotorFlow.LxDraft : baseLx.Status;
        newLx.IsDraft = lxInScope;
        newLx.SubmitTime = lxInScope ? null : baseLx.SubmitTime;
        if (!lxInScope)
        {
            newLx.EngineerCode = baseLx.EngineerCode;
        }
        _db.MotorLx.Add(newLx);

        if (baseGg is not null)
        {
            var newGg = CloneGg(baseGg, next, now, empNo, empName);
            newGg.SourceLxDocGuid = newLx.DocGuid;
            newGg.Status = ggInScope ? MotorFlow.GgDraft : baseGg.Status;
            newGg.IsDraft = ggInScope;
            newGg.InheritedFromVersion = ggInScope ? null : baseVersionNo;
            if (ggInScope)
            {
                newGg.ApproverCode = null;
                newGg.ApproveTime = null;
                newGg.SignFileId = null;
                newGg.SignFileName = null;
                newGg.SignUploadTime = null;
                newGg.SignUploadPersonCode = null;
            }
            _db.MotorGg.Add(newGg);

            if (baseCr is not null)
            {
                var newCr = CloneCr(baseCr, next, now, empNo, empName);
                newCr.SourceLxDocGuid = newLx.DocGuid;
                newCr.SourceGgDocGuid = newGg.DocGuid;
                newCr.Status = crInScope ? MotorFlow.CrDraft : baseCr.Status;
                newCr.IsDraft = crInScope;
                newCr.InheritedFromVersion = crInScope ? null : baseVersionNo;
                if (crInScope)
                {
                    newCr.ManagerCode = null;
                    newCr.ManagerApproveTime = null;
                    newCr.JiangCode = null;
                    newCr.JiangApproveTime = null;
                    newCr.SignFileId = null;
                    newCr.SignFileName = null;
                    newCr.SignUploadTime = null;
                    newCr.SignUploadPersonCode = null;
                }
                _db.MotorCr.Add(newCr);
            }
        }

        // 新版本从哪一步重新开始：勾了立项单 → 节点 1；只勾规格书 → 节点 3；
        // 只勾承认书 → 节点 7（规格书沿用，业务员直接编承认书）。
        var startNode = lxInScope ? MotorNode.LxCreate
            : ggInScope ? MotorNode.GgWrite
            : MotorNode.CrWrite;

        var newVersion = new ProjectVersionModel
        {
            ProjectGuid = projectGuid,
            VersionNo = next,
            Status = MotorFlow.VersionInProgress,
            BaseVersionNo = baseVersionNo,
            ChangeReason = changeReason,
            ChangeScope = string.Join(",", scope),
            CurrentNode = startNode,
            NodeEnterTime = now,
            NodeUpdateEmpNo = empNo,
            CreateTime = now,
            CreatePersonCode = empNo
        };
        _db.ProjectVersions.Add(newVersion);

        // 沿用的步骤在新版本里同样算完成：把基准版本对应节点的记录照搬过来，
        // 否则 V2 的流程记录会显示「节点 1/2 未完成」，与 CurrentNode 对不上。
        var baseVersion = await _db.ProjectVersions.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.VersionNo == baseVersionNo)
            .FirstOrDefaultAsync(ct);
        if (baseVersion is not null)
        {
            for (var node = MotorNode.LxCreate; node < startNode; node++)
            {
                if (!baseVersion.IsNodeDone(node)) continue;
                var (nodeEmp, nodeTime) = baseVersion.NodeRecord(node);
                newVersion.MarkNodeDone(node, nodeEmp, nodeTime ?? now);
            }
        }

        // 项目表把当前版本指针挪到新版本，列表默认就显示这一版
        var versionProject = await EnsureProjectAsync(projectGuid, empNo, ct);
        if (versionProject is not null)
        {
            versionProject.CurrentVersionNo = next;
            versionProject.UpdateTime = now;
            versionProject.UpdatePersonCode = empNo;
        }

        // 新版本从 startNode 开始等活儿：待办人和「进行中」状态一起落，
        // 否则列表上这一版会显示成「未开始」却没人处理。
        var (pendingEmp, pendingRole) = MotorNode.PendingOf(
            startNode, versionProject?.OwnerEmpNo ?? empNo, baseLx.EngineerCode);
        newVersion.PendingEmpNo = pendingEmp;
        newVersion.PendingRole = pendingRole;
        newVersion.SetNodeStatus(startNode, MotorFlow.NodeRunning);
        newVersion.SetNodeOwner(startNode, pendingEmp);
        if (startNode == MotorNode.LxCreate)
            newVersion.TouchNodeTime(MotorNode.LxCreate, now, empNo);

        // 沿用的步骤在新版本的日志里也补出来：V2 应当从第 1 步开始就能看到完整轨迹，
        // 而不是只看到「从哪一步重新开始」之后的行。
        var carrySeq = 0;
        for (var node = MotorNode.LxCreate; node < startNode; node++)
        {
            if (!newVersion.IsNodeDone(node)) continue;
            var (nodeEmp, nodeTime) = newVersion.NodeRecord(node);
            _db.ProjectFlowLogs.Add(new ProjectFlowLogModel
            {
                ProjectGuid = projectGuid,
                VersionNo = next,
                Seq = ++carrySeq,
                Node = node,
                NodeName = MotorNode.RecordTitle(node),
                OperatorCode = nodeEmp,
                RelatedCode = MotorNode.RelatedOf(
                    node, versionProject?.OwnerEmpNo ?? empNo, baseLx.EngineerCode,
                    newVersion.ManagerEmpNo, newVersion.FinalEmpNo),
                State = MotorFlow.LogSuccess,
                Remark = null,
                ActionTime = nodeTime ?? now,
                ChangeReason = changeReason,
                CreateTime = now
            });
        }

        AddFlow(projectGuid, MotorFlow.DocLx, newLx.DocGuid, next, MotorFlow.ActionSubmit,
            newLx.Status ?? MotorFlow.LxDraft, empNo, "", null,
            $"新建版本 {next}：{changeReason}", MotorFlow.TodoDone, now);

        await _db.SaveChangesAsync(ct);
        return (true, $"已创建版本 {next}", next);
    }

    /// <summary>版本列表，倒序（最新在前）。</summary>
    public Task<List<ProjectVersionModel>> GetVersionsAsync(string projectGuid, CancellationToken ct = default)
        => _db.ProjectVersions.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid)
            .OrderByDescending(x => x.Id)
            .ToListAsync(ct);

    /// <summary>项目当前版本：Id 最大且未归档的那一版；都归档了则取最新一版。</summary>
    public async Task<ProjectVersionModel?> GetCurrentVersionAsync(string projectGuid, CancellationToken ct = default)
    {
        var versions = await GetVersionsAsync(projectGuid, ct);
        return versions.FirstOrDefault(x => !Same(x.Status, MotorFlow.VersionArchived)) ?? versions.FirstOrDefault();
    }

    // ==================== 权限 ====================

    /// <summary>
    /// 是否具备某个审批角色。
    /// 角色唯一来源是 JcCommonApi（用户在 ZYQDManager 系统下的 Role），
    /// 不再读本地 T_ZYQD_ApprovalRole，也不再「没配人就放行」——
    /// 查不到角色一律视为无权限，避免上线期间任何人都能审批。
    /// 承认书终审额外要求「是本版指定的终审人」，见 <c>ApproveCrAsync</c>。
    /// </summary>
    public Task<bool> HasRoleAsync(string roleCode, string? empNo, CancellationToken ct = default)
        => _roles.HasRoleAsync(roleCode, empNo, ct);

    /// <summary>
    /// 校验「被指派的那个人」的角色：和选人弹窗同源，直读账号库。
    /// 普通业务员提交时未必有权限调账号中心的 users 详情接口，不能用 HasRoleAsync 校验他人。
    /// 当前登录人自己的审批权限仍走 HasRoleAsync（登录态里有角色）。
    /// </summary>
    private async Task<bool> HasDirectoryRoleAsync(string roleCode, string? empNo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(empNo)) return false;
        return await _directory.FindAsync(empNo.Trim(), roleCode, ct) is not null;
    }

    /// <summary>当前登录人是否具备某个审批角色（页面判断按钮显隐用）。</summary>
    public Task<bool> CurrentUserHasRoleAsync(string roleCode, CancellationToken ct = default)
        => _roles.CurrentUserHasRoleAsync(roleCode, ct);

    /// <summary>
    /// 取当前版本最近一次退回记录（含退回原因），给三张编辑页回显。
    /// 按整个版本查是刻意的：退回可能把规格书退回立项单，业务员要在立项单页看到同一条原因。
    /// 「这条退回还要不要显示」由页面判断：单据还停在被退回的那个节点才显示，处理完就自动收起。
    /// </summary>
    public Task<ApprovalFlowModel?> GetLatestRejectAsync(
        string? projectGuid, string? versionNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(versionNo))
            return Task.FromResult<ApprovalFlowModel?>(null);

        return _db.ApprovalFlows.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid!.Trim()
                        && x.DocVersion == versionNo!.Trim()
                        && x.Action == MotorFlow.ActionReject)
            .OrderByDescending(x => x.ActionTime)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// 审批角色清单。
    /// 成员不在本地维护（改由用户管理页写 JcCommonApi），这里只返回角色定义。
    /// </summary>
    public static IReadOnlyList<(string Code, string Name)> GetRoleDefinitions() => MotorFlow.Roles;

    // ==================== 内部辅助 ====================

    private Task<MotorGgModel?> FindGgAsync(string projectGuid, string versionNo, CancellationToken ct)
        => _db.MotorGg.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

    private Task<MotorCrModel?> FindCrAsync(string projectGuid, string versionNo, CancellationToken ct)
        => _db.MotorCr.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

    private Task<MotorLxModel?> FindLxByDocGuidAsync(string? docGuid, CancellationToken ct)
        => string.IsNullOrWhiteSpace(docGuid)
            ? Task.FromResult<MotorLxModel?>(null)
            : _db.MotorLx.AsNoTracking().FirstOrDefaultAsync(x => x.DocGuid == docGuid && x.DeleteFlag != "X", ct);

    /// <summary>
    /// 找单据来源的立项单：先按 DocGuid，找不到再按「项目 + 版本」兜底。
    /// 早期手工建的规格书/承认书可能没写 SourceLxDocGuid，
    /// 只靠外键会让退回时立项单状态改不到，节点变了但单据没退。
    /// </summary>
    private async Task<MotorLxModel?> ResolveLxAsync(
        string? docGuid, string projectGuid, string? versionNo, CancellationToken ct)
    {
        var lx = await FindLxByDocGuidAsync(docGuid, ct);
        if (lx is not null) return lx;
        if (string.IsNullOrWhiteSpace(projectGuid)) return null;

        if (!string.IsNullOrWhiteSpace(versionNo))
        {
            var vn = versionNo!.Trim();
            lx = await _db.MotorLx.AsNoTracking()
                .Where(x => x.ProjectGuid == projectGuid && x.VersionNo == vn && x.DeleteFlag != "X")
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(ct);
            if (lx is not null) return lx;
        }

        return await _db.MotorLx.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// 流程是不是停在「业务员重填立项单」这一步（节点 1）。
    /// 规格书被退回立项单后，单据状态仍是 GgDraft（那张纸自己没变），
    /// 只有版本表上的节点能说明它已经作废、不能再提交。
    /// </summary>
    private async Task<bool> IsWaitingLxAsync(string projectGuid, string? versionNo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(versionNo)) return false;

        var vn = versionNo!.Trim();
        var version = await _db.ProjectVersions.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.VersionNo == vn)
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        return version is not null
               && MotorNode.IsValid(version.CurrentNode)
               && MotorNode.Normalize(version.CurrentNode) == MotorNode.LxCreate;
    }

    /// <summary>
    /// 确保项目主表有对应记录。正常流程下项目行由立项单首次保存时创建，
    /// 这里只处理项目表上线前的老数据或缺行的异常情况。
    /// </summary>
    private async Task<ProjectModel?> EnsureProjectAsync(
        string projectGuid, string? empNo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectGuid)) return null;

        var project = await _db.Projects
            .FirstOrDefaultAsync(x => x.ProjectGuid == projectGuid, ct);
        if (project is not null) return project;

        var lx = await _db.MotorLx.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        project = new ProjectModel
        {
            ProjectGuid = projectGuid,
            ProductType = ProjectTypes.Motor,
            Customer = lx?.Customer,
            CustomerPartCode = lx?.CustomerPartCode,
            MotorModelSpec = lx?.MotorModelSpec,
            Description = lx?.Description,
            DemandPerson = lx?.DemandPerson,
            OwnerEmpNo = lx?.CreatePersonCode ?? empNo,
            CurrentVersionNo = lx?.VersionNo,
            Status = ProjectStatus.Active,
            CreateTime = lx?.CreateTime ?? DateTime.Now,
            CreatePersonCode = lx?.CreatePersonCode ?? empNo,
            UpdateTime = DateTime.Now,
            UpdatePersonCode = empNo,
            DeleteFlag = ""
        };
        _db.Projects.Add(project);
        return project;
    }

    private async Task<ProjectVersionModel> EnsureVersionAsync(
        string projectGuid, string versionNo, string? empNo, CancellationToken ct)
    {        var version = await _db.ProjectVersions
            .FirstOrDefaultAsync(x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo, ct);
        if (version is not null) return version;

        version = new ProjectVersionModel
        {
            ProjectGuid = projectGuid,
            VersionNo = versionNo,
            Status = MotorFlow.VersionInProgress,
            // 新建缺失行时默认落在节点 1，随后由 MoveNodeAsync 覆盖成真实节点；
            // 流程记录同步置为「进行中」，避免第一步显示未开始
            CurrentNode = MotorNode.LxCreate,
            Node1Status = MotorFlow.NodeRunning,
            Node1EmpNo = empNo,
            NodeEnterTime = DateTime.Now,
            CreateTime = DateTime.Now,
            CreatePersonCode = empNo
        };
        _db.ProjectVersions.Add(version);
        return version;
    }

    /// <summary>
    /// 把版本推进到指定节点，并写清「现在等谁」。
    ///
    /// 待办人默认由 <see cref="MotorNode.PendingOf"/> 按节点推（指定人 / 角色），
    /// 个别场景（刚提交立项单、项目表还没落工程师）用 override 显式指定。
    /// 节点没变且待办人没变时不更新时间，避免停留时长被无意义的重复调用冲掉。
    /// </summary>
    private async Task MoveNodeAsync(
        string projectGuid,
        string? versionNo,
        int node,
        DateTime now,
        string? empNo,
        CancellationToken ct,
        string? pendingEmpNoOverride = null,
        string? pendingRoleOverride = null,
        string? engineerEmpNoOverride = null,
        string? comment = null)
    {
        if (string.IsNullOrWhiteSpace(projectGuid)) return;

        var project = await _db.Projects.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid)
            .Select(x => new { x.OwnerEmpNo })
            .FirstOrDefaultAsync(ct);

        var target = MotorNode.Normalize(node);
        var vno = versionNo ?? MotorLxService.FirstVersionNo;

        var version = await EnsureVersionAsync(projectGuid, vno, empNo, ct);

        // 研发工程师只存在立项单上（项目表不再冗余工程师）。
        // 注意这里查的是数据库里的旧值：提交立项单时 EngineerCode 刚在内存里改过、
        // 还没 SaveChanges，AsNoTracking 查不到，所以调用方必须把本次选的人传进来覆盖，
        // 否则节点 2「指派研发工程师」的流程日志相关人会存成 NULL。
        var engineerCode = await _db.MotorLx.AsNoTracking()
            .Where(x => x.ProjectGuid == projectGuid && x.VersionNo == vno && x.DeleteFlag != "X")
            .OrderByDescending(x => x.Id)
            .Select(x => x.EngineerCode)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(engineerEmpNoOverride))
            engineerCode = engineerEmpNoOverride.Trim();

        // 终审人（节点 7）来自版本表：提交承认书时选定，换人只改数据不改代码
        var (pendingEmp, pendingRole) = MotorNode.PendingOf(
            target, project?.OwnerEmpNo, engineerCode, version.FinalEmpNo);

        if (!string.IsNullOrWhiteSpace(pendingEmpNoOverride)) pendingEmp = pendingEmpNoOverride.Trim();
        if (!string.IsNullOrWhiteSpace(pendingRoleOverride)) pendingRole = pendingRoleOverride.Trim();

        var samePending = string.Equals(version.PendingEmpNo ?? "", pendingEmp ?? "", StringComparison.OrdinalIgnoreCase)
                          && string.Equals(version.PendingRole ?? "", pendingRole ?? "", StringComparison.OrdinalIgnoreCase);
        if (version.CurrentNode == target && samePending) return;

        version.CurrentNode = target;
        version.PendingEmpNo = string.IsNullOrWhiteSpace(pendingEmp) ? null : pendingEmp;
        version.PendingRole = string.IsNullOrWhiteSpace(pendingRole) ? null : pendingRole;
        version.NodeEnterTime = now;
        version.NodeUpdateEmpNo = empNo;

        // 12 步留痕：target 之前的节点全部记为完成（跨步时 2/4/8 会一起补上），
        // target 及之后打回未开始，target 置进行中。
        var completed = ApplyNodeRecords(version, target, now, empNo, pendingEmp);

        // 备注只属于本次实际操作的那一步：completed 里 target 的前一个节点
        // （提交立项单=节点2 指派工程师、提交规格书=节点4、提交承认书=节点8）。
        // 一并补齐的前序节点（创建立项单 / 编写规格书 / 编写承认书）不写备注，
        // 否则用户在一个弹窗填的备注会重复出现在同一批次的所有日志行上。
        var remarkNode = completed.Count > 0 ? completed[^1] : target - 1;
        var remark = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        // 每个刚完成的节点往流程日志表追加一行（操作人 / 相关人 / 时间都取落库值）
        if (completed.Count > 0)
        {
            var logs = new List<ProjectFlowLogModel>();
            var seq = await _db.ProjectFlowLogs
                .Where(x => x.ProjectGuid == projectGuid && x.VersionNo == vno)
                .MaxAsync(x => (int?)x.Seq, ct) ?? 0;

            foreach (var doneNode in completed)
            {
                var (opEmp, doneTime) = version.NodeRecord(doneNode);
                seq++;
                logs.Add(new ProjectFlowLogModel
                {
                    ProjectGuid = projectGuid,
                    VersionNo = vno,
                    Seq = seq,
                    Node = doneNode,
                    NodeName = MotorNode.RecordTitle(doneNode),
                    OperatorCode = string.IsNullOrWhiteSpace(opEmp) ? empNo : opEmp,
                    RelatedCode = MotorNode.RelatedOf(
                        doneNode, project?.OwnerEmpNo, engineerCode, version.ManagerEmpNo, version.FinalEmpNo),
                    State = MotorFlow.LogSuccess,
                    Remark = doneNode == remarkNode ? remark : null,
                    ActionTime = doneTime ?? now,
                    ChangeReason = version.ChangeReason,
                    CreateTime = now
                });
            }
            _db.ProjectFlowLogs.AddRange(logs);
        }

        if (target == MotorNode.Archived)
            version.Status = MotorFlow.VersionArchived;
    }

    /// <summary>
    /// 维护 12 步留痕（Node{n}EmpNo / Node{n}Status / Node{n}DoneTime），返回本次新完成的节点。
    ///
    /// 规律：流程进入节点 target，说明 target 之前的节点都已经做完 ——
    /// 逐个 MarkNodeDone（已完成的不覆盖，所以退回重走不会改掉首次时间）。
    /// 退回是反向移动：target 及之后打回 -1，target 之前的记录原样保留，
    /// 这样记录永远和 CurrentNode 对得上，不会留下「已完成」的假痕迹。
    ///
    /// 跨步很常见：提交立项单从 1 直接到 3，节点 2（指派研发工程师）随之完成；
    /// 提交规格书从 3 到 5、提交承认书从 7 到 9 同理。
    /// </summary>
    private static List<int> ApplyNodeRecords(
        ProjectVersionModel version, int target, DateTime now, string? empNo, string? pendingEmpNo)
    {
        var completed = new List<int>();

        // target 之前全部记完成（跨步一次性补齐 2/4/8 这类伴随节点）
        for (var node = 1; node < target; node++)
        {
            if (!version.IsNodeDone(node)) completed.Add(node);
            version.MarkNodeDone(node, empNo, now);
        }

        // target 及之后一律打回未开始（含负责人与完成时间）
        for (var node = target; node <= MotorNode.LastNode; node++)
            version.ClearNodeDone(node);

        if (target <= MotorNode.LastNode)
        {
            version.SetNodeStatus(target, MotorFlow.NodeRunning);
            version.SetNodeOwner(target, pendingEmpNo);
        }

        return completed;
    }

    /// <summary>
    /// 往流程日志追加一条「退回」记录。
    /// 退回不删旧行，只往后加一行，这样「退回 → 重走」的完整历史都留得住。
    /// Node 记「在哪一步点的退回」而不是退回目标；节点名写成「发生退回的节点 → 退回到的节点」，
    /// 例如「编写规格书 → 编写立项单」，流程记录表直接显示，不用猜发生了什么。
    /// </summary>
    private async Task WriteRejectLogAsync(
        string projectGuid, string versionNo, int fromNode, int toNode,
        string? operatorCode, string? relatedCode, string? reason, DateTime now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(versionNo)) return;

        var seq = await _db.ProjectFlowLogs
            .Where(x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo)
            .MaxAsync(x => (int?)x.Seq, ct) ?? 0;

        var version = await _db.ProjectVersions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo, ct);

        _db.ProjectFlowLogs.Add(new ProjectFlowLogModel
        {
            ProjectGuid = projectGuid,
            VersionNo = versionNo,
            Seq = seq + 1,
            Node = fromNode,
            NodeName = $"{MotorNode.RecordTitle(fromNode)} → {MotorNode.RejectTitle(toNode)}",
            OperatorCode = operatorCode,
            RelatedCode = relatedCode,
            State = MotorFlow.LogReject,
            Remark = string.IsNullOrWhiteSpace(reason) ? "退回" : reason.Trim(),
            ActionTime = now,
            ChangeReason = version?.ChangeReason,
            CreateTime = now
        });
    }

    /// <summary>三份都完成时把版本标记为待归档。</summary>
    private async Task TryMarkReadyToArchiveAsync(string projectGuid, string versionNo, DateTime now, CancellationToken ct)
    {
        var lx = await _db.MotorLx.AsNoTracking().FirstOrDefaultAsync(
            x => x.ProjectGuid == projectGuid && x.VersionNo == versionNo && x.DeleteFlag != "X", ct);
        var gg = await FindGgAsync(projectGuid, versionNo, ct);
        var cr = await FindCrAsync(projectGuid, versionNo, ct);

        if (!MotorFlow.IsLxDone(lx?.Status) || !MotorFlow.IsGgDone(gg?.Status) || !MotorFlow.IsCrDone(cr?.Status))
            return;

        var version = await EnsureVersionAsync(projectGuid, versionNo, null, ct);
        if (Same(version.Status, MotorFlow.VersionArchived)) return;

        version.Status = MotorFlow.VersionReadyToArchive;
    }

    /// <summary>关闭某张单据上还挂着的待办。</summary>
    private async Task CloseTodosAsync(
        string projectGuid, string docType, string docGuid, string todoStatus, string? empNo, CancellationToken ct)
    {
        var pendings = await _db.ApprovalFlows
            .Where(x => x.ProjectGuid == projectGuid
                        && x.DocType == docType
                        && x.DocGuid == docGuid
                        && x.TodoStatus == MotorFlow.TodoPending)
            .ToListAsync(ct);

        var now = DateTime.Now;
        foreach (var item in pendings)
        {
            item.TodoStatus = todoStatus;
            item.DoneTime = now;
        }
    }

    private void AddFlow(
        string projectGuid, string docType, string docGuid, string? versionNo,
        string action, string stepCode,
        string? fromCode,
        string? toCode, string? toRole,
        string? comment, string todoStatus, DateTime now,
        string? prevStepCode = null)
    {
        _db.ApprovalFlows.Add(new ApprovalFlowModel
        {
            ProjectGuid = projectGuid,
            ProductType = "电机",
            DocType = docType,
            DocGuid = docGuid,
            DocVersion = versionNo,
            StepCode = stepCode,
            PrevStepCode = prevStepCode,
            Action = action,
            FromUserCode = fromCode ?? "",
            ToUserCode = toCode ?? "",
            ToRole = toRole,
            TodoStatus = todoStatus,
            FlowComment = comment,
            ActionTime = now,
            DoneTime = Same(todoStatus, MotorFlow.TodoPending) ? null : now,
            CreateTime = now
        });
    }

    private static MotorLxModel CloneLx(MotorLxModel src, string versionNo, DateTime now, string? empNo, string? empName)
    {
        var copy = new MotorLxModel();
        CopyScalars(src, copy);
        copy.Id = 0;
        copy.ProjectGuid = src.ProjectGuid;
        copy.DocGuid = Guid.NewGuid().ToString("N");
        copy.VersionNo = versionNo;
        copy.CreateTime = now;
        copy.CreatePersonCode = empNo;
        copy.UpdateTime = now;
        copy.UpdatePersonCode = empNo;
        copy.DeleteFlag = "";
        copy.DeleteTime = null;
        copy.DeletePersonCode = null;
        return copy;
    }

    private static MotorGgModel CloneGg(MotorGgModel src, string versionNo, DateTime now, string? empNo, string? empName)
    {
        var copy = new MotorGgModel();
        CopyScalars(src, copy);
        copy.Id = 0;
        copy.ProjectGuid = src.ProjectGuid;
        copy.DocGuid = Guid.NewGuid().ToString("N");
        copy.VersionNo = versionNo;
        copy.CreateTime = now;
        copy.CreatePersonCode = empNo;
        copy.UpdateTime = now;
        copy.UpdatePersonCode = empNo;
        copy.DeleteFlag = "";
        copy.DeleteTime = null;
        copy.DeletePersonCode = null;
        return copy;
    }

    private static MotorCrModel CloneCr(MotorCrModel src, string versionNo, DateTime now, string? empNo, string? empName)
    {
        var copy = new MotorCrModel();
        CopyScalars(src, copy);
        copy.Id = 0;
        copy.ProjectGuid = src.ProjectGuid;
        copy.DocGuid = Guid.NewGuid().ToString("N");
        copy.VersionNo = versionNo;
        copy.CreateTime = now;
        copy.CreatePersonCode = empNo;
        copy.UpdateTime = now;
        copy.UpdatePersonCode = empNo;
        copy.DeleteFlag = "";
        copy.DeleteTime = null;
        copy.DeletePersonCode = null;
        return copy;
    }

    /// <summary>按同名属性把业务字段从源单据拷到副本，Id / 审计 / 版本字段随后再覆盖。</summary>
    private static void CopyScalars<T>(T src, T dest)
    {
        foreach (var p in typeof(T).GetProperties())
        {
            if (!p.CanRead || !p.CanWrite) continue;
            var t = p.PropertyType;
            if (!t.IsValueType && t != typeof(string)) continue;
            p.SetValue(dest, p.GetValue(src));
        }
    }

    /// <summary>V1 → V2，取现有最大编号 +1。</summary>
    private static string NextVersionNo(IEnumerable<string> existing)
    {
        var max = 0;
        foreach (var v in existing)
        {
            var s = (v ?? "").Trim();
            if (s.StartsWith('V') || s.StartsWith('v'))
                s = s[1..];
            if (int.TryParse(s, out var n) && n > max) max = n;
        }
        return "V" + (max + 1);
    }

    private static bool Same(string? a, string b)
        => string.Equals(a?.Trim(), b, StringComparison.OrdinalIgnoreCase);
}
