namespace ZYQDManager.DataModels;

/// <summary>尾款支付方式字典 T_ZYQD_Balance_PayType</summary>
public class ZyqdBalancePayTypeModel
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public string? UpdatePerson { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? deleteSigh { get; set; }
    public DateTime? deleteTime { get; set; }
    public string? deletePerson { get; set; }
}
