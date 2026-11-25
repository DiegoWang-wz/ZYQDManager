using System.Reflection;
using System.Text.Json;
using ZYQDManager.DataModels;

namespace ZYQDManager.Utilities;

/// <summary>
/// 一条修改点：哪个字段、改之前是什么、改之后是什么。
/// 存进版本表 ChangeDiff（JSON），研发工程师编写规格书时按这个列表展示。
/// </summary>
public sealed class RemoteLxChange
{
    public string Field { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    /// <summary>空值统一显示成「（空）」，页面上不会出现一列空白。</summary>
    public string OldText => string.IsNullOrWhiteSpace(OldValue) ? "（空）" : OldValue!;
    public string NewText => string.IsNullOrWhiteSpace(NewValue) ? "（空）" : NewValue!;
}

/// <summary>
/// 立项单字段级差异：修改前后各存一份快照，逐字段比出「改了哪些地方」。
///
/// 用反射按白名单表格比，好处是加字段只要在表里补一行标签，
/// 不用再写一遍取值 / 比较 / 格式化的样板代码；系统字段（Id/DocGuid/状态/审计列）
/// 不在表里，自然不会进差异。
/// </summary>
public static class RemoteLxDiff
{
    /// <summary>参与比较的业务字段与中文标签，顺序与页面表单一致。</summary>
    private static readonly (string Property, string Label)[] Fields =
    [
        ("Customer", "客户"),
        ("CustomerPartCode", "客户产品编号"),
        ("DemandPerson", "需求人"),
        ("MotorModelSpec", "捷昌型号"),
        ("Description", "产品描述"),
        ("TimeToRequest", "提出需求日期"),
        ("RequiredCompletionTime", "要求完成日期"),
        ("InputVoltage", "输入电压"),
        ("InputVoltageOther", "其它电压"),
        ("RatedCurrentNoLoad", "空载电流"),
        ("RatedCurrentFullLoad", "负载电流"),
        ("RatedTorque", "额定扭矩"),
        ("RatedLevelSpeed", "额定等级转速"),
        ("DefaultRunningSpeed", "出厂默认速度"),
        ("RatedPower", "额定功率"),
        ("OperatingNoise", "运行噪音"),
        ("FastCharge", "快充"),
        ("MaxRunningTime", "持续运行时间"),
        ("MotorHead", "遥控器头"),
        ("Shaft", "输出轴"),
        ("OuterTubeDiameter", "外管直径"),
        ("OuterTubeLengthLimit", "外管长度限制"),
        ("OuterTubeColor", "外管颜色"),
        ("MotorWeight", "遥控器重量"),
        ("MotorLength", "遥控器长度"),
        ("OperationMethod", "运行方式"),
        ("ProgramCode", "程序代码"),
        ("CrownAndDrive", "冠齿轮 / 驱动"),
        ("RemoteController", "遥控器"),
        ("ChargingMethod", "充电方式"),
        ("ChargingMethodNo", "充电型号"),
        ("JstPlugV", "JST 插头电压"),
        ("JstPlugA", "JST 插头电流"),
        ("LeadWireSpecification", "遥控器引出线规格"),
        ("CordCertified", "电线认证"),
        ("CordColor", "电线颜色"),
        ("CordColorOther", "其它电线颜色"),
        ("CordType", "电线类型"),
        ("CordTypeOther", "其它电线类型"),
        ("CordPlug", "电线插头"),
        ("CordLength", "电线长度"),
        ("CordOtherRequirements", "电线其它要求"),
        ("WireLength", "线长"),
        ("RemoteFrequency", "遥控频率"),
        ("RemoteFrequencyOther", "其它遥控频率"),
        ("RemoteDistance", "遥控距离"),
        ("RemoteTechnology", "遥控技术"),
        ("RemoteProtocol", "遥控协议"),
        ("RemoteProtocolOther", "其它遥控协议"),
        ("MotorFunction", "功能"),
        ("SmartModule", "智能模块"),
        ("SmartModuleOther", "其它智能模块"),
        ("SignalReceivingDistance", "信号接收距离"),
        ("SignalReceivingProtocol", "信号接收协议"),
        ("BuiltInLithiumWhether", "内置锂电池"),
        ("BuiltInLithiumBatteryCell", "电池电芯"),
        ("BuiltInLithiumBattery2", "备用电池"),
        ("InputPower", "输入电源"),
        ("IpLevel", "IP 等级"),
        ("IpLevelOther", "其它 IP 等级"),
        ("UsageEnv", "使用环境"),
        ("UsageEnvOther", "其它使用环境"),
        ("Application1", "应用场景 1"),
        ("Application2", "应用场景 2"),
        ("Application3", "应用场景 3"),
        ("Application4", "应用场景 4"),
        ("Application5", "应用场景 5"),
        ("Application6", "应用场景 6"),
        ("Application7", "应用场景 7"),
        ("Application8", "应用场景 8"),
        ("Application9", "应用场景 9"),
        ("Application10", "应用场景 10"),
        ("MaxWidth", "最大宽度"),
        ("MaxHeight", "最大高度"),
        ("PayloadWeight", "载重"),
        ("MarketDemandYear", "市场需求年份"),
        ("MarketDemandQuantity", "市场需求数量"),
        ("IsPrototypeNeeded", "是否需要样机"),
        ("PrototypeProvidedTime", "样机提供日期"),
        ("PrototypeQuantity", "样机数量"),
        ("ExpectedPrototypeConfirmTime", "样机确认日期"),
        ("SCRequirementUL", "UL 认证"),
        ("SCRequirementCE", "CE 认证"),
        ("SCRequirementFCC", "FCC 认证"),
        ("SCRequirementROHS", "ROHS 认证"),
        ("SCRequirementOtherC", "其它认证"),
        ("SCRequirementOther", "其它认证说明"),
        ("SpecialFunction", "特殊功能"),
        ("PackageMaxSize", "包装最大尺寸"),
        ("PackageMaxWeight", "包装最大重量"),
        ("PackageOther", "包装其它要求"),
        ("OtherRequirement", "其它要求"),
        ("DemandChange", "需求变更"),
        ("Remark", "备注")
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>白名单字段的反射信息，只解析一次。</summary>
    private static readonly (PropertyInfo Info, string Label)[] Props =
        Fields.Select(f => (typeof(RemoteLxModel).GetProperty(f.Property), f.Label))
              .Where(x => x.Item1 is not null)
              .Select(x => (x.Item1!, x.Label))
              .ToArray();

    /// <summary>逐字段比出差异；没有变化时返回空列表。</summary>
    public static List<RemoteLxChange> Compare(RemoteLxModel? before, RemoteLxModel? after)
    {
        var changes = new List<RemoteLxChange>();
        if (before is null || after is null) return changes;

        foreach (var (info, label) in Props)
        {
            var oldText = Format(info.GetValue(before));
            var newText = Format(info.GetValue(after));
            if (string.Equals(oldText, newText, StringComparison.Ordinal)) continue;

            changes.Add(new RemoteLxChange { Field = label, OldValue = oldText, NewValue = newText });
        }

        return changes;
    }

    /// <summary>只把参与比较的业务字段复制一份，作为「改之前」的快照。</summary>
    public static RemoteLxModel Snapshot(RemoteLxModel source)
    {
        var clone = new RemoteLxModel();
        if (source is null) return clone;

        foreach (var (info, _) in Props)
            info.SetValue(clone, info.GetValue(source));

        return clone;
    }

    public static string ToJson(IReadOnlyList<RemoteLxChange> changes) => JsonSerializer.Serialize(changes, JsonOptions);

    /// <summary>从版本表的 ChangeDiff 读出修改点；格式不对就当没有，不让弹窗把页面搞崩。</summary>
    public static List<RemoteLxChange> FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<RemoteLxChange>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>日期只比到天（表单本来就只有日期），bool 用「是 / 否」。</summary>
    private static string Format(object? value) => value switch
    {
        null => "",
        DateTime dt => dt.ToString("yyyy-MM-dd"),
        bool b => b ? "是" : "否",
        string s => s.Trim(),
        _ => value.ToString()?.Trim() ?? ""
    };
}

