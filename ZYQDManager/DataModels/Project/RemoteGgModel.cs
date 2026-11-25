namespace ZYQDManager.DataModels;

/// <summary>
/// 遥控器规格书 T_ZYQD_Remote_GG（同 ProjectGuid 多版本）。
/// 规格书正文参数、配件、包装、操作说明、变更履历等全部存 ExtraJson（RemoteDocExtra）。
/// 固定列只放流程字段与表头基础信息，与电机规格书完全独立。
/// </summary>
public class RemoteGgModel
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

    /// <summary>研发经理审批人（通过后写入）。</summary>
    public string? ApproverCode { get; set; }
    public DateTime? ApproveTime { get; set; }

    /// <summary>业务员回传的客户签字版文件（存文件服务返回的标识）。</summary>
    public string? SignFileId { get; set; }
    public string? SignFileName { get; set; }
    public DateTime? SignUploadTime { get; set; }
    public string? SignUploadPersonCode { get; set; }

    /// <summary>沿用上一版本时记录来源版本号；为空表示本版本重新填写。</summary>
    public string? InheritedFromVersion { get; set; }

    /// <summary>规格书正文（参数表/配件/包装/操作说明/变更履历等），RemoteDocExtra 序列化 JSON。</summary>
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
