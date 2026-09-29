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
    public string Action { get; set; } = "";
    public string FromUserCode { get; set; } = "";
    public string? FromUserName { get; set; }
    public string ToUserCode { get; set; } = "";
    public string? ToUserName { get; set; }
    public string? ToRole { get; set; }
    public string TodoStatus { get; set; } = "Pending";
    public string? FlowComment { get; set; }
    public DateTime ActionTime { get; set; }
    public DateTime? DoneTime { get; set; }
    public DateTime? CreateTime { get; set; }
}
