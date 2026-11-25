using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;

namespace ZYQDManager.Utilities;

/// <summary>
/// 遥控器立项单 → 规格书 的字段映射（遥控器独立口径，与电机无关）。
/// 提交立项单自动建规格书草稿、规格书页「重新带入立项单」两处都走这里。
/// 只填空字段（SetIfEmpty），研发已填过的不覆盖。
///
/// 立项单里多数是下拉/自由文本，规格书参数表多数是复选框，
/// 因此这里统一走 <see cref="SetOption"/>：能对上模板选项就写选项，
/// 对不上（自定义内容）就落到同一行的「其它」格，保证内容不丢。
/// </summary>
public static class RemoteGgFromLx
{
    /// <summary>表头字段（客户、需求人、日期等）从立项单搬过来。</summary>
    public static void FillHead(RemoteGgModel gg, RemoteLxModel lx)
    {
        if (string.IsNullOrWhiteSpace(gg.ProjectGuid)) gg.ProjectGuid = lx.ProjectGuid;
        gg.SourceLxDocGuid = lx.DocGuid;
        if (string.IsNullOrWhiteSpace(gg.VersionNo) || gg.VersionNo == "vn")
            gg.VersionNo = lx.VersionNo;

        gg.Customer = string.IsNullOrWhiteSpace(gg.Customer) ? lx.Customer : gg.Customer;
        gg.DemandPerson = lx.DemandPerson;
        gg.CustomerPartCode = lx.CustomerPartCode;
        gg.TimeToRequest = lx.TimeToRequest ?? lx.CreateTime;
        gg.RequiredCompletionTime = lx.RequiredCompletionTime;
    }

    /// <summary>正文参数（RemoteDocExtra.Fields）从立项单搬过来，行序与《遥控器规格书.xlsx》一致。</summary>
    public static void Fill(RemoteDocExtra extra, RemoteLxModel lx)
    {
        // 表头
        SetIfEmpty(extra, RemoteDocKeys.ModelNo, lx.MotorModelSpec);
        SetIfEmpty(extra, RemoteDocKeys.ProductName, lx.Description);
        SetIfEmpty(extra, RemoteDocKeys.Customer, lx.Customer);
        SetIfEmpty(extra, RemoteDocKeys.PartCode, lx.CustomerPartCode);

        // 一、产品功性能要求及参数规格表
        SetIfEmpty(extra, RemoteDocKeys.RemoteSize, lx.RemoteSize);
        SetIfEmpty(extra, RemoteDocKeys.BracketSize, lx.BracketSize);
        SetIfEmpty(extra, RemoteDocKeys.RemoteWeight, lx.RemoteWeight);
        SetIfEmpty(extra, RemoteDocKeys.RemoteColor, lx.RemoteColor);

        SetOption(extra, RemoteDocKeys.BatteryType, RemoteDocKeys.BatteryTypeOther, lx.BatteryType, BatteryTypeOptions);
        SetOption(extra, RemoteDocKeys.RechargePort, RemoteDocKeys.RechargePortOther, lx.ChargingMethod, RechargePortOptions);
        SetIfEmpty(extra, RemoteDocKeys.RechargePortOther, lx.ChargingMethodNo);

        SetIfEmpty(extra, RemoteDocKeys.TransmitPower, lx.TransmitPower);

        SetOption(extra, RemoteDocKeys.RadioMode, null, lx.RemoteTechnology, RadioModeOptions);
        SetOption(extra, RemoteDocKeys.RadioFrequency, RemoteDocKeys.RfOther, lx.RemoteFrequency, RadioFreqOptions);
        SetIfEmpty(extra, RemoteDocKeys.RfOther, lx.RemoteFrequencyOther);

        SetOption(extra, RemoteDocKeys.Protocol, "protocolOther", lx.RemoteProtocol, ProtocolOptions);
        SetIfEmpty(extra, "protocolOther", lx.RemoteProtocolOther);

        SetOption(extra, RemoteDocKeys.ChannelQuantity, null, lx.ChannelQuantity, ChannelOptions);

        SetIfEmpty(extra, RemoteDocKeys.RfDistanceIndoor, lx.RemoteDistanceIndoor);
        SetIfEmpty(extra, RemoteDocKeys.RfDistanceOutdoor, lx.RemoteDistanceOutdoor);

        // 立项单的「应用场景」是自由文本，规格书对应行是 10 个复选 + 补充格
        SetOption(extra, RemoteDocKeys.Applications, RemoteDocKeys.ApplicationsOther, lx.Application, ApplicationOptions);

        // 二、产品使用工况
        SetOption(extra, RemoteDocKeys.IpDegree, RemoteDocKeys.IpOther, lx.IpLevel, IpOptions);
        SetIfEmpty(extra, RemoteDocKeys.IpOther, lx.IpLevelOther);
        SetIfEmpty(extra, RemoteDocKeys.WorkingTemp, lx.WorkingTemperature);
    }

    private static void SetIfEmpty(RemoteDocExtra extra, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!extra.Fields.ContainsKey(key) || string.IsNullOrWhiteSpace(extra.Fields[key]))
            extra.Fields[key] = value!.Trim();
    }

    /// <summary>先按模板选项找，找不到就写进同一行的「其它」格（otherKey 为空时仍写主键）。</summary>
    private static void SetOption(RemoteDocExtra extra, string key, string? otherKey, string? value, string[] options)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var text = value.Trim();

        var hit = options.FirstOrDefault(o => string.Equals(o, text, StringComparison.OrdinalIgnoreCase))
                  ?? options.FirstOrDefault(o => text.Contains(o, StringComparison.OrdinalIgnoreCase));

        if (hit is not null) SetIfEmpty(extra, key, hit);
        else if (!string.IsNullOrWhiteSpace(otherKey)) SetIfEmpty(extra, otherKey, text);
        else SetIfEmpty(extra, key, text);
    }

    // 与 RemoteSpecSections.razor 里的选项数组保持一致
    private static readonly string[] BatteryTypeOptions =
        ["AAA Alkaline Battery", "CR2450 Lithium Battery", "CR2430 Lithium Battery", "CR2032 Lithium Battery", "Rechargeable Battery"];

    private static readonly string[] RechargePortOptions = ["None", "Micro-USB", "Type-C"];

    private static readonly string[] RadioModeOptions = ["单向 Single-way", "双向 Bi-directional"];

    private static readonly string[] RadioFreqOptions = ["433.92 MHz", "447.x MHz", "868.x MHz"];

    private static readonly string[] ProtocolOptions = ["Standard", "Customized"];

    private static readonly string[] ChannelOptions = ["1-channel", "2-channels", "6-channels", "16-channels"];

    private static readonly string[] ApplicationOptions =
    [
        "卷帘 Roller shade", "蜂巢帘 Honeycomb Shade", "香格里拉帘 Shangri-la Blinds",
        "罗马帘 Roman Shade (light weight)", "木百叶帘 Wooden Venetian Blinds", "斑马帘 Zebra Shade",
        "遮阳篷 Ceiling Blinds", "铝百叶帘 Aluminum Venetian Blinds", "投影屏 Projection screen", "开合帘 Curtain"
    ];

    private static readonly string[] IpOptions = ["IP40", "IP44", "IP45", "IP66"];
}
