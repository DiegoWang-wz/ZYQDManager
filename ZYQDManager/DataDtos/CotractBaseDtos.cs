namespace ZYQDManager.DataDtos;

public sealed class CotractBaseQueryDto
{
    public string FilterColumn { get; set; } = "ClientNo";
    public string? Keyword { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public bool ViewAllCustomers { get; set; }
    public string? ViewerEmpNo { get; set; }
    public List<string> AssignedCustomerCodes { get; set; } = [];
    public string? SortLabel { get; set; }
    public bool SortDescending { get; set; } = true;
}

public sealed class CotractBaseImportResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public int Imported { get; set; }
    public int Replaced { get; set; }
}

/// <summary>列表可筛字段（不含图片列）</summary>
public static class CotractBaseFilterFields
{
    public static readonly (string Key, string Label)[] All =
    [
        ("ClientNo", "客户编号"),
        ("ClientName", "客户名称"),
        ("materialNo", "物料号"),
        ("materialName", "物料名"),
        ("Tuo_Mark", "托唛"),
        ("Customer_destination_country", "客户目的国"),
        ("materialType", "物料类型"),
        ("materialModel", "物料model"),
        ("materiaSpecs", "物料specs"),
        ("materiaPowered", "物料Powered"),
        ("materiaCust", "物料Cust"),
        ("Notes", "备注Notes"),
        ("Quantity_unit", "数量单位"),
        ("currency_unit", "货币单位"),
        ("price", "单价"),
        ("price_unit", "单价单位"),
        ("Box_label", "箱唛"),
        ("Box_label2", "箱唛2"),
        ("Client_poNo", "客户po号"),
        ("SAP_order_NO", "sap单号"),
        ("Packing_method", "打包方式"),
        ("Packing_method_unit", "打包方式单位"),
        ("Carton_Size", "纸箱尺寸"),
        ("Pallet_Size", "托盘尺寸"),
        ("palletizing_method", "打托方式"),
        ("palletizing_method_unit", "打托方式单位"),
        ("Net_weight", "净重"),
        ("gross_weight", "毛重"),
        ("Customer_Manager", "负责人"),
        ("CreatePerson", "创建人"),
        ("CreateTime", "创建日期")
    ];
}
