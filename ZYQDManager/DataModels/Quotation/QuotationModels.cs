namespace ZYQDManager.DataModels.Quotation;

/// <summary>报价单抬头（对齐旧 T_ZYQD_Quotation_main）。</summary>
public sealed class ZyqdQuotationMainModel
{
    public int Id { get; set; }
    public string guid { get; set; } = "";
    public string? ClientCode { get; set; }
    public string? ClientName { get; set; }
    public string? Address { get; set; }
    public string? Tel { get; set; }
    public string? Mail { get; set; }
    public string? Attn { get; set; }
    public string? Delivery_Term { get; set; }
    public string? Payment_Term { get; set; }
    public string? Quotation_validity { get; set; }
    public string? Note { get; set; }
    public string? signature { get; set; }
    public string? signature_url { get; set; }
    public string? Quota_No { get; set; }
    public string? DATE { get; set; }
    public string? personId { get; set; }
    public string? rate { get; set; }
    public DateTime? createTime { get; set; }
    public string? currencyUnit { get; set; }
}

/// <summary>统一报价明细：物料 + 数量/单价/折扣/金额（单档）+ 可选备注。</summary>
public sealed class ZyqdQuotationDetailModel
{
    public int Id { get; set; }
    public string main_guid { get; set; } = "";
    public string ItemType { get; set; } = QuotationBaseTypes.Motor;
    public int? BaseId { get; set; }
    public string? Part_No { get; set; }
    public string? Quotation_logo { get; set; }
    public decimal? Qty { get; set; }
    public decimal? Price { get; set; }
    public decimal? Discount { get; set; }
    public decimal? Amount { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }
    public DateTime? createTime { get; set; }
}
