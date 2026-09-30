namespace ZYQDManager.DataModels;

/// <summary>样品检验报告主表 T_ZYQD_SIR_main</summary>
public class ZyqdSirMainModel
{
    public int Id { get; set; }
    public string? guid { get; set; }

    public string? Department { get; set; }
    public string? SalesmanName { get; set; }
    public string? Client { get; set; }
    public string? ClientDepartment { get; set; }
    public string? ContactName { get; set; }
    public string? Report_type { get; set; }
    public string? product_type { get; set; }

    public string? Product_partNo { get; set; }
    public string? ProductName { get; set; }
    public string? SoftwareVersion { get; set; }
    public DateTime IssuanceDate { get; set; }
    public int SampleQuantity { get; set; }
    public int DeliveryTimes { get; set; }
    public string? program_name { get; set; }
    public string? program_code { get; set; }
    public string? check_code { get; set; }

    public string? Test_Engineer { get; set; }
    public string? Test_Checked_by { get; set; }
    public string? Test_Approved_By { get; set; }

    public bool UL { get; set; }
    public bool FCC { get; set; }
    public bool CE { get; set; }
    public bool RoHS { get; set; }
    public bool REACH { get; set; }
    public bool Certification_None { get; set; }

    public bool Appearance_Approval_Report { get; set; }
    public bool Dimension_Report { get; set; }
    public bool Material_Report { get; set; }
    public bool Function_Report { get; set; }

    public string? Note { get; set; }
    public DateTime? createTime { get; set; }
    public string? ImageUrls { get; set; }
}
