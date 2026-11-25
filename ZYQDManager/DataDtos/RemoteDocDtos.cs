using System.Text.Encodings.Web;
using System.Text.Json;
using ZYQDManager.DataModels;

namespace ZYQDManager.DataDtos;

/// <summary>
/// 规格书 / 承认书参数表的字段代码。
/// 页面读写统一走常量，避免拼写不一致导致"保存后读不出来"。
/// </summary>
public static class RemoteDocKeys
{
    public const string ProductName = "productName";
    public const string PartCode = "partCode";
    public const string Customer = "customer";
    public const string Company = "company";

    // 遥控器规格书参数规格表专属字段
    public const string ModelNo = "modelNo";
    public const string RemoteSize = "remoteSize";
    public const string BracketSize = "bracketSize";
    public const string RemoteWeight = "remoteWeight";
    public const string RemoteColor = "remoteColor";
    public const string TransmitPower = "transmitPower";
    public const string ChannelQuantity = "channelQuantity";


    public const string InputVoltage = "inputVoltage";
    public const string RatedTorque = "ratedTorque";
    public const string Speed = "speed";
    public const string RatedPower = "ratedPower";
    public const string Noise = "noise";

    public const string RadioFrequency = "radioFrequency";
    public const string RadioMode = "radioMode";
    public const string Protocol = "protocol";
    public const string RfDistance = "rfDistance";
    public const string SmartModule = "smartModule";
    public const string Function = "function";

    public const string PowerSupply = "powerSupply";
    public const string InternalBattery = "internalBattery";
    public const string BatteryType = "batteryType";
    public const string BatteryCapacity = "batteryCapacity";
    public const string BatteryEndurance = "batteryEndurance";
    public const string RechargePort = "rechargePort";
    public const string FastCharge = "fastCharge";

    public const string MotorHead = "motorHead";
    public const string Shaft = "shaft";
    public const string MotorLength = "motorLength";
    public const string MotorWeight = "motorWeight";
    public const string SurfacePrint = "surfacePrint";

    public const string Cord = "cord";
    public const string CordCertified = "cordCertified";
    public const string CordColor = "cordColor";
    public const string CordType = "cordType";
    public const string CordPlug = "cordPlug";
    public const string CordLength = "cordLength";

    public const string ProtectionTime = "protectionTime";
    public const string WorkingTemp = "workingTemp";
    public const string Applications = "applications";
    public const string UsageEnv = "usageEnv";
    public const string IpDegree = "ipDegree";
    public const string Certification = "certification";
    public const string SpecialFunction = "specialFunction";
    public const string OtherRequirement = "otherRequirement";
    public const string Warranty = "warranty";

    public const string Esd = "esd";
    public const string InterfaceDim = "interfaceDim";
    public const string CustomerParts = "customerParts";
    public const string CustomerLoad = "customerLoad";

    /// <summary>第四部分两张图的 URL，图片文件由 RemoteImageService 落盘。</summary>
    public const string OverallDrawing = "overallDrawing";
    public const string ControlDiagram = "controlDiagram";

    // ===== 遥控器规格书 / 承认书参数表（逐行对应 Templates/遥控器规格书.xlsx）=====

    /// <summary>模板第 12 行重复出现的第二个「遥控技术 Radio Mode」。</summary>
    public const string RadioModeSecond = "radioModeSecond";

    /// <summary>产品应用场合（模板第 16 行，10 个复选）补充说明。</summary>
    public const string ApplicationsOther = "applicationsOther";

    /// <summary>频率「Other____ MHz」。</summary>
    public const string RfOther = "rfOther";

    /// <summary>传输距离 室内 / 室外。</summary>
    public const string RfDistanceIndoor = "rfDistanceIndoor";
    public const string RfDistanceOutdoor = "rfDistanceOutdoor";

    /// <summary>电池型号「□Other________」。</summary>
    public const string BatteryTypeOther = "batteryTypeOther";

    /// <summary>充电口要求「其它」。</summary>
    public const string RechargePortOther = "rechargePortOther";

    /// <summary>认证要求「□其它Other」。</summary>
    public const string CertificationOther = "certificationOther";

    /// <summary>使用环境「特殊环境」的具体说明。</summary>
    public const string UsageOther = "usageOther";

    /// <summary>IP 等级「□其它Other」。</summary>
    public const string IpOther = "ipOther";
}

/// <summary>
/// 规格书 / 承认书的扩展内容。
/// 参数表、工况等按字段代码存 Fields（字典），配件/包装/操作说明等结构性内容用列表承载。
/// 整体序列化后写入规格书 / 承认书的 ExtraJson 列，由 RemoteDocService 负责读写。
/// </summary>
public class RemoteDocExtra
{
    public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>变更履历表 ECN List</summary>
    public List<RemoteChangeLogRow> ChangeLog { get; set; } = new();

    /// <summary>三、选配配件</summary>
    public List<RemotePartRow> Accessories { get; set; } = new();

    /// <summary>五、产品附件</summary>
    public List<RemotePartRow> Attachments { get; set; } = new();

    /// <summary>六、包装</summary>
    public List<RemotePackageRow> Packages { get; set; } = new();

    /// <summary>七、产品操作说明（小节代码 → 内容）</summary>
    public Dictionary<string, string> Operations { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>样品检验结果（仅承认书）</summary>
    public List<RemoteSampleCheckRow> SampleChecks { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static RemoteDocExtra FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return CreateDefault();
        try
        {
            var obj = JsonSerializer.Deserialize<RemoteDocExtra>(json, JsonOptions);
            return obj is null ? CreateDefault() : Normalize(obj);
        }
        catch
        {
            return CreateDefault();
        }
    }

    /// <summary>补齐固定行，避免老数据缺行导致页面塌陷。</summary>
    public static RemoteDocExtra Normalize(RemoteDocExtra e)
    {
        e.Fields ??= new(StringComparer.OrdinalIgnoreCase);
        e.Operations ??= new(StringComparer.OrdinalIgnoreCase);
        e.ChangeLog ??= new();
        e.Accessories ??= new();
        e.Attachments ??= new();
        e.Packages ??= new();
        e.SampleChecks ??= new();

        EnsurePartRows(e.Accessories, DefaultAccessories);
        EnsurePartRows(e.Attachments, DefaultAttachments);
        EnsurePackageRows(e.Packages, DefaultPackages);

        while (e.SampleChecks.Count < DefaultSampleChecks.Length)
        {
            var d = DefaultSampleChecks[e.SampleChecks.Count];
            e.SampleChecks.Add(new RemoteSampleCheckRow { Code = d.Code, Name = d.Name });
        }

        return e;
    }

    /// <summary>
    /// 按模板行序整理固定行：
    /// 1. 没填过内容的行改回模板名称（早期照电机模板抄过来的行名会被纠正）；
    /// 2. 行数不够补足；
    /// 3. 多出来的空行删掉（模板没有那么多行）。
    /// 填过型号/图片/备注的行一律原样保留，避免数据被挪到别的行上。
    /// </summary>
    private static void EnsurePartRows(List<RemotePartRow> rows, string?[] templateNames)
    {
        for (var i = 0; i < rows.Count && i < templateNames.Length; i++)
            if (!HasPartContent(rows[i]) && IsPresetPartName(rows[i].Name))
                rows[i].Name = templateNames[i];

        while (rows.Count < templateNames.Length)
        {
            var i = rows.Count;
            rows.Add(new RemotePartRow { Name = templateNames[i] });
        }

        for (var i = rows.Count - 1; i >= templateNames.Length; i--)
            if (!HasPartContent(rows[i]) && IsPresetPartName(rows[i].Name))
                rows.RemoveAt(i);
    }

    /// <summary>
    /// 模板印好的零件名 + 早期照电机模板抄错的默认名。
    /// 名称不在这个集合里，说明是用户自己敲的配件名（三、选配配件是可编辑的），
    /// 这种行不能被改名，也不能被当成多余空行删掉。
    /// </summary>
    private static readonly HashSet<string> PresetPartNames = new(StringComparer.Ordinal)
    {
        "电机 Motor",
        "配件包 Accessory bag",
        "支架 Bracket",
        "遥控器铭牌 Label on Remote",
        "内盒铭牌 Label on Inner box",
        "遥控器二维码 QR Code on Remote",
        "内盒二维码 QR Code on Inner Box",
        "说明书 Manual",
        "其它 Other"
    };

    private static bool IsPresetPartName(string? name)
        => string.IsNullOrWhiteSpace(name) || PresetPartNames.Contains(name.Trim());

    private static void EnsurePackageRows(List<RemotePackageRow> rows, string?[] templateNames)
    {
        for (var i = 0; i < rows.Count && i < templateNames.Length; i++)
            if (!HasPackageContent(rows[i]))
                rows[i].Name = templateNames[i];

        while (rows.Count < templateNames.Length)
        {
            var i = rows.Count;
            rows.Add(new RemotePackageRow { Name = templateNames[i] });
        }

        for (var i = rows.Count - 1; i >= templateNames.Length; i--)
            if (!HasPackageContent(rows[i]))
                rows.RemoveAt(i);
    }

    private static bool HasPartContent(RemotePartRow r)
        => !string.IsNullOrWhiteSpace(r.Type)
           || !string.IsNullOrWhiteSpace(r.Photo)
           || !string.IsNullOrWhiteSpace(r.Remark);

    private static bool HasPackageContent(RemotePackageRow r)
        => !string.IsNullOrWhiteSpace(r.Drawing)
           || !string.IsNullOrWhiteSpace(r.Size)
           || !string.IsNullOrWhiteSpace(r.Material)
           || !string.IsNullOrWhiteSpace(r.Qty);

    public static RemoteDocExtra CreateDefault() => Normalize(new RemoteDocExtra());

    /// <summary>三、选配配件：模板固定 4 行，只有第 1 行印了「电机 Motor」，其余留白。</summary>
    public static readonly string?[] DefaultAccessories = ["电机 Motor", null, null, null];

    /// <summary>五、产品附件：模板固定 7 行，名称已印在表上。</summary>
    public static readonly string?[] DefaultAttachments =
    [
        "配件包 Accessory bag",
        "支架 Bracket",
        "遥控器铭牌 Label on Remote",
        "内盒铭牌 Label on Inner box",
        "遥控器二维码 QR Code on Remote",
        "内盒二维码 QR Code on Inner Box",
        "说明书 Manual"
    ];

    /// <summary>六、包装：模板固定 6 行，名称已印在表上（「Bagt」为模板原文拼写）。</summary>
    public static readonly string?[] DefaultPackages =
    [
        "内盒 Inner Box",
        "袋子 Bagt",
        "外箱 Master Carton",
        "唛头 Shipping Mark",
        "托盘 Pallet",
        "托唛 Pallet Shipping Mark"
    ];

    public static readonly (string Code, string Name)[] DefaultSampleChecks =
    [
        ("appearance", "外观 Appearance"),
        ("dimension", "尺寸 Dimension"),
        ("performance", "功性能 Performance capabilities"),
        ("reliability", "可靠性 Reliability"),
        ("other", "其它 Others"),
        ("spec", "附产品规格书 Attach the product specification sheet")
    ];
}

public class RemoteChangeLogRow
{
    public string? Date { get; set; }
    public string? Rev { get; set; }
    public string? By { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
}

public class RemotePartRow
{
    public string? No { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Photo { get; set; }
    public string? Remark { get; set; }
}

public class RemotePackageRow
{
    public string? Name { get; set; }
    public string? Drawing { get; set; }
    public string? Size { get; set; }
    public string? Material { get; set; }
    public string? Qty { get; set; }
}

public class RemoteSampleCheckRow
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    /// <summary>PASS / FAIL / 空</summary>
    public string? Result { get; set; }
    public string? Remark { get; set; }
}

/// <summary>规格书 / 承认书返回包装</summary>
public class RemoteDocPayload
{
    public required string DocGuid { get; init; }
    public required string ProjectGuid { get; init; }
    public string? VersionNo { get; init; }
    public string? Status { get; init; }
    public RemoteDocExtra Extra { get; init; } = RemoteDocExtra.CreateDefault();
}

/// <summary>
/// 规格书 / 承认书保存结果（原随内存版仓库定义，接库后独立在此）。
/// Created=true 表示这次是新建并生成了 DocGuid。
/// </summary>
public class RemoteSheetSaveResult
{
    public string ProjectGuid { get; set; } = "";
    public string DocGuid { get; set; } = "";
    public string VersionNo { get; set; } = "";
    public bool Created { get; set; }
}

/// <summary>
/// 同一项目、同一版本下三份单据的定位信息。
/// 左侧时间线用它决定「能不能点」以及跳到哪个 DocGuid。
/// </summary>
public class RemoteVersionDocs
{
    public string? LxDocGuid { get; set; }
    public string? LxStatus { get; set; }

    public string? GgDocGuid { get; set; }
    public string? GgStatus { get; set; }

    public string? CrDocGuid { get; set; }
    public string? CrStatus { get; set; }

    /// <summary>版本当前流程节点 1..7，8 = 已归档，0 = 未回填，见 <see cref="Utilities.RemoteNode"/>。</summary>
    public int CurrentNode { get; set; }

    /// <summary>版本自身状态：InProgress / ReadyToArchive / Archived，归档按钮靠它判断是否已归档。</summary>
    public string? VersionStatus { get; set; }

    /// <summary>项目业务员（立项人）：立项单/承认书只有他能改，回传签字版、归档也只允许他本人或管理员。</summary>
    public string? OwnerEmpNo { get; set; }

    public string? OwnerName { get; set; }

    /// <summary>项目当前研发工程师：规格书只有他能改；研发经理只审批，不看编辑入口。</summary>
    public string? EngineerEmpNo { get; set; }

    public string? EngineerName { get; set; }

    /// <summary>版本研发经理（节点 3 提交审批时指定，节点 5 沿用）；未指派时为空。</summary>
    public string? ManagerEmpNo { get; set; }

    public string? ManagerName { get; set; }

    /// <summary>
    /// 版本行本身，带七个流程的 21 列留痕（Node{n}EmpNo / Done / DoneTime）。
    /// 页面上的「流程记录」直接渲染它，不再另查一次库。
    /// </summary>
    public ProjectVersionModel? Version { get; set; }

    /// <summary>七个节点负责人工号→姓名，流程记录表直接显示姓名。</summary>
    public Dictionary<string, string> NodeNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 本项目所有版本的版本行（含七个节点的 21 列留痕），按创建顺序。
    /// 流程记录按版本分组展示整条历史，不能只拿当前版本那一行。
    /// </summary>
    public List<ProjectVersionModel> Versions { get; set; } = [];

    /// <summary>本项目所有版本的流转记录，页面按 DocVersion 分组后翻译成业务节点名。</summary>
    public List<ApprovalFlowModel> ExtraFlows { get; set; } = [];

    /// <summary>
    /// 本项目所有版本的流程日志（T_ZYQD_ProjectFlowLog），按版本 + 序号排好。
    /// 「流程记录」表直接渲染这份数据，不再从流水倒推，也就不存在把操作人认错的问题。
    /// </summary>
    public List<ProjectFlowLogModel> FlowLogs { get; set; } = [];

    public static bool Has(string? docGuid) => !string.IsNullOrWhiteSpace(docGuid);
}

/// <summary>
/// 一屏三单（立项单 / 规格书 / 承认书）打开或切换时所需的全量数据。
/// 由 <see cref="Services.RemoteDocService.GetBundleAsync"/> 一次取齐，
/// 三个单据组件共用同一份：
/// 1. 同一版本的三张单据只查一轮库，切换时不重复查询；
/// 2. 同一渲染树里的多个子组件不会并发使用同一个 scoped DbContext。
/// </summary>
public class RemoteDocBundle
{
    /// <summary>三单所属项目。</summary>
    public string? ProjectGuid { get; set; }

    /// <summary>三单所属版本号。</summary>
    public string? VersionNo { get; set; }

    public RemoteLxModel? Lx { get; set; }
    public RemoteGgModel? Gg { get; set; }
    public RemoteCrModel? Cr { get; set; }

    /// <summary>三单定位 + 版本流程节点 + 流程留痕，时间线 / 翻页箭头 / 审批指派共用。</summary>
    public RemoteVersionDocs Docs { get; set; } = new();

    /// <summary>版本下拉的版本号（按立项单口径，最新在前）。</summary>
    public List<string> VersionNos { get; set; } = [];

    /// <summary>版本下拉的版本行（含归档等状态）。</summary>
    public List<ProjectVersionModel> Versions { get; set; } = [];

    /// <summary>项目表上的当前版本号，用于下拉标注「当前版本」。</summary>
    public string? CurrentVersionNo { get; set; }

    /// <summary>
    /// 本版本最近一次退回记录（含原因），由父组件一次查回，
    /// 三个单据组件直接读它，避免并发使用同一个 DbContext 报错。
    /// </summary>
    public ApprovalFlowModel? LatestReject { get; set; }

    /// <summary>三份单据都不存在（版本刚建、还没有任何单据）。</summary>
    public bool IsEmpty => Lx is null && Gg is null && Cr is null;

    /// <summary>某个 DocGuid 是否落在本 bundle 的三份单据里（判断「同版本切换，不用重查」）。</summary>
    public bool ContainsDocGuid(string? docGuid)
        => !string.IsNullOrWhiteSpace(docGuid)
           && (SameDoc(Lx?.DocGuid, docGuid) || SameDoc(Gg?.DocGuid, docGuid) || SameDoc(Cr?.DocGuid, docGuid));

    private static bool SameDoc(string? a, string? b)
        => string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// 一个流程节点的完成记录（对应 T_ZYQD_ProjectVersion 的 Node{n}Done / EmpNo / DoneTime）。
/// 归档后查档与单据页的「流程记录」都用它。
/// </summary>
public class RemoteFlowRecord
{
    /// <summary>节点号 1..7，见 <see cref="Utilities.RemoteNode"/>。</summary>
    public int Node { get; set; }

    /// <summary>流程名，取 RemoteNode.Title（「待谁做什么」口径）。</summary>
    public string Title { get; set; } = "";

    public bool Done { get; set; }

    public string? EmpNo { get; set; }

    /// <summary>负责人姓名，按工号实时解析账号中心；查不到时为 null，页面回退显示工号。</summary>
    public string? EmpName { get; set; }

    public DateTime? DoneTime { get; set; }

    /// <summary>负责人展示名：优先姓名，其次工号，都没有就空着。</summary>
    public string OwnerText => EmpName ?? EmpNo ?? "";

    /// <summary>完成时间统一显示到秒，库里本来就是 DATETIME2。</summary>
    public string DoneTimeText => DoneTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";

    /// <summary>完成情况的中文文案。</summary>
    public string StateText => Done ? "已完成" : "未完成";
}


