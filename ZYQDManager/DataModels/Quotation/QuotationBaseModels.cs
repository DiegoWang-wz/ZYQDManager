namespace ZYQDManager.DataModels.Quotation;

/// <summary>五张报价基础表公共字段。</summary>
public abstract class QuotationBaseEntity
{
    public int Id { get; set; }
    public string? Part_No { get; set; }
    public string? Quotation_logo { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? CreatePerson { get; set; }
    public string? UpdatePerson { get; set; }
    public string? Remark { get; set; }
    public string? deleteSigh { get; set; }
    public DateTime? deleteTime { get; set; }
    public string? deletePerson { get; set; }
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
}

public sealed class ZyqdQuotationBaseMotorModel : QuotationBaseEntity
{
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
}

public sealed class ZyqdQuotationBaseRemoteModel : QuotationBaseEntity
{
    public string? Remote_controller_type { get; set; }
    public string? Battery_Type { get; set; }
    public string? Radio_Frequency { get; set; }
    public string? Working_Temperature { get; set; }
    public string? NoOfChannels { get; set; }
}

public sealed class ZyqdQuotationBaseAccessoryModel : QuotationBaseEntity
{
    public string? adaptor { get; set; }
}

public sealed class ZyqdQuotationBaseCbModel : QuotationBaseEntity
{
    public string? Rated_Voltage { get; set; }
    public string? Size { get; set; }
    public string? Rated_Power { get; set; }
    public string? No_Of_Actuator { get; set; }
    public string? Output_Control { get; set; }
    public string? Sensor { get; set; }
    public string? Protection_Degree { get; set; }
}

public sealed class ZyqdQuotationBasePrModel : QuotationBaseEntity
{
    public string? Max_Loading { get; set; }
    public string? Max_Stroke { get; set; }
    public string? Protection_Degree2 { get; set; }
    public string? Noise { get; set; }
}
