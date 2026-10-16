namespace ZYQDManager.DataDtos;

public sealed class CustomsAndPackingQueryDto
{
    public string FilterColumn { get; set; } = "客户编码";
    public string? Keyword { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public bool ViewAll { get; set; }
    public string? ViewerEmpNo { get; set; }
    public string? SortLabel { get; set; }
    public bool SortDescending { get; set; } = true;
}

public sealed class CustomsAndPackingLineDto
{
    public int TempId { get; set; }
    public string? materialNo { get; set; }
    public string? materialName { get; set; }
    public string? materialType { get; set; }
    public string? materialModel { get; set; }
    public string? materiaSpecs { get; set; }
    public string? materiaPowered { get; set; }
    public string? materiaColor { get; set; }
    public string? materiaCust { get; set; }
    public string? UNIT { get; set; }
    public double price { get; set; }
    public double qty { get; set; }
    public string? currency_unit { get; set; }
    public string? Packing_method { get; set; }
    public string? Packing_method_unit { get; set; }
    /// <summary>尾箱装箱量（qty % 打包方式）</summary>
    public string? var1 { get; set; }
    public double box_qty { get; set; }
    /// <summary>整托数（不含尾托）</summary>
    public double Pallet_qty { get; set; }
    /// <summary>不足托箱数（box_qty % 打托方式）</summary>
    public string? var3 { get; set; }
    /// <summary>尾托尺寸，如 1200*800*100</summary>
    public string? var2 { get; set; }
    /// <summary>尾托打托方式</summary>
    public string? var4 { get; set; }
    public string? Carton_Size { get; set; }
    public string? Pallet_Size { get; set; }
    public string? palletizing_method { get; set; }
    public string? palletizing_method_unit { get; set; }
    public double Pallet_weight { get; set; }
    public double Net_weight { get; set; }
    public double gross_weight { get; set; }
    public double total_Net_weight { get; set; }
    public double total_gross_weight { get; set; }
    public double volume { get; set; }
    public string? Pallet_code { get; set; }
    public string? emit { get; set; }
    public string? packing_unit { get; set; }
    public string? sample_type { get; set; }
    public string? SAP_order_NO { get; set; }
    public string? Client_poNo { get; set; }
    public string? PI_Material_Name { get; set; }
    /// <summary>原始行 / 拆分行</summary>
    public string? var7 { get; set; }
    /// <summary>拼托角色：拼托主 / 拼托（挂靠）；空=未拼托</summary>
    public string? var8 { get; set; }
    /// <summary>拆分行来源 TempId（仅编辑态，不落库；删除拆分行时加回原行）</summary>
    public int? SplitFromTempId { get; set; }
    /// <summary>编辑页勾选，不落库</summary>
    public bool Selected { get; set; }
    public string? Customer_outer_box_logo { get; set; }
    public string? Tuo_Mark { get; set; }
    public string? Tuo_Mark_url { get; set; }
    public string? Box_label { get; set; }
    public string? Box_label_url { get; set; }
    public string? Box_label2 { get; set; }
    public string? Box_label_url2 { get; set; }
    public string? nameplate_url { get; set; }
    public string? nameplate_type { get; set; }
    public string? nameplate_url2 { get; set; }
    public string? nameplate_type2 { get; set; }
    public string? Customer_destination_country { get; set; }
}

public static class CustomsAndPackingFilterFields
{
    public static readonly (string Key, string Label)[] All =
    [
        ("客户编码", "客户编码"),
        ("客户名称", "客户名称"),
        ("PI合同", "PI合同"),
        ("SAP订单号", "SAP订单号"),
        ("创建人", "创建人"),
        ("创建时间", "创建时间"),
    ];

    public static bool IsDateFilter(string? filterColumn)
        => string.Equals(filterColumn, "创建时间", StringComparison.Ordinal);
}
