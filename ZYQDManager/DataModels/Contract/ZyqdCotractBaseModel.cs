namespace ZYQDManager.DataModels;

/// <summary>合同基础数据 T_ZYQD_Cotract_Base（从老库拷入 Sunlight_Management_BU）</summary>
public class ZyqdCotractBaseModel
{
    public int Id { get; set; }

    public string? Customer_outer_box_logo { get; set; }
    public string? Tuo_Mark { get; set; }
    public string? Tuo_Mark_url { get; set; }
    public string? Customer_destination_country { get; set; }
    public string? ClientNo { get; set; }
    public string? ClientName { get; set; }
    public string? materialNo { get; set; }
    public string? materialName { get; set; }
    public string? materialType { get; set; }
    public string? materialModel { get; set; }
    public string? materiaSpecs { get; set; }
    public string? materiaPowered { get; set; }
    public string? materiaColor { get; set; }
    public string? materiaCust { get; set; }
    public string? materiaCustLogo { get; set; }
    public string? RadFreq { get; set; }
    public string? inBox { get; set; }
    public string? MstCtn { get; set; }
    public string? AI { get; set; }
    public string? UNIT { get; set; }
    public double price { get; set; }
    public string? Box_label { get; set; }
    public string? Box_label_url { get; set; }
    public string? Box_label2 { get; set; }
    public string? Box_label_url2 { get; set; }
    public string? Client_poNo { get; set; }
    public string? Client_Model { get; set; }
    public string? nameplate_url { get; set; }
    public string? nameplate_url2 { get; set; }
    public string? SAP_order_NO { get; set; }
    public string? SAP_Material_NO { get; set; }
    public string? SAP_Material_Name { get; set; }
    public string? Packing_method { get; set; }
    public string? Packing_method_unit { get; set; }
    public string? Carton_Size { get; set; }
    public string? Pallet_Size { get; set; }
    public double Net_weight { get; set; }
    public double gross_weight { get; set; }
    public string? Customer_Manager { get; set; }
    public string? palletizing_method { get; set; }
    public string? palletizing_method_unit { get; set; }
    public string? currency_unit { get; set; }
    public string? Quantity_unit { get; set; }
    public string? price_unit { get; set; }
    public string? Notes { get; set; }
    public string? var1 { get; set; }
    public string? var2 { get; set; }
    public string? var3 { get; set; }
    public string? var4 { get; set; }
    public string? var5 { get; set; }
    public string? var6 { get; set; }
    public string? var7 { get; set; }
    public string? var8 { get; set; }
    public string? var9 { get; set; }
    public string? var10 { get; set; }

    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? CreatePerson { get; set; }
    public string? UpdatePerson { get; set; }
    public string? Remark { get; set; }
    public string? deleteSigh { get; set; }
    public DateTime? deleteTime { get; set; }
    public string? deletePerson { get; set; }
}
