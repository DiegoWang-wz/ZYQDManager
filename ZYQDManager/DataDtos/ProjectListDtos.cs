namespace ZYQDManager.DataDtos;

public class ProjectListQueryDto
{
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    /// <summary>Customer / DemandPerson / CustomerPartCode / MotorModelSpec / Description</summary>
    public string FilterColumn { get; set; } = "Customer";
    public string? Keyword { get; set; }
    public bool IncludeDraft { get; set; } = true;
    public bool IncludeForwarded { get; set; } = true;

    /// <summary>管理员或「查看全部客户」时为 true。</summary>
    public bool ViewAllCustomers { get; set; } = true;
    public string? ViewerEmpNo { get; set; }
    public List<string> AssignedCustomerCodes { get; set; } = new();
}

public class ProjectListItemDto
{
    public int Id { get; set; }
    public string ProjectGuid { get; set; } = "";
    public string DocGuid { get; set; } = "";
    public string ProductType { get; set; } = "电机";
    public string VersionNo { get; set; } = "";
    public bool IsDraft { get; set; }
    public string? Customer { get; set; }
    public string? DemandPerson { get; set; }
    public string? CustomerPartCode { get; set; }
    public string? ProductModel { get; set; }
    public string? Description { get; set; }
    public DateTime? TimeToRequest { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }

    /// <summary>研发审批1</summary>
    public string? RdApprove1Name { get; set; }
    public string? RdApprove1Comment { get; set; }
    public string? RdApprove1Status { get; set; }

    /// <summary>研发审批2</summary>
    public string? RdApprove2Name { get; set; }
    public string? RdApprove2Comment { get; set; }
    public string? RdApprove2Status { get; set; }

    /// <summary>最终审批</summary>
    public string? FinalApproveName { get; set; }
    public string? FinalApproveComment { get; set; }
    public string? FinalApproveStatus { get; set; }

    public string? CurrentAssigneeName { get; set; }
    public string? CurrentStepCode { get; set; }
    public string VersionLabel => IsDraft ? $"{VersionNo}（草稿）" : VersionNo;
}
