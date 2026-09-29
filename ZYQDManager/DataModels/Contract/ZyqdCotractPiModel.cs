using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZYQDManager.DataModels;

/// <summary>客户/报关 PI、客户 CI，表 T_ZYQD_Cotract_PI（抬头+明细同表，按 Guid 分组）</summary>
[Table("T_ZYQD_Cotract_PI")]
public class ZyqdCotractPiModel
{
    [Key]
    public int Id { get; set; }

    [Column("Guid")]
    public string? DocumentGuid { get; set; }

    public string? Status { get; set; }
    public string? Person { get; set; }
    public string? Cotract_PI_type { get; set; }

    public string? BILL_TO_info1 { get; set; }
    public string? BILL_TO_info2 { get; set; }
    public string? BILL_TO_info3 { get; set; }
    public string? BILL_TO_info4 { get; set; }
    public string? SHIP_TO_info1 { get; set; }
    public string? SHIP_TO_info2 { get; set; }
    public string? SHIP_TO_info3 { get; set; }
    public string? SHIP_TO_info4 { get; set; }

    public string? materialNo { get; set; }
    public string? materialName { get; set; }
    public string? ClientNo { get; set; }
    public string? ClientName { get; set; }
    public string? INVOICE_NO { get; set; }
    public string? BILL_TO_DATE { get; set; }
    public string? BILL_TO_PO_REF { get; set; }
    public string? BILL_TO_FOB { get; set; }

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

    public double unit { get; set; }
    public double price { get; set; }
    public double qty { get; set; }
    public double ext { get; set; }
    public double TOTAL { get; set; }
    public string? currency_unit { get; set; }
    public double Payment_ratio { get; set; }
    public double Payment_ratio_Ext { get; set; }
    public double balance_before_delivery { get; set; }
    public double payment_method { get; set; }
    public string? email { get; set; }

    [Column("DATE")]
    public string? SignDate { get; set; }

    public string? Notes { get; set; }
    public string? Estimated_TimeofCompletion { get; set; }

    public string? var1 { get; set; }
    public string? var2 { get; set; }
    public string? var3 { get; set; }
    public string? var4 { get; set; }
    public string? var5 { get; set; }
    /// <summary>订单类型：标准订单 / 贸易订单（1300）等</summary>
    public string? var6 { get; set; }
    public string? var7 { get; set; }
    public string? var8 { get; set; }
    public string? var9 { get; set; }
    public string? var10 { get; set; }
    public string? var11 { get; set; }
    public string? var12 { get; set; }
    public string? var13 { get; set; }
    public string? var14 { get; set; }
    public string? var15 { get; set; }
    public string? var16 { get; set; }
    public string? var17 { get; set; }
    public string? var18 { get; set; }
    public string? var19 { get; set; }
    public string? var20 { get; set; }
    public string? ClientName_old { get; set; }
    public double percent { get; set; }
    public double Insurance_premium { get; set; }
    public string? Shipping_Fee { get; set; }

    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? CreatePerson { get; set; }
    public string? UpdatePerson { get; set; }
    public string? Remark { get; set; }
    public string? deleteSigh { get; set; }
    public DateTime? deleteTime { get; set; }
    public string? deletePerson { get; set; }
}
