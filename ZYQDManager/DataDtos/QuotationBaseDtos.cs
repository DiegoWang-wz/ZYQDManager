namespace ZYQDManager.DataDtos;

public sealed class QuotationBaseQueryDto
{
    public string ItemType { get; set; } = "Motor";
    public string? FilterColumn { get; set; }
    public string? Keyword { get; set; }
    public string? SortLabel { get; set; }
    public bool SortDescending { get; set; } = true;
}

/// <summary>统一列表行：按 ItemType 只填对应规格列。</summary>
public sealed class QuotationBaseListItemDto
{
    public int Id { get; set; }
    public string ItemType { get; set; } = "";
    public string? Part_No { get; set; }
    public string? Quotation_logo { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? CreatePerson { get; set; }
    public string? UpdatePerson { get; set; }
    public string? Remark { get; set; }

    // Motor
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

    // Remote
    public string? Remote_controller_type { get; set; }
    public string? Battery_Type { get; set; }
    public string? Radio_Frequency { get; set; }
    public string? Working_Temperature { get; set; }
    public string? NoOfChannels { get; set; }

    // Accessory
    public string? adaptor { get; set; }

    // CB
    public string? Rated_Voltage { get; set; }
    public string? Size { get; set; }
    public string? No_Of_Actuator { get; set; }
    public string? Output_Control { get; set; }
    public string? Sensor { get; set; }
    public string? Protection_Degree { get; set; }

    // PR
    public string? Max_Loading { get; set; }
    public string? Max_Stroke { get; set; }
    public string? Protection_Degree2 { get; set; }
    public string? Noise { get; set; }
}

public static class QuotationBaseFilterFields
{
    public static (string Key, string Label)[] ForType(string itemType) => itemType switch
    {
        "Remote" =>
        [
            ("Part_No", "型号"),
            ("Remote_controller_type", "遥控器型号"),
            ("Battery_Type", "电池型号"),
            ("Radio_Frequency", "频率"),
            ("CreatePerson", "创建人"),
            ("Remark", "备注"),
        ],
        "Accessory" =>
        [
            ("Part_No", "型号"),
            ("adaptor", "配件名"),
            ("CreatePerson", "创建人"),
            ("Remark", "备注"),
        ],
        "CB" =>
        [
            ("Part_No", "型号"),
            ("Rated_Voltage", "额定电压"),
            ("Size", "尺寸"),
            ("Rated_Power", "额定功率"),
            ("CreatePerson", "创建人"),
            ("Remark", "备注"),
        ],
        "PR" =>
        [
            ("Part_No", "型号"),
            ("Max_Loading", "最大负载"),
            ("Max_Stroke", "最大行程"),
            ("Noise", "噪音"),
            ("CreatePerson", "创建人"),
            ("Remark", "备注"),
        ],
        _ =>
        [
            ("Part_No", "型号"),
            ("Rated_torque", "额定扭矩"),
            ("Rated_speed", "额定转速"),
            ("Power_supply", "电源"),
            ("Rated_Power", "额定功率"),
            ("CreatePerson", "创建人"),
            ("Remark", "备注"),
        ],
    };
}
