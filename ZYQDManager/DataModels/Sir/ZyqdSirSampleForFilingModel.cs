namespace ZYQDManager.DataModels;

/// <summary>样品留样明细 T_ZYQD_SIR_datail_SampleForFiling（表名保留旧拼写 datail）</summary>
public class ZyqdSirSampleForFilingModel
{
    public int Id { get; set; }
    public int code { get; set; }
    public string? main_guid { get; set; }
    public string? Specification_type { get; set; }
    public string? Report_type { get; set; }
    public string? materialNo { get; set; }
    public string? materialName { get; set; }
    public string? manufacturer { get; set; }
    public string? materialModel { get; set; }
    public string? materiaSpecs { get; set; }
    public string? Note { get; set; }
    public string? Note1 { get; set; }
    public DateTime? createTime { get; set; }
    public string? ImageUrls { get; set; }
}
