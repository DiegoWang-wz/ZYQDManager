namespace ZYQDManager.DataModels;

/// <summary>客户付款方式 T_ZYQD_Client_Payment</summary>
public class ZyqdClientPaymentModel
{
    public int Id { get; set; }
    public string? ClientNo { get; set; }
    /// <summary>定金百分比，库中为字符串（如 30 或 30%）。</summary>
    public string? Deposit { get; set; }
    public int BalancePayTypeId { get; set; }
    public ZyqdBalancePayTypeModel? BalancePayType { get; set; }
    public string? UpdatePerson { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? deleteSigh { get; set; }
}
