namespace ZYQDManager.DataModels;

/// <summary>
/// 遥控器样品承认书 T_ZYQD_Remote_CR（同 ProjectGuid 多版本）。
/// 与规格书共用正文结构（ExtraJson = RemoteDocExtra），但库表、流程审批与电机完全独立。
/// </summary>
public class RemoteCrModel
{
    // ===== 通用流程字段 =====
    public int Id { get; set; }
    public string ProjectGuid { get; set; } = "";
    public string DocGuid { get; set; } = "";
    public string VersionNo { get; set; } = "vn";
    public bool IsDraft { get; set; } = true;
    public DateTime? ForwardedAt { get; set; }
    public string? Status { get; set; }
    public string? SourceLxDocGuid { get; set; }
    public string? SourceGgDocGuid { get; set; }

    /// <summary>研发经理审批（第一级）。</summary>
    public string? ManagerCode { get; set; }
    public DateTime? ManagerApproveTime { get; set; }

    /// <summary>蒋经理审批（第二级）。</summary>
    public string? JiangCode { get; set; }
    public DateTime? JiangApproveTime { get; set; }

    /// <summary>业务员回传的客户签字版文件。</summary>
    public string? SignFileId { get; set; }
    public string? SignFileName { get; set; }
    public DateTime? SignUploadTime { get; set; }
    public string? SignUploadPersonCode { get; set; }

    /// <summary>沿用上一版本时不重走审批。</summary>
    public string? InheritedFromVersion { get; set; }

    /// <summary>参数表/配件/包装/样品检验等，RemoteDocExtra 序列化 JSON。</summary>
    public string? ExtraJson { get; set; }

    // ===== 表头基础信息 =====
    public string? Customer { get; set; }
    public string? DemandPerson { get; set; }
    public string? CustomerPartCode { get; set; }
    public string? MotorModelSpec { get; set; }
    public string? Description { get; set; }
    public string? ProductDescription { get; set; }
    public DateTime? TimeToRequest { get; set; }
    public DateTime? RequiredCompletionTime { get; set; }

    // ===== 样品承认书确认签字 =====
    public string? RdConfirm { get; set; }
    public string? RdConfirmDate { get; set; }
    public string? CustomerConfirm { get; set; }
    public string? CustomerConfirmDate { get; set; }
    public string? SaleConfirm { get; set; }
    public string? SaleConfirmDate { get; set; }

    // ===== 审计 =====
    public string? DocRemark { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? CreatePersonCode { get; set; }
    public string? UpdatePersonCode { get; set; }
    public string DeleteFlag { get; set; } = "";
    public DateTime? DeleteTime { get; set; }
    public string? DeletePersonCode { get; set; }
}
