namespace ZYQDManager.DataModels;

/// <summary>
/// 项目主表 T_ZYQD_Project。
///
/// 一个项目 = 一条记录，跨版本、跨单据唯一一份数据。
/// 三张单据（LX/GG/CR）通过 ProjectGuid 挂在项目上，
/// 版本通过 T_ZYQD_ProjectVersion 挂，构成三层：
///     Project → ProjectVersion → (LX / GG / CR)
///
/// 字段划分原则：只放「跨版本不变」的信息，
/// 技术参数属于单据、版本次次可能不同，不放这里。
/// </summary>
public class ProjectModel
{
    public int Id { get; set; }

    /// <summary>项目标识，与三张单据表的 ProjectGuid 同值，全库唯一。</summary>
    public string ProjectGuid { get; set; } = "";

    /// <summary>产品类型，见 <see cref="Utilities.ProjectTypes"/>。四类产品共用这张表。</summary>
    public string ProductType { get; set; } = Utilities.ProjectTypes.Motor;

    // ---------- 项目身份 ----------

    public string? Customer { get; set; }
    public string? CustomerPartCode { get; set; }

    /// <summary>产品型号（沿用立项单的列名 MotorModelSpec）。</summary>
    public string? MotorModelSpec { get; set; }

    /// <summary>品名。</summary>
    public string? Description { get; set; }

    public string? DemandPerson { get; set; }

    // ---------- 人员（离职移交只改这两组）----------

    /// <summary>项目负责人（业务员）。项目归属以此为准。</summary>
    public string? OwnerEmpNo { get; set; }

    // ---------- 系统维护 ----------

    /// <summary>
    /// 当前版本号，指向 <see cref="ProjectVersionModel"/>。
    /// 由代码维护的指针，用户不可编辑；查不到对应版本时按最新一条立项单兜底。
    /// </summary>
    public string? CurrentVersionNo { get; set; }

    /// <summary>项目状态：Active / Void（作废），见 <see cref="Utilities.ProjectStatus"/>。</summary>
    public string Status { get; set; } = Utilities.ProjectStatus.Active;

    // ---------- 审计 ----------

    public DateTime? CreateTime { get; set; }
    public string? CreatePersonCode { get; set; }

    public DateTime? UpdateTime { get; set; }
    public string? UpdatePersonCode { get; set; }

    public string DeleteFlag { get; set; } = "";
    public DateTime? DeleteTime { get; set; }
    public string? DeletePersonCode { get; set; }
}
