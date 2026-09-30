using ZYQDManager.DataModels.Quotation;

namespace ZYQDManager.DataDtos;

public sealed class QuotationQueryDto
{
    public string? FilterColumn { get; set; }
    public string? Keyword { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public string? SortLabel { get; set; }
    public bool SortDescending { get; set; } = true;
}

public sealed class QuotationListItemDto
{
    public int Id { get; set; }
    public string Guid { get; set; } = "";
    public string? Quota_No { get; set; }
    public string? ClientCode { get; set; }
    public string? ClientName { get; set; }
    public string? DATE { get; set; }
    public string? personId { get; set; }
    public DateTime? createTime { get; set; }
    public string? currencyUnit { get; set; }
    public string? signature { get; set; }
}

public sealed class QuotationDocumentDto
{
    public ZyqdQuotationMainModel Main { get; set; } = new();
    public List<QuotationDetailLineDto> Details { get; set; } = new();
}

public sealed class QuotationDetailLineDto
{
    public string Uid { get; set; } = System.Guid.NewGuid().ToString("N");
    public int Id { get; set; }
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

    // 选型后从基础数据带出，仅展示（不单独落库规格列）
    public string? Rated_torque { get; set; }
    public string? Rated_speed { get; set; }
    public string? Power_supply { get; set; }
    public string? Rated_Power { get; set; }
    public string? Rated_Current { get; set; }
    public string? Noise_dBA { get; set; }
    public string? Motor_Length { get; set; }
    public string? Tube_diameter { get; set; }
    public string? Battery_Capacity { get; set; }
    public string? Exclude_text { get; set; }
    public string? Remote_controller_type { get; set; }
    public string? Battery_Type { get; set; }
    public string? Radio_Frequency { get; set; }
    public string? Working_Temperature { get; set; }
    public string? NoOfChannels { get; set; }
    public string? adaptor { get; set; }
    public string? Rated_Voltage { get; set; }
    public string? Size { get; set; }
    public string? No_Of_Actuator { get; set; }
    public string? Output_Control { get; set; }
    public string? Sensor { get; set; }
    public string? Protection_Degree { get; set; }
    public string? Max_Loading { get; set; }
    public string? Max_Stroke { get; set; }
    public string? Protection_Degree2 { get; set; }
    public string? Noise { get; set; }

    /// <summary>金额 = 数量 × 单价 × (100 − 折扣%) / 100；折扣 0 表示无折扣。</summary>
    public void RecalcAmount()
    {
        var qty = Qty ?? 0m;
        var price = Price ?? 0m;
        var discount = Discount ?? 0m;
        if (qty == 0m || price == 0m)
        {
            Amount = 0m;
            return;
        }

        Amount = Math.Round(qty * price * (100m - discount) / 100m, 2);
    }

    public void ApplyBase(QuotationBaseListItemDto? b)
    {
        if (b is null)
        {
            BaseId = null;
            Part_No = null;
            Quotation_logo = null;
            ClearSpecs();
            return;
        }

        BaseId = b.Id;
        Part_No = b.Part_No;
        Quotation_logo = b.Quotation_logo;
        Rated_torque = b.Rated_torque;
        Rated_speed = b.Rated_speed;
        Power_supply = b.Power_supply;
        Rated_Power = b.Rated_Power;
        Rated_Current = b.Rated_Current;
        Noise_dBA = b.Noise_dBA;
        Motor_Length = b.Motor_Length;
        Tube_diameter = b.Tube_diameter;
        Battery_Capacity = b.Battery_Capacity;
        Exclude_text = b.Exclude_text;
        Remote_controller_type = b.Remote_controller_type;
        Battery_Type = b.Battery_Type;
        Radio_Frequency = b.Radio_Frequency;
        Working_Temperature = b.Working_Temperature;
        NoOfChannels = b.NoOfChannels;
        adaptor = b.adaptor;
        Rated_Voltage = b.Rated_Voltage;
        Size = b.Size;
        No_Of_Actuator = b.No_Of_Actuator;
        Output_Control = b.Output_Control;
        Sensor = b.Sensor;
        Protection_Degree = b.Protection_Degree;
        Max_Loading = b.Max_Loading;
        Max_Stroke = b.Max_Stroke;
        Protection_Degree2 = b.Protection_Degree2;
        Noise = b.Noise;
    }

    private void ClearSpecs()
    {
        Rated_torque = Rated_speed = Power_supply = Rated_Power = Rated_Current = null;
        Noise_dBA = Motor_Length = Tube_diameter = Battery_Capacity = Exclude_text = null;
        Remote_controller_type = Battery_Type = Radio_Frequency = Working_Temperature = NoOfChannels = null;
        adaptor = null;
        Rated_Voltage = Size = No_Of_Actuator = Output_Control = Sensor = Protection_Degree = null;
        Max_Loading = Max_Stroke = Protection_Degree2 = Noise = null;
    }
}

public sealed class QuotationPartOptionDto
{
    public int Id { get; set; }
    public string Part_No { get; set; } = "";
    public string? Label { get; set; }
}

public static class QuotationFilterFields
{
    public static readonly (string Key, string Label)[] All =
    [
        ("Quota_No", "报价单号"),
        ("ClientCode", "客户编号"),
        ("ClientName", "客户名称"),
        ("Attn", "联系人"),
        ("personId", "创建人"),
    ];
}
