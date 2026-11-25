namespace ZYQDManager.Utilities;

/// <summary>
/// 遥控器项目流程节点：整条流程固定 12 步 + 归档终态，节点号存在
/// T_ZYQD_ProjectVersion.CurrentNode 上，是「现在走到第几步、在等谁」的唯一权威。
///
/// 与单据 Status 的分工：
///   - CurrentNode 决定流程推进、入口跳转、我的待办；
///   - 单据 Status 只表示「这张纸自身锁不锁」，不再参与流程判断。
/// 每次流转必须在同一个 SaveChanges 里同时写节点和单据状态。
///
/// 12 步（业务口径，对应流程日志表）：
///   1  创建立项单          业务员
///   2  指派研发工程师      业务员 → 研发工程师
///   3  编写规格书          研发工程师
///   4  提交审核（规格书）  研发工程师 → 研发经理
///   5  研发经理审核（规格书）  研发经理 → 业务员
///   6  回传签名版规格书    业务员
///   7  编写承认书          业务员
///   8  提交审核（承认书）  业务员 → 研发经理
///   9  研发经理审核（承认书）  研发经理 → 销售经理
///   10 销售经理审核（承认书）  销售经理 → 业务员
///   11 回传签名版承认书    业务员
///   12 归档                业务员
///   13 已归档（终态，无待办）
///
/// 注意：2/4/8 是「提交时的伴随动作」，与上一个节点在同一次操作里完成，
/// 流程不会停在这三个节点上（CurrentNode 不会等于 2/4/8），
/// 但状态列会记成已完成，流程日志里也各占一行。
/// </summary>
public static class RemoteNode
{
    /// <summary>未开始（历史数据缺省值，读取时按节点 1 处理）。</summary>
    public const int None = 0;

    /// <summary>1 创建立项单（业务员填立项单）。</summary>
    public const int LxCreate = 1;
    /// <summary>2 指派研发工程师（提交立项单时一并完成）。</summary>
    public const int AssignEngineer = 2;
    /// <summary>3 编写规格书（研发工程师）。</summary>
    public const int GgWrite = 3;
    /// <summary>4 提交审核（规格书）（工程师提交审批时一并完成）。</summary>
    public const int GgSubmit = 4;
    /// <summary>5 研发经理审核（规格书）。</summary>
    public const int GgApprove = 5;
    /// <summary>6 回传签名版规格书（业务员）。</summary>
    public const int GgSign = 6;
    /// <summary>7 编写承认书（业务员）。</summary>
    public const int CrWrite = 7;
    /// <summary>8 提交审核（承认书）（业务员提交审批时一并完成）。</summary>
    public const int CrSubmit = 8;
    /// <summary>9 研发经理审核（承认书）。</summary>
    public const int CrApprove = 9;
    /// <summary>10 销售经理审核（承认书）。</summary>
    public const int CrFinal = 10;
    /// <summary>11 回传签名版承认书（业务员）。</summary>
    public const int CrSign = 11;
    /// <summary>12 归档（业务员）。</summary>
    public const int Archive = 12;

    /// <summary>已归档：整版冻结，没有待办人。</summary>
    public const int Archived = 13;

    /// <summary>流程节点总数（1~12，不含归档终态）。</summary>
    public const int LastNode = Archive;

    /// <summary>节点中文名，用「待谁做什么」的口径写，列表阶段提示直接取这个。</summary>
    public static readonly IReadOnlyList<(int Node, string Title)> All =
    [
        (LxCreate, "立项单编写中"),
        (AssignEngineer, "待指派研发工程师"),
        (GgWrite, "待研发工程师编写规格书"),
        (GgSubmit, "待提交规格书审批"),
        (GgApprove, "待研发经理审批规格书"),
        (GgSign, "待回传签字版规格书"),
        (CrWrite, "待业务员编写承认书"),
        (CrSubmit, "待提交承认书审批"),
        (CrApprove, "待研发经理审批承认书"),
        (CrFinal, "待销售经理终审承认书"),
        (CrSign, "待回传签字版承认书"),
        (Archive, "待归档"),
        (Archived, "已归档")
    ];

    public static string Title(int node)
    {
        var target = Normalize(node);
        foreach (var (n, title) in All)
        {
            if (n == target) return title;
        }
        return "立项单编写中";
    }

    /// <summary>
    /// 「流程记录」用的环节名：写的是这件事本身，而不是「在等谁做」。
    /// 与 <see cref="Title"/> 刻意分开，避免记录表里出现「待…」这种时态。
    /// </summary>
    public static readonly IReadOnlyList<(int Node, string Title)> RecordTitles =
    [
        (LxCreate, "创建立项单"),
        (AssignEngineer, "指派研发工程师"),
        (GgWrite, "编写规格书"),
        (GgSubmit, "提交审核（规格书）"),
        (GgApprove, "研发经理审核（规格书）"),
        (GgSign, "回传签名版规格书"),
        (CrWrite, "编写承认书"),
        (CrSubmit, "提交审核（承认书）"),
        (CrApprove, "研发经理审核（承认书）"),
        (CrFinal, "销售经理审核（承认书）"),
        (CrSign, "回传签名版承认书"),
        (Archive, "归档")
    ];

    /// <summary>
    /// 单据页顶部标签用的「当前步骤」文案：只描述流程停在哪一步。
    /// 2/4/8 是伴随动作，停不到那里，映射到上一节点即可。
    /// </summary>
    public static string StepName(int node, bool crSheet = false)
    {
        var target = Normalize(node);
        return target switch
        {
            LxCreate or AssignEngineer => "立项单编写中",
            GgWrite or GgSubmit => "规格书编写中",
            GgApprove => "规格书审批中",
            GgSign => "待回传签字版规格书",
            CrWrite or CrSubmit => "承认书编写中",
            CrApprove => "承认书审批中",
            CrFinal => "承认书终审中",
            CrSign => "待回传签字版承认书",
            Archive => "待归档",
            _ => "已归档"
        };
    }

    public static string RecordTitle(int node)
    {
        var target = Normalize(node);
        foreach (var (n, title) in RecordTitles)
        {
            if (n == target) return title;
        }
        return $"流程 {target}";
    }

    /// <summary>
    /// 退回日志里「退回到哪」的文案：用动作口径（编写立项单/编写规格书/编写承认书），
    /// 与流程记录里「发生了什么」的 RecordTitle 拼成「编写规格书 → 编写立项单」。
    /// </summary>
    public static string RejectTitle(int node) => Normalize(node) switch
    {
        LxCreate => "编写立项单",
        GgWrite => "编写规格书",
        CrWrite => "编写承认书",
        _ => RecordTitle(node)
    };

    /// <summary>退回对话框里显示「这一步会退给谁」：姓名（工号），没指派就显示角色名。</summary>
    public static string AssigneeLabel(
        int node,
        string? ownerEmpNo, string? ownerName,
        string? engineerEmpNo, string? engineerName,
        string? managerEmpNo, string? managerName,
        string? finalEmpNo, string? finalName)
    {
        var (emp, name, fallback) = Normalize(node) switch
        {
            GgWrite or GgSubmit => (engineerEmpNo, engineerName, "研发工程师"),
            GgApprove or CrApprove => (managerEmpNo, managerName, "研发经理"),
            CrFinal => (finalEmpNo, finalName, "销售经理"),
            _ => (ownerEmpNo, ownerName, "业务员")
        };

        if (string.IsNullOrWhiteSpace(emp)) return fallback;
        return string.IsNullOrWhiteSpace(name)
            ? emp!.Trim()
            : $"{name!.Trim()}（{emp!.Trim()}）";
    }

    /// <summary>
    /// 退回下拉里的完整标签：节点号 + 责任人 + 动作，
    /// 例如「1.业务员编写立项单」「2.业务员指派研发工程师」，避免只写动作分不清谁负责。
    /// </summary>
    public static string OptionLabel(int node) => Normalize(node) switch
    {
        LxCreate => "1.业务员编写立项单",
        AssignEngineer => "2.业务员指派研发工程师",
        GgWrite => "3.研发工程师编写规格书",
        GgSubmit => "4.研发工程师提交规格书审批",
        GgApprove => "5.研发经理审批规格书",
        GgSign => "6.业务员回传签字版规格书",
        CrWrite => "7.业务员编写承认书",
        CrSubmit => "8.业务员提交承认书审批",
        CrApprove => "9.研发经理审批承认书",
        CrFinal => "10.销售经理终审承认书",
        CrSign => "11.业务员回传签字版承认书",
        Archive => "12.业务员归档",
        _ => RecordTitle(node)
    };

    public static bool IsValid(int node) => node >= LxCreate && node <= Archived;

    /// <summary>还在流程中（1..12），归档（13）后为 false。</summary>
    public static bool IsActive(int node) => node >= LxCreate && node <= Archive;

    /// <summary>归档后的节点统一按归档处理，历史 0 值按立项单处理。</summary>
    public static int Normalize(int node)
    {
        if (node <= None) return LxCreate;
        return node > Archived ? Archived : node;
    }

    /// <summary>节点对应的主单据类型（LX / GG / CR），用于跳转。</summary>
    public static string DocKind(int node) => Normalize(node) switch
    {
        GgWrite or GgSubmit or GgApprove or GgSign => RemoteFlow.DocGg,
        CrWrite or CrSubmit or CrApprove or CrFinal or CrSign => RemoteFlow.DocCr,
        Archive or Archived => RemoteFlow.DocCr,
        _ => RemoteFlow.DocLx
    };

    /// <summary>映射到列表页 stepper 的四个圆点：0 立项单 / 1 规格书 / 2 承认书 / 3 归档。</summary>
    public static int Stage(int node) => Normalize(node) switch
    {
        GgWrite or GgSubmit or GgApprove or GgSign => RemoteFlow.StageGg,
        CrWrite or CrSubmit or CrApprove or CrFinal or CrSign => RemoteFlow.StageCr,
        Archive or Archived => RemoteFlow.StageArchive,
        _ => RemoteFlow.StageLx
    };

    /// <summary>
    /// 节点默认的待办承担者。
    /// 指定人节点：1/2/6/7/8/11/12 业务员，3/4 研发工程师，10 销售经理；
    /// 角色节点：5/9 研发经理，由 <c>PendingEmpNo</c> / <c>PendingRole</c> 两列分开存，判定时不混用。
    /// 注意：节点 5 在流程里会由版本表的 ManagerEmpNo 覆盖成具体经理（承认书审批沿用），
    /// 这里的角色只是没人可指派时的兜底。
    /// </summary>
    public static (string? EmpNo, string? Role) PendingOf(
        int node, string? ownerEmpNo, string? engineerEmpNo, string? finalEmpNo = null)
    {
        var owner = Blank(ownerEmpNo) ? null : ownerEmpNo!.Trim();
        var engineer = Blank(engineerEmpNo) ? null : engineerEmpNo!.Trim();
        var final = Blank(finalEmpNo) ? null : finalEmpNo!.Trim();

        return Normalize(node) switch
        {
            LxCreate or AssignEngineer or GgSign or CrWrite or CrSubmit or CrSign or Archive => (owner, null),
            GgWrite or GgSubmit => (engineer, null),
            GgApprove or CrApprove => (null, RemoteFlow.RoleRdManager),
            // 终审：版本上指定了人就是那个人；没指定时退回「销售经理」角色池
            CrFinal => final is null ? (null, RemoteFlow.RoleFinalApprover) : (final, null),
            _ => (null, null)
        };
    }

    /// <summary>
    /// 流程日志里这一行的「相关人」：这一步把活交给了谁。
    /// 只有真正发生流转的节点才有相关人；编写类节点（1/3/6/7/11/12）没有。
    /// </summary>
    public static string? RelatedOf(
        int node, string? ownerEmpNo, string? engineerEmpNo,
        string? managerEmpNo, string? finalEmpNo)
    {
        static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        return Normalize(node) switch
        {
            AssignEngineer => Clean(engineerEmpNo),
            GgSubmit => Clean(managerEmpNo),
            CrSubmit => Clean(managerEmpNo),
            GgApprove => Clean(ownerEmpNo),
            CrFinal => Clean(ownerEmpNo),
            CrApprove => Clean(finalEmpNo),
            _ => null
        };
    }

    /// <summary>
    /// 当前节点是否落在登录人手上（列表整行标淡红、顶部待办计数用）。
    /// 指定人节点只认人；角色节点只认角色 —— 和审批权限判定保持一致。
    /// </summary>
    public static bool IsMine(
        int node, string? pendingEmpNo, string? pendingRole, string? myEmpNo, string? myRole)
    {
        if (!IsActive(node)) return false;
        var me = (myEmpNo ?? "").Trim();
        if (me.Length == 0) return false;

        if (!Blank(pendingEmpNo)) return Same(pendingEmpNo, me);
        if (Blank(pendingRole)) return false;

        // 老数据里角色码写作 RdManager，一并认
        if (Same(pendingRole, myRole)) return true;
        return Same(pendingRole, RemoteFlow.RoleRdManager) && Same(myRole, "RdManager");
    }

    /// <summary>
    /// 「当前流程在谁那处理」列的显示文案。
    ///
    /// 指定人节点优先用库里现成的姓名，省一次人物档案查询；
    /// 工号和库里存的姓名对不上（中途换过人）时老实用待办工号，免得张冠李戴。
    /// 角色节点没人可指派时显示角色名。
    /// </summary>
    public static string PendingName(
        int node,
        string? pendingEmpNo,
        string? pendingRole,
        string? ownerName = null,
        string? ownerEmpNo = null,
        string? engineerName = null,
        string? engineerEmpNo = null,
        string? managerName = null,
        string? finalName = null)
    {
        var target = Normalize(node);
        if (!IsActive(target)) return "—";

        var emp = Blank(pendingEmpNo) ? null : pendingEmpNo!.Trim();
        if (emp is null)
        {
            // 审批节点只存了角色码，但版本表上通常已有指派的经理姓名，优先显示真实的人
            if (target is (GgApprove or CrApprove) && !Blank(managerName))
                return managerName!.Trim();

            return Blank(pendingRole) ? "—" : RemoteFlow.RoleName(pendingRole);
        }

        if (target is (LxCreate or AssignEngineer or GgSign or CrWrite or CrSubmit or CrSign or Archive)
            && Same(emp, ownerEmpNo) && !Blank(ownerName))
            return ownerName!.Trim();

        if (target is (GgWrite or GgSubmit) && Same(emp, engineerEmpNo) && !Blank(engineerName))
            return engineerName!.Trim();

        if (target is (GgApprove or CrApprove) && !Blank(managerName))
            return managerName!.Trim();

        // 终审人姓名存在版本表上（提交承认书时选定），直接拿来用
        if (target == CrFinal && !Blank(finalName))
            return finalName!.Trim();

        return emp;
    }

    /// <summary>
    /// 点进项目时该打开哪份单据。
    ///
    /// 1. 节点 1~2（立项单编写 / 指派）：进立项单。
    /// 2. 节点 3、5（规格书编写 / 审批）：只有研发侧直接进规格书；业务员停在立项单。
    /// 3. 节点 4（提交审批）、6（回传签字版规格书）：进规格书。
    /// 4. 节点 7~11（承认书环节）：进承认书。
    /// 5. 归档后沿用「最高阶已生成」：承认书 → 规格书 → 立项单。
    /// </summary>
    public static string EntryHref(
        int node, string? lxDocGuid, string? ggDocGuid, string? ggStatus, string? crDocGuid,
        bool isMyTodo = false, string? myRole = null, string? lxStatus = null)
    {
        var target = Normalize(node);

        // 立项单环节：入口一律落在立项单。
        // 正常退回时立项单是 LxDraft；早期单据的外键/状态没回填时，只要规格书还是草稿，
        // 也说明这一轮退回到了立项单，不能再被已作废的规格书草稿截胡。
        if (target is LxCreate or AssignEngineer
            && (Same(lxStatus, RemoteFlow.LxDraft) || Same(ggStatus, RemoteFlow.GgDraft)))
            return Href("lx", lxDocGuid) ?? Href("gg", ggDocGuid) ?? "";

        if (target is GgWrite or GgApprove && !IsRdSide(isMyTodo, myRole))
            return Href("lx", lxDocGuid) ?? Href("gg", ggDocGuid) ?? "";

        // 回传签字版规格书：还在规格书页；承认书环节：全部进承认书页
        if (target is GgSubmit or GgSign)
            return Href("gg", ggDocGuid) ?? Href("lx", lxDocGuid) ?? "";
        if (target is CrWrite or CrSubmit or CrApprove or CrFinal or CrSign)
            return Href("cr", crDocGuid) ?? Href("gg", ggDocGuid) ?? Href("lx", lxDocGuid) ?? "";

        return Href("cr", crDocGuid)
               ?? Href("gg", ggDocGuid)
               ?? Href("lx", lxDocGuid)
               ?? "";
    }

    /// <summary>
    /// 规格书对登录人是否可见可点：节点 6 起（研发经理已审批通过）与归档后对所有人开放；
    /// 节点 1~5 只给研发侧 —— 业务员看不到还在制作的草稿。
    /// </summary>
    public static bool CanViewGg(
        int node, string? myEmpNo, string? myRole,
        string? pendingEmpNo = null, string? pendingRole = null)
    {
        var target = Normalize(node);
        return target >= GgSign
               || IsRdSide(false, myRole, myEmpNo)
               || IsMine(target, pendingEmpNo, pendingRole, myEmpNo, myRole);
    }

    /// <summary>
    /// 研发侧：我就是这条待办的人、本身有研发角色，或超级管理员 JcAdmin。
    /// 普通 admin 角色不放大查看权 —— 节点 6 之前规格书草稿只给研发侧看。
    /// </summary>
    private static bool IsRdSide(bool isMyTodo, string? myRole, string? empNo = null)
        => isMyTodo
           || AppRoles.IsRdRole(myRole)
           || AppRoles.IsStrictAdmin(myRole, empNo);

    private static string? Href(string kind, string? docGuid)
    {
        var guid = (docGuid ?? "").Trim();
        return guid.Length == 0
            ? null
            : $"/project/remote/{kind}/{Uri.EscapeDataString(guid)}";
    }

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);

    private static bool Same(string? a, string? b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}

