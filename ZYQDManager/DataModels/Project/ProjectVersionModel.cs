using ZYQDManager.Utilities;

namespace ZYQDManager.DataModels;

/// <summary>
/// 电机项目版本 T_ZYQD_ProjectVersion。
/// 一个版本 = 同一 ProjectGuid + VersionNo 下的「立项单 + 规格书 + 承认书」三份单据。
/// 任意一步需要修改就新建一个版本，历史版本整版冻结，可在项目里切换查看。
///
/// 职责：只记录「当前走到哪一步、每步做到什么状态、在等谁」。
/// 完整的流程历史（含退回、重走）由 T_ZYQD_ProjectFlowLog 负责，不在这张表里。
/// </summary>
public class ProjectVersionModel
{
    public int Id { get; set; }
    public string ProjectGuid { get; set; } = "";
    public string VersionNo { get; set; } = "";

    /// <summary>版本状态：InProgress / ReadyToArchive / Archived，见 <see cref="Utilities.MotorFlow"/>。</summary>
    public string Status { get; set; } = "";

    /// <summary>
    /// 当前流程节点 1..12，13 = 已归档，0 = 历史数据未回填（读取时按节点 1 处理）。
    /// 流程判断、入口跳转、我的待办都以这一列为准，见 <see cref="Utilities.MotorNode"/>。
    /// </summary>
    public int CurrentNode { get; set; }

    /// <summary>
    /// 当前节点待办人工号。指定人节点（1/2/3/4/6/7/8/10/11/12）存具体人；
    /// 角色节点（5/9）为空，改看 <see cref="PendingRole"/>。
    /// 这是「待办归属」的唯一来源，列表页、我的待办都读这里。
    /// </summary>
    public string? PendingEmpNo { get; set; }

    /// <summary>当前节点待办角色码（角色节点：5 规格书审批 / 9 承认书审批）。</summary>
    public string? PendingRole { get; set; }

    /// <summary>
    /// 这个版本的研发经理（节点 4 提交审批时由研发工程师指定）。
    /// 节点 5、9 承认书审批沿用同一个人，不再重复指派；审批权限也按这一列卡人。
    /// </summary>
    public string? ManagerEmpNo { get; set; }

    /// <summary>
    /// 版本级终审人（销售经理）：业务员提交承认书时选定，节点 10 只认这个人。
    /// </summary>
    public string? FinalEmpNo { get; set; }

    /// <summary>进入当前节点的时间，用于停留时长与超期提醒。</summary>
    public DateTime? NodeEnterTime { get; set; }

    /// <summary>把版本推进到当前节点的人（审计用）。</summary>
    public string? NodeUpdateEmpNo { get; set; }

    /// <summary>从哪个版本派生出来，V1 为空。</summary>
    public string? BaseVersionNo { get; set; }

    /// <summary>这次改版的原因，业务员新建版本时填写。</summary>
    public string? ChangeReason { get; set; }

    /// <summary>改动的单据范围，逗号分隔：LX / GG / CR。用于决定哪些步骤要重走。</summary>
    public string? ChangeScope { get; set; }

    /// <summary>
    /// 逐字段的修改点，JSON 数组：[{Field,OldValue,NewValue}]，由
    /// <see cref="Utilities.MotorLxDiff"/> 生成。
    /// </summary>
    public string? ChangeDiff { get; set; }

    public DateTime? ArchivedTime { get; set; }
    public string? ArchivePersonCode { get; set; }

    public DateTime? CreateTime { get; set; }
    public string? CreatePersonCode { get; set; }

    // ==================== 12 步节点留痕 ====================
    // 每步三列：负责人（工号）/ 状态 / 完成时间，共 36 列。
    // 这是「这个节点谁在做、做到哪一步」的落库记录；
    // 流程能否前进以 CurrentNode 为准，两者由 MotorFlowService.MoveNodeAsync 同步写入。
    //   1 创建立项单      2 指派研发工程师   3 编写规格书      4 提交审核（规格书）
    //   5 研发经理审核（规格书）  6 回传签名版规格书
    //   7 编写承认书      8 提交审核（承认书） 9 研发经理审核（承认书）
    //   10 销售经理审核（承认书） 11 回传签名版承认书 12 归档
    //
    // 状态取值（见 MotorFlow.NodeIdle / NodeRunning / NodeFinished）：
    //   -1 未开始   0 进行中   1 已完成
    // 负责人一律存工号 —— 工号唯一，姓名会变、会重名；未指派时为 NULL。

    public string? Node1EmpNo { get; set; }
    public int Node1Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node1DoneTime { get; set; }

    public string? Node2EmpNo { get; set; }
    public int Node2Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node2DoneTime { get; set; }

    public string? Node3EmpNo { get; set; }
    public int Node3Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node3DoneTime { get; set; }

    public string? Node4EmpNo { get; set; }
    public int Node4Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node4DoneTime { get; set; }

    public string? Node5EmpNo { get; set; }
    public int Node5Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node5DoneTime { get; set; }

    public string? Node6EmpNo { get; set; }
    public int Node6Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node6DoneTime { get; set; }

    public string? Node7EmpNo { get; set; }
    public int Node7Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node7DoneTime { get; set; }

    public string? Node8EmpNo { get; set; }
    public int Node8Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node8DoneTime { get; set; }

    public string? Node9EmpNo { get; set; }
    public int Node9Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node9DoneTime { get; set; }

    public string? Node10EmpNo { get; set; }
    public int Node10Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node10DoneTime { get; set; }

    public string? Node11EmpNo { get; set; }
    public int Node11Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node11DoneTime { get; set; }

    public string? Node12EmpNo { get; set; }
    public int Node12Status { get; set; } = MotorFlow.NodeIdle;
    public DateTime? Node12DoneTime { get; set; }

    /// <summary>该节点的状态：-1 未开始 / 0 进行中 / 1 已完成。</summary>
    public int NodeState(int node) => node switch
    {
        1 => Node1Status,
        2 => Node2Status,
        3 => Node3Status,
        4 => Node4Status,
        5 => Node5Status,
        6 => Node6Status,
        7 => Node7Status,
        8 => Node8Status,
        9 => Node9Status,
        10 => Node10Status,
        11 => Node11Status,
        12 => Node12Status,
        _ => MotorFlow.NodeIdle
    };

    /// <summary>该节点是否已完成。</summary>
    public bool IsNodeDone(int node) => NodeState(node) == MotorFlow.NodeFinished;

    /// <summary>读该节点记录的负责人与完成时间。</summary>
    public (string? EmpNo, DateTime? DoneTime) NodeRecord(int node) => node switch
    {
        1 => (Node1EmpNo, Node1DoneTime),
        2 => (Node2EmpNo, Node2DoneTime),
        3 => (Node3EmpNo, Node3DoneTime),
        4 => (Node4EmpNo, Node4DoneTime),
        5 => (Node5EmpNo, Node5DoneTime),
        6 => (Node6EmpNo, Node6DoneTime),
        7 => (Node7EmpNo, Node7DoneTime),
        8 => (Node8EmpNo, Node8DoneTime),
        9 => (Node9EmpNo, Node9DoneTime),
        10 => (Node10EmpNo, Node10DoneTime),
        11 => (Node11EmpNo, Node11DoneTime),
        12 => (Node12EmpNo, Node12DoneTime),
        _ => (null, null)
    };

    /// <summary>
    /// 记录节点责任人（工号）。进入节点时调用一次：
    /// 这样「进行中」也能看出谁在做，不必等做完才有人名。
    /// 只写工号，不碰状态 / 完成时间。
    /// </summary>
    public void SetNodeOwner(int node, string? empNo)
    {
        if (node is < 1 or > 12) return;
        var emp = string.IsNullOrWhiteSpace(empNo) ? null : empNo.Trim();
        switch (node)
        {
            case 1: Node1EmpNo = emp; break;
            case 2: Node2EmpNo = emp; break;
            case 3: Node3EmpNo = emp; break;
            case 4: Node4EmpNo = emp; break;
            case 5: Node5EmpNo = emp; break;
            case 6: Node6EmpNo = emp; break;
            case 7: Node7EmpNo = emp; break;
            case 8: Node8EmpNo = emp; break;
            case 9: Node9EmpNo = emp; break;
            case 10: Node10EmpNo = emp; break;
            case 11: Node11EmpNo = emp; break;
            case 12: Node12EmpNo = emp; break;
        }
    }

    /// <summary>
    /// 标记节点完成（状态 1 + 完成时间 + 负责人）。
    /// 已完成的不再覆盖 —— 退回后在原地重新走一遍时，
    /// 首次完成时间与负责人应当保留，不能因为重试被刷新。
    /// 编写类节点（1/3/7）在保存草稿时已由 <see cref="TouchNodeTime"/> 写过时间，
    /// 提交时不得用提交时刻覆盖，否则「最后保存时间」就丢了。
    /// </summary>
    public void MarkNodeDone(int node, string? empNo, DateTime time)
    {
        if (node is < 1 or > 12) return;
        if (IsNodeDone(node)) return;
        var (existEmp, existTime) = NodeRecord(node);
        var doneTime = node is (1 or 3 or 7) && existTime is not null ? existTime.Value : time;
        var owner = node is (1 or 3 or 7) && !string.IsNullOrWhiteSpace(existEmp) ? existEmp : empNo;
        WriteNode(node, MotorFlow.NodeFinished, owner, doneTime);
    }

    /// <summary>把节点打回未开始（-1），负责人与完成时间一并清空。</summary>
    public void ClearNodeDone(int node)
    {
        if (node is < 1 or > 12) return;
        WriteNode(node, MotorFlow.NodeIdle, null, null);
    }

    /// <summary>只改状态，保留负责人（进入节点时用：状态 0 进行中，人已经指派好了）。</summary>
    public void SetNodeStatus(int node, int status)
    {
        if (node is < 1 or > 12) return;
        switch (node)
        {
            case 1: Node1Status = status; break;
            case 2: Node2Status = status; break;
            case 3: Node3Status = status; break;
            case 4: Node4Status = status; break;
            case 5: Node5Status = status; break;
            case 6: Node6Status = status; break;
            case 7: Node7Status = status; break;
            case 8: Node8Status = status; break;
            case 9: Node9Status = status; break;
            case 10: Node10Status = status; break;
            case 11: Node11Status = status; break;
            case 12: Node12Status = status; break;
        }
    }

    /// <summary>
    /// 刷新「编写」类节点（1 立项 / 3 规格书 / 7 承认书）的完成时间。
    /// 这三步在提交前每次保存都会更新时间，所以接受反复覆盖；
    /// 负责人保留首次写入值不动（只有为空时才补）。
    /// </summary>
    public void TouchNodeTime(int node, DateTime time, string? empNo = null)
    {
        if (node is not (1 or 3 or 7)) return;
        var emp = string.IsNullOrWhiteSpace(empNo) ? null : empNo.Trim();
        switch (node)
        {
            case 1:
                Node1DoneTime = time;
                if (emp is not null) Node1EmpNo = emp;
                break;
            case 3:
                Node3DoneTime = time;
                if (emp is not null) Node3EmpNo = emp;
                break;
            case 7:
                Node7DoneTime = time;
                if (emp is not null) Node7EmpNo = emp;
                break;
        }
    }

    /// <summary>当前处于「进行中」的节点号（1..12）；全部完成或未开始返回 0。</summary>
    public int RunningNode()
    {
        for (var node = 1; node <= 12; node++)
        {
            if (NodeState(node) == MotorFlow.NodeRunning) return node;
        }
        return 0;
    }

    private void WriteNode(int node, int status, string? empNo, DateTime? time)
    {
        var emp = string.IsNullOrWhiteSpace(empNo) ? null : empNo.Trim();
        var doneTime = status == MotorFlow.NodeFinished ? time : null;
        switch (node)
        {
            case 1: Node1Status = status; Node1EmpNo = emp; Node1DoneTime = doneTime; break;
            case 2: Node2Status = status; Node2EmpNo = emp; Node2DoneTime = doneTime; break;
            case 3: Node3Status = status; Node3EmpNo = emp; Node3DoneTime = doneTime; break;
            case 4: Node4Status = status; Node4EmpNo = emp; Node4DoneTime = doneTime; break;
            case 5: Node5Status = status; Node5EmpNo = emp; Node5DoneTime = doneTime; break;
            case 6: Node6Status = status; Node6EmpNo = emp; Node6DoneTime = doneTime; break;
            case 7: Node7Status = status; Node7EmpNo = emp; Node7DoneTime = doneTime; break;
            case 8: Node8Status = status; Node8EmpNo = emp; Node8DoneTime = doneTime; break;
            case 9: Node9Status = status; Node9EmpNo = emp; Node9DoneTime = doneTime; break;
            case 10: Node10Status = status; Node10EmpNo = emp; Node10DoneTime = doneTime; break;
            case 11: Node11Status = status; Node11EmpNo = emp; Node11DoneTime = doneTime; break;
            case 12: Node12Status = status; Node12EmpNo = emp; Node12DoneTime = doneTime; break;
        }
    }
}
