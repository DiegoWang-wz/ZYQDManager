namespace ZYQDManager.DataModels;

/// <summary>样品检验报告明细 T_ZYQD_SIR_detail</summary>
public class ZyqdSirDetailModel
{
    public int Id { get; set; }
    public string? main_guid { get; set; }
    public string? Specification_type { get; set; }
    public string? Specification { get; set; }
    public string? Criteria { get; set; }

    public string? Actual_Results1 { get; set; }
    public string? Actual_Results2 { get; set; }
    public string? Actual_Results3 { get; set; }
    public string? Actual_Results4 { get; set; }
    public string? Actual_Results5 { get; set; }
    public string? Actual_Results6 { get; set; }

    public string? Conclusion_A { get; set; }
    public string? Conclusio_B { get; set; }
    public string? Conclusion_C { get; set; }
    public string? Conclusion_D { get; set; }

    public double qty1 { get; set; }
    public double Price1 { get; set; }
    public double Discount1 { get; set; }
    public double Amount1 { get; set; }
    public double qty2 { get; set; }
    public double Price2 { get; set; }
    public double Discount2 { get; set; }
    public double Amount2 { get; set; }
    public double qty3 { get; set; }
    public double Price3 { get; set; }
    public double Discount3 { get; set; }
    public double Amount3 { get; set; }
    public double qty4 { get; set; }
    public double Price4 { get; set; }
    public double Discount4 { get; set; }
    public double Amount4 { get; set; }

    public string? Note { get; set; }
    public DateTime? createTime { get; set; }
    public string? Report_type { get; set; }
    public string? filling_type { get; set; }
}
