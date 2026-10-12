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
    public const int MaxTiers = 4;

    public string Uid { get; set; } = System.Guid.NewGuid().ToString("N");
    public int Id { get; set; }
    public string ItemType { get; set; } = QuotationBaseTypes.Motor;
    public int? BaseId { get; set; }
    public string? Part_No { get; set; }
    /// <summary>仅界面展示，从基础数据带出，不落库。</summary>
    public string? Quotation_logo { get; set; }

    /// <summary>界面展示的阶梯档数（1–4），默认 1。</summary>
    public int TierCount { get; set; } = 1;

    // 阶梯 1（兼容原单档字段）
    public decimal? Qty { get; set; }
    public decimal? Price { get; set; }
    public decimal? Discount { get; set; }
    public decimal? Amount { get; set; }
    // 阶梯 2–4
    public decimal? Qty2 { get; set; }
    public decimal? Price2 { get; set; }
    public decimal? Discount2 { get; set; }
    public decimal? Amount2 { get; set; }
    public decimal? Qty3 { get; set; }
    public decimal? Price3 { get; set; }
    public decimal? Discount3 { get; set; }
    public decimal? Amount3 { get; set; }
    public decimal? Qty4 { get; set; }
    public decimal? Price4 { get; set; }
    public decimal? Discount4 { get; set; }
    public decimal? Amount4 { get; set; }

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

    /// <summary>各档金额合计（用于小计 / 明细总额）。</summary>
    public decimal TotalAmount =>
        (Amount ?? 0m) + (Amount2 ?? 0m) + (Amount3 ?? 0m) + (Amount4 ?? 0m);

    /// <summary>金额 = 数量 × 单价 × (100 − 折扣%) / 100；折扣 0 表示无折扣。</summary>
    public void RecalcAmount() => RecalcAllAmounts();

    public void RecalcAllAmounts()
    {
        for (var t = 1; t <= MaxTiers; t++)
            SetAmount(t, CalcAmount(GetQty(t), GetPrice(t), GetDiscount(t)));
    }

    public void RecalcTier(int tier)
        => SetAmount(tier, CalcAmount(GetQty(tier), GetPrice(tier), GetDiscount(tier)));

    public static decimal CalcAmount(decimal? qty, decimal? price, decimal? discount)
    {
        var q = qty ?? 0m;
        var p = price ?? 0m;
        var d = discount ?? 0m;
        if (q == 0m || p == 0m) return 0m;
        return Math.Round(q * p * (100m - d) / 100m, 2);
    }

    public decimal? GetQty(int tier) => tier switch
    {
        1 => Qty, 2 => Qty2, 3 => Qty3, 4 => Qty4, _ => null
    };

    public decimal? GetPrice(int tier) => tier switch
    {
        1 => Price, 2 => Price2, 3 => Price3, 4 => Price4, _ => null
    };

    public decimal? GetDiscount(int tier) => tier switch
    {
        1 => Discount, 2 => Discount2, 3 => Discount3, 4 => Discount4, _ => null
    };

    public decimal? GetAmount(int tier) => tier switch
    {
        1 => Amount, 2 => Amount2, 3 => Amount3, 4 => Amount4, _ => null
    };

    public void SetQty(int tier, decimal? v)
    {
        switch (tier)
        {
            case 1: Qty = v; break;
            case 2: Qty2 = v; break;
            case 3: Qty3 = v; break;
            case 4: Qty4 = v; break;
        }
    }

    public void SetPrice(int tier, decimal? v)
    {
        switch (tier)
        {
            case 1: Price = v; break;
            case 2: Price2 = v; break;
            case 3: Price3 = v; break;
            case 4: Price4 = v; break;
        }
    }

    public void SetDiscount(int tier, decimal? v)
    {
        switch (tier)
        {
            case 1: Discount = v; break;
            case 2: Discount2 = v; break;
            case 3: Discount3 = v; break;
            case 4: Discount4 = v; break;
        }
    }

    public void SetAmount(int tier, decimal? v)
    {
        switch (tier)
        {
            case 1: Amount = v; break;
            case 2: Amount2 = v; break;
            case 3: Amount3 = v; break;
            case 4: Amount4 = v; break;
        }
    }

    public void AddTier()
    {
        if (TierCount >= MaxTiers) return;
        TierCount++;
        SetDiscount(TierCount, 0m);
        SetQty(TierCount, 0m);
        SetPrice(TierCount, 0m);
        SetAmount(TierCount, 0m);
    }

    public void RemoveLastTier()
    {
        if (TierCount <= 1) return;
        SetQty(TierCount, null);
        SetPrice(TierCount, null);
        SetDiscount(TierCount, null);
        SetAmount(TierCount, null);
        TierCount--;
    }

    /// <summary>按已填档位推断展示档数（至少 1）。</summary>
    public void SyncTierCountFromData()
    {
        var n = 1;
        for (var t = MaxTiers; t >= 2; t--)
        {
            if ((GetQty(t) ?? 0m) != 0m
                || (GetPrice(t) ?? 0m) != 0m
                || (GetDiscount(t) ?? 0m) != 0m
                || (GetAmount(t) ?? 0m) != 0m)
            {
                n = t;
                break;
            }
        }

        TierCount = n;
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
