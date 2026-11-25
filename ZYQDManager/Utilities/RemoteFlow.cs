namespace ZYQDManager.Utilities;

/// <summary>
/// 遥控器项目流程状态机：立项单 → 规格书 → 承认书 → 归档。
///
/// 设计要点：
/// 1. 状态码存在各单据的 Status 列，节点即状态，不需要额外的状态表；
/// 2. 退回不新增状态，直接写回目标节点（退回人自选层级）；
/// 3. 每次流转都往 T_ZYQD_ApprovalFlow 落一条记录，既当审批历史也当待办队列；
/// 4. 版本内「立项单 + 规格书 + 承认书」用 ProjectGuid + VersionNo 关联。
/// </summary>
public static class RemoteFlow
{
    // ---------- 单据类型（对应 ApprovalFlow.DocType）----------
    public const string DocLx = "LX";
    public const string DocGg = "GG";
    public const string DocCr = "CR";

    // ---------- 立项单 ----------
    /// <summary>业务员编辑中。提交后即离开此状态，立项单步骤完成。</summary>
    public const string LxDraft = "LxDraft";
    /// <summary>已提交到研发工程师，立项单步骤完成。</summary>
    public const string LxSubmitted = "LxSubmitted";

    // ---------- 规格书 ----------
    /// <summary>研发工程师填写中。</summary>
    public const string GgDraft = "GgDraft";
    /// <summary>工程师已提交，待研发经理审批。</summary>
    public const string GgPendingManager = "GgPendingManager";
    /// <summary>研发经理已通过，待业务员回传签字版。</summary>
    public const string GgPendingSign = "GgPendingSign";
    /// <summary>签字版已上传，规格书步骤完成。</summary>
    public const string GgCompleted = "GgCompleted";

    // ---------- 承认书 ----------
    /// <summary>业务员编制中。</summary>
    public const string CrDraft = "CrDraft";
    /// <summary>待研发经理审批。</summary>
    public const string CrPendingManager = "CrPendingManager";
    /// <summary>
    /// 研发经理已通过，待终审（销售经理）。
    /// 状态码沿用历史值 CrPendingJiang，库里已有数据不必迁移，语义已改为「终审中」。
    /// </summary>
    public const string CrPendingJiang = "CrPendingJiang";
    /// <summary>两级审批均已通过，待业务员回传签字版。</summary>
    public const string CrPendingSign = "CrPendingSign";
    /// <summary>签字版已上传，承认书步骤完成。</summary>
    public const string CrCompleted = "CrCompleted";

    // ---------- 版本状态 ----------
    /// <summary>进行中：四步未全部完成。</summary>
    public const string VersionInProgress = "InProgress";
    /// <summary>三份单据均完成，等待业务员归档。</summary>
    public const string VersionReadyToArchive = "ReadyToArchive";
    /// <summary>已归档：整版冻结只读。</summary>
    public const string VersionArchived = "Archived";

    // ---------- 审批角色 ----------
    /// <summary>
    /// 研发经理。直接复用 JcCommonApi 中 ZYQDManager 系统的角色码，
    /// 角色成员由用户管理页维护，流程侧不再单独存一份名单。
    /// </summary>
    public const string RoleRdManager = AppRoles.RdManager;

    /// <summary>
    /// 承认书终审角色：销售经理。成员由用户管理页维护（JcCommonApi 角色码 sales_manager），
    /// 具体审批到谁，由版本表上的 FinalEmpNo 决定 —— 提交承认书时选定，不再写死某个人。
    /// </summary>
    public const string RoleFinalApprover = AppRoles.SalesManager;

    /// <summary>
    /// 历史角色码：老审批流水里存的是 JiangManager（固定工号 105214 那位），
    /// 只用于把旧记录展示成「销售经理」，新流程不再产生这个值。
    /// </summary>
    public const string LegacyJiangRole = "JiangManager";

    public static readonly IReadOnlyList<(string Code, string Name)> Roles =
    [
        (RoleRdManager, "研发经理"),
        (RoleFinalApprover, "销售经理")
    ];

    public static string RoleName(string? roleCode)
    {
        // 老数据的审批流水里存的还是 RdManager / JiangManager，一并认
        if (Same(roleCode, RoleRdManager) || Same(roleCode, "RdManager")) return "研发经理";
        if (Same(roleCode, RoleFinalApprover) || Same(roleCode, LegacyJiangRole)) return "销售经理";

        foreach (var (code, name) in Roles)
        {
            if (Same(code, roleCode)) return name;
        }
        return roleCode ?? "";
    }

    // ---------- 流转动作 ----------
    public const string ActionSubmit = "Submit";
    public const string ActionApprove = "Approve";
    public const string ActionReject = "Reject";
    public const string ActionUploadSign = "UploadSign";
    public const string ActionArchive = "Archive";

    // ---------- 流程步骤状态（Node{n}Status）----------
    /// <summary>还没轮到这一步。</summary>
    public const int NodeIdle = -1;
    /// <summary>轮到这一步了，还没做完（负责人可能已指派，也可能还是角色待办）。</summary>
    public const int NodeRunning = 0;
    /// <summary>这一步已做完，DoneTime 一定有值。</summary>
    public const int NodeFinished = 1;

    public static string NodeStateName(int status) => status switch
    {
        >= NodeFinished => "已完成",
        NodeRunning => "进行中",
        _ => "未开始"
    };

    // ---------- 待办状态 ----------
    public const string TodoPending = "Pending";
    public const string TodoDone = "Done";
    public const string TodoCanceled = "Canceled";

    // ---------- 流程日志状态（ProjectFlowLog.State）----------
    /// <summary>这一步正常完成。</summary>
    public const string LogSuccess = "成功";
    /// <summary>这一步被退回（退回本身也占一行，原因存 Remark）。</summary>
    public const string LogReject = "退回";
    /// <summary>编写类环节正在编辑（保存草稿时刷新，提交后改成功）。</summary>
    public const string LogRunning = "进行中";

    // ---------- 状态中文名 ----------
    private static readonly Dictionary<string, string> StatusNames = new(StringComparer.OrdinalIgnoreCase)
    {
        [LxDraft] = "立项单草稿",
        [LxSubmitted] = "立项单已完成",
        [GgDraft] = "规格书填写中",
        [GgPendingManager] = "待研发经理审批",
        [GgPendingSign] = "待回传签字版规格书",
        [GgCompleted] = "规格书已完成",
        [CrDraft] = "承认书编制中",
        [CrPendingManager] = "待研发经理审批",
        [CrPendingJiang] = "待销售经理终审",
        [CrPendingSign] = "待回传签字版承认书",
        [CrCompleted] = "承认书已完成"
    };

    public static string StatusName(string? status)
        => StatusNames.TryGetValue((status ?? "").Trim(), out var name) ? name : (status ?? "").Trim();

    /// <summary>
    /// 退回记录里「从哪个节点退回」的文案：把来源状态码翻成动作口径（编写规格书 / 研发经理审批规格书…），
    /// 与流程日志「编写规格书 → 编写立项单」保持同一套说法。空值兜底为「立项单编写」。
    /// </summary>
    public static string RejectFromTitle(string? status) => (status ?? "").Trim() switch
    {
        LxDraft => "编写立项单",
        LxSubmitted => "指派研发工程师",
        GgDraft => "编写规格书",
        GgPendingManager => "研发经理审批规格书",
        GgPendingSign => "回传签字版规格书",
        GgCompleted => "规格书审批完成",
        CrDraft => "编写承认书",
        CrPendingManager => "研发经理审批承认书",
        CrPendingJiang => "销售经理终审承认书",
        CrPendingSign => "回传签字版承认书",
        CrCompleted => "承认书审批完成",
        _ => "立项单编写"
    };

    /// <summary>
    /// 整版是否已归档。以版本状态为主，节点兜底 ——
    /// 历史数据里出现过版本 Status 没回写、只有 CurrentNode=Archived 的情况。
    /// </summary>
    public static bool IsVersionArchived(string? versionStatus, int currentNode)
        => Same(versionStatus, VersionArchived) || RemoteNode.Normalize(currentNode) == RemoteNode.Archived;

    /// <summary>
    /// 单据页顶部状态标签的文案口径：归档优先，其余才显示单据自身状态。
    /// 这里刻意不用流程节点文案（例如「待工程师填写规格书」），
    /// 否则流程已经走完、标签却还挂着一个中间待办，看着像卡住了。
    /// </summary>
    public static string SheetBadge(string? docStatus, string? versionStatus, int currentNode)
        => IsVersionArchived(versionStatus, currentNode) ? "已归档" : StatusName(docStatus);

    // ---------- 阶段（对应列表页 stepper 的四个圆点）----------
    public const int StageLx = 0;
    public const int StageGg = 1;
    public const int StageCr = 2;
    public const int StageArchive = 3;

    public static readonly IReadOnlyList<string> StageTitles = ["立项单", "规格书", "承认书", "归档"];

    public static string StageTitle(int stage)
        => StageTitles[Math.Clamp(stage, 0, StageTitles.Count - 1)];

    // ---------- 步骤完成标记 ----------
    public static bool IsLxDone(string? status) => Same(status, LxSubmitted);

    public static bool IsGgDone(string? status) => Same(status, GgCompleted);

    public static bool IsCrDone(string? status) => Same(status, CrCompleted);

    /// <summary>该状态是否还挂着待办（用于判断能否新建版本）。</summary>
    public static bool IsPending(string? status)
        => Same(status, GgPendingManager)
           || Same(status, GgPendingSign)
           || Same(status, CrPendingManager)
           || Same(status, CrPendingJiang)
           || Same(status, CrPendingSign);

    /// <summary>
    /// 当前节点由谁负责、打开哪份单据，统一在 <see cref="RemoteNode"/> 里，
    /// 这里只保留单据状态自身的语义（锁定 / 完成 / 在途），不再推导流程节点。
    /// </summary>

    /// <summary>状态码比较：忽略大小写与首尾空格（库里历史数据大小写不统一）。</summary>
    public static bool Same(string? a, string? b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}

