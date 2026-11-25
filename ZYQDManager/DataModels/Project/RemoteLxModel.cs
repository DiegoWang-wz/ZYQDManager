namespace ZYQDManager.DataModels;

/// <summary>
/// 遥控器立项单 T_ZYQD_Remote_LX（同 ProjectGuid 多版本）。
/// 字段严格对应《遥控器立项单》模板的 17 项 + 表头，只保留遥控器自己的字段；
/// 通用流程字段（状态/版本/提交/审计）与电机立项单同构，但库表完全独立。
/// </summary>
public class RemoteLxModel
{
    // ===== 通用流程字段（与项目管理流程一致，表独立）=====
    public int Id { get; set; }
    public string ProjectGuid { get; set; } = "";
    public string DocGuid { get; set; } = "";
    public string VersionNo { get; set; } = "vn";
    public bool IsDraft { get; set; } = true;
    public DateTime? ForwardedAt { get; set; }
    public string? Status { get; set; }

    /// <summary>提交时指定的研发工程师，立项单步骤完成的标志是 SubmitTime 非空。</summary>
    public string? EngineerCode { get; set; }
    public DateTime? SubmitTime { get; set; }

    // ===== 模板表头 =====
    public string? Customer { get; set; }
    public string? DemandPerson { get; set; }
    public DateTime? TimeToRequest { get; set; }
    public DateTime? RequiredCompletionTime { get; set; }
    public string? CustomerPartCode { get; set; }

    // ===== 模板 17 项参数 =====
    /// <summary>1 型号 Model No.</summary>
    public string? MotorModelSpec { get; set; }
    /// <summary>2 描述 Description</summary>
    public string? Description { get; set; }
    /// <summary>3 遥控器尺寸 Remote Size</summary>
    public string? RemoteSize { get; set; }
    /// <summary>4 支架尺寸 Bracket Size</summary>
    public string? BracketSize { get; set; }
    /// <summary>5 遥控器重量 Remote Weight</summary>
    public string? RemoteWeight { get; set; }
    /// <summary>6 遥控器颜色 Remote Color</summary>
    public string? RemoteColor { get; set; }
    /// <summary>7 电池型号 Battery Type</summary>
    public string? BatteryType { get; set; }
    /// <summary>8 充电口要求 Charging port for chargable remote</summary>
    public string? ChargingMethod { get; set; }
    public string? ChargingMethodNo { get; set; }
    /// <summary>9 发射功率 Transmit Power</summary>
    public string? TransmitPower { get; set; }
    /// <summary>10 频率 Radio Frequency</summary>
    public string? RemoteFrequency { get; set; }
    public string? RemoteFrequencyOther { get; set; }
    /// <summary>11 遥控技术 Radio Mode</summary>
    public string? RemoteTechnology { get; set; }
    /// <summary>12 遥控协议 Protocol</summary>
    public string? RemoteProtocol { get; set; }
    public string? RemoteProtocolOther { get; set; }
    /// <summary>13 通道数量 Channel Quantity</summary>
    public string? ChannelQuantity { get; set; }
    /// <summary>14 工作温度 Working Temperature</summary>
    public string? WorkingTemperature { get; set; }
    /// <summary>15 传输距离 Transmit Distance（室内/室外）</summary>
    public string? RemoteDistanceIndoor { get; set; }
    public string? RemoteDistanceOutdoor { get; set; }
    /// <summary>16 应用场景 Application</summary>
    public string? Application { get; set; }
    /// <summary>17 防水等级 IP Degree</summary>
    public string? IpLevel { get; set; }
    public string? IpLevelOther { get; set; }

    // ===== 审计 =====
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? CreatePersonCode { get; set; }
    public string? UpdatePersonCode { get; set; }
    public string? Remark { get; set; }
    public string DeleteFlag { get; set; } = "";
    public DateTime? DeleteTime { get; set; }
    public string? DeletePersonCode { get; set; }
}
