namespace ZYQDManager.DataModels;

/// <summary>审批流转 / 待办 T_ZYQD_ApprovalFlow</summary>
public class ApprovalFlowModel
{
    public long Id { get; set; }
    public string ProjectGuid { get; set; } = "";
    public string ProductType { get; set; } = "";
    public string DocType { get; set; } = "";
    public string DocGuid { get; set; } = "";
    public string? DocVersion { get; set; }
    public string StepCode { get; set; } = "";
    /// <summary>
    /// 动作发生前的单据状态。退回时 StepCode 存的是「退回到」的状态，
    /// 这里存退回当时的节点（例如 CrPendingJiang = 销售经理终审退回），
    /// 流程记录才写得清「从哪个节点退回到哪个节点」。老数据为空，展示时按前一条流转兜底。
    /// </summary>
    public string? PrevStepCode { get; set; }
    public string Action { get; set; } = "";
    public string FromUserCode { get; set; } = "";
    public string ToUserCode { get; set; } = "";
    public string? ToRole { get; set; }
    public string TodoStatus { get; set; } = "Pending";
    public string? FlowComment { get; set; }
    public DateTime ActionTime { get; set; }
    public DateTime? DoneTime { get; set; }
    public DateTime? CreateTime { get; set; }
}
