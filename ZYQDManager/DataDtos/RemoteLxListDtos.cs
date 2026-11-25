namespace ZYQDManager.DataDtos;

/// <summary>
/// 立项单列表行（对应 T_ZYQD_Motor_LX 的列表投影）。
/// 只带列表需要的列，避免把 120 多个业务字段整行拉回。
/// </summary>
public sealed class RemoteLxListDto
{
    /// <summary>
    /// 立项单自增 Id。列表数据现在按「项目」分页，这个字段只在服务端
    /// 用来挑出当前版本的立项单，不参与界面展示。
    /// </summary>
    public int Id { get; set; }

    public string ProjectGuid { get; set; } = "";
    public string DocGuid { get; set; } = "";

    /// <summary>当前版本的规格书 / 承认书标识，列表直接跳转用；没有则为空。</summary>
    public string GgDocGuid { get; set; } = "";
    public string CrDocGuid { get; set; } = "";

    /// <summary>
    /// 当前版本规格书 / 承认书的状态，只在「节点 4」判断该打开规格书还是承认书时使用，
    /// 不参与流程推进（流程只看 <see cref="Node"/>）。
    /// </summary>
    public string? GgStatus { get; set; }
    public string? CrStatus { get; set; }

    public string VersionNo { get; set; } = "";

    public string? Customer { get; set; }
    public string? DemandPerson { get; set; }
    public string? CustomerPartCode { get; set; }
    public string? MotorModelSpec { get; set; }
    public string? Description { get; set; }

    /// <summary>项目负责人（取自项目主表，离职移交时改的就是这个字段）。</summary>
    public string? OwnerEmpNo { get; set; }
    public string? OwnerName { get; set; }

    /// <summary>项目当前指派的研发工程师（取自项目主表），用于判定「规格书填写」是不是轮到自己。</summary>
    public string? EngineerEmpNo { get; set; }
    public string? EngineerName { get; set; }

    /// <summary>当前节点待办人工号（指定人节点才有，角色节点为空）。</summary>
    public string? PendingEmpNo { get; set; }

    /// <summary>当前节点待办角色码（研发经理等），与 <see cref="PendingEmpNo"/> 二选一。</summary>
    public string? PendingRole { get; set; }

    /// <summary>
    /// 「当前处理人」列的显示文案：指定人节点用姓名（查不到人时退回工号），
    /// 角色节点用角色名，归档没人则显示「—」。由 <see cref="Utilities.RemoteNode.PendingName"/> 统一算好。
    /// </summary>
    public string? PendingName { get; set; }

    /// <summary>
    /// 当前节点是否落在登录人手上（我的待办）：为 true 时列表整行标淡红。
    /// 判定规则见 <see cref="Utilities.RemoteNode.IsMine"/>。
    /// </summary>
    public bool IsMyTodo { get; set; }

    /// <summary>
    /// 流程节点 1..7，8 = 已归档，见 <see cref="Utilities.RemoteNode"/>。
    /// 入口跳转、阶段列、待办都从这里取，不再由三张单据的状态反推。
    /// </summary>
    public int Node { get; set; }

    /// <summary>节点中文（待谁做什么），列表阶段列悬停提示用。</summary>
    public string? NodeText { get; set; }

    public string? Status { get; set; }
    public bool IsDraft { get; set; }

    /// <summary>
    /// 当前所处阶段，取值见 <see cref="Utilities.RemoteFlow"/> 的 Stage 常量：
    /// 0 立项单 / 1 规格书 / 2 承认书 / 3 归档。
    /// </summary>
    public int Stage { get; set; }

    /// <summary>当前节点中文，例如「待研发经理审批」。</summary>
    public string? StageText { get; set; }

    /// <summary>立项单步骤是否完成（已提交给研发工程师）。</summary>
    public bool StepLxDone { get; set; }

    /// <summary>规格书步骤是否完成（研发经理通过且签字版已回传）。</summary>
    public bool StepGgDone { get; set; }

    /// <summary>承认书步骤是否完成（两级审批通过且签字版已回传）。</summary>
    public bool StepCrDone { get; set; }

    /// <summary>当前版本是否已归档。</summary>
    public bool StepArchived { get; set; }

    /// <summary>该阶段最近一次单据变更时间，用于阶段列的悬停提示。</summary>
    public DateTime? StageTime { get; set; }

    public string? UpdatePersonCode { get; set; }
    public string? UpdatePersonName { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? CreatePersonCode { get; set; }
    public string? CreatePersonName { get; set; }
    public DateTime? CreateTime { get; set; }
}

/// <summary>立项单列表查询条件。</summary>
public sealed class RemoteLxQueryDto
{
    /// <summary>过滤字段：Customer / DemandPerson / CustomerPartCode / MotorModelSpec / Description / Status。</summary>
    public string FilterColumn { get; set; } = RemoteLxFilterColumns.Customer;

    public string? Keyword { get; set; }
}

/// <summary>列表「条件」下拉的可用字段，与 <see cref="RemoteLxQueryDto.FilterColumn"/> 一一对应。</summary>
public static class RemoteLxFilterColumns
{
    public const string Customer = "Customer";
    public const string DemandPerson = "DemandPerson";
    public const string CustomerPartCode = "CustomerPartCode";
    public const string MotorModelSpec = "MotorModelSpec";
    public const string Description = "Description";
    public const string Status = "Status";

    public static readonly IReadOnlyList<(string Value, string Label)> All =
    [
        (Customer, "客户"),
        (DemandPerson, "需求人"),
        (CustomerPartCode, "客户产品编号"),
        (MotorModelSpec, "产品型号"),
        (Description, "品名"),
        (Status, "状态")
    ];

    public static bool IsValid(string? value)
        => All.Any(x => string.Equals(x.Value, value, StringComparison.Ordinal));
}

