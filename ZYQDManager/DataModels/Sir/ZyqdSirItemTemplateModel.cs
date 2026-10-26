namespace ZYQDManager.DataModels;

/// <summary>检测项目模板 T_ZYQD_SIR_item_template（按检测类型 + 产品类型维护文案）</summary>
public class ZyqdSirItemTemplateModel
{
    public int Id { get; set; }
    public string Report_type { get; set; } = "";
    public string product_type { get; set; } = "";
    public string Specification_type { get; set; } = "";
    public int SortNo { get; set; }
    public string? GroupTitle { get; set; }
    public string? Specification { get; set; }
    public string? Specification_EN { get; set; }
    public string? Criteria { get; set; }
    public string? Criteria_EN { get; set; }
    public string filling_type { get; set; } = "0";
    public bool Enabled { get; set; } = true;
    public DateTime? createTime { get; set; }
}
