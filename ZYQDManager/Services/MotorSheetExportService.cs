using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 三份表单的 Excel 导出。PDF 由 <see cref="LibreOfficePdfService"/> 把这个 xlsx 转过去，
/// 所以屏幕上看到的版式和导出的文件是同一套数据、同一种左右结构。
///
/// 版式：5 列（16% / 16% / 21% / 23% / 24%），标签占 A:B，值占 C:E，
/// 与 wwwroot/css/motor-sheet.css 里的 .msheet 比例一致。
/// </summary>
public class MotorSheetExportService
{
    private const string Company = "浙江捷昌线性驱动科技股份有限公司";

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MotorSheetExportService> _logger;
    private readonly SqlServerDbContext _db;

    public MotorSheetExportService(
        IWebHostEnvironment env, ILogger<MotorSheetExportService> logger, SqlServerDbContext db)
    {
        _env = env;
        _logger = logger;
        _db = db;
    }

    /// <summary>工号 → 姓名（员工表）。业务表只存工号，导出时补姓名。</summary>
    private string? EmpName(string? empNo)
    {
        if (string.IsNullOrWhiteSpace(empNo)) return null;
        var no = empNo.Trim();
        return _db.Employees.AsNoTracking()
            .Where(x => x.EmpNo == no)
            .Select(x => x.EmpName)
            .FirstOrDefault();
    }

    /// <summary>wwwroot/Templates 下的模板路径；文件不存在时返回 null（调用方回退旧实现）。</summary>
    private string? TemplatePath(string fileName)
    {
        var root = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(AppContext.BaseDirectory, "wwwroot");

        var path = Path.Combine(root, "Templates", fileName);
        return File.Exists(path) ? path : null;
    }

    // ==================== 对外入口 ====================

    /// <summary>下载文件名：单据名_客户_型号_版本.扩展名，去掉文件系统非法字符。</summary>
    public string FileName(string docLabel, string? customer, string? model, string? versionNo, string ext)
    {
        var parts = new[] { docLabel, customer, model, versionNo }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Sanitize(x!))
            .Where(x => x.Length > 0);

        var name = string.Join("_", parts);
        if (name.Length == 0) name = docLabel;
        if (name.Length > 90) name = name[..90];
        return name + "." + ext.TrimStart('.');
    }

    /// <summary>
    /// 规格书导出：优先套用 wwwroot/Templates/电机规格书.xlsx。
    /// 模板里的表格线、合并区域、中英文对照说明全部原样保留，只填数据与勾选。
    /// </summary>
    public byte[] BuildGg(MotorGgModel head, MotorDocExtra extra)
    {
        var filled = TryBuildSpecFromTemplate(extra, "电机规格书.xlsx", GgParamStart, approval: false);
        if (filled is not null) return filled;

        _logger.LogWarning("规格书模板导出未生效，已回退到内置版式：{Reason}",
            ExcelTemplateWriter.LastError ?? "未知原因");
        return BuildGgLegacy(head, extra);
    }

    /// <summary>承认书导出：模板「电机样品承认书.xlsx」，比规格书多一块样品检验结果。</summary>
    public byte[] BuildCr(MotorCrModel head, MotorDocExtra extra)
    {
        var filled = TryBuildSpecFromTemplate(extra, "电机样品承认书.xlsx", CrParamStart, approval: true);
        if (filled is not null) return filled;

        _logger.LogWarning("承认书模板导出未生效，已回退到内置版式：{Reason}",
            ExcelTemplateWriter.LastError ?? "未知原因");
        return BuildCrLegacy(head, extra);
    }

    /// <summary>两份模板表头以下的行偏移恒为 3，参数表起始行不同，其余按偏移推。</summary>
    private const int GgParamStart = 33;
    private const int CrParamStart = 30;

    /// <summary>
    /// 模板坐标 ← 单据字段。行号按 <paramref name="paramStart"/>（参数表首行）算偏移，
    /// 两份模板共用一个映射，承认书只多出检验结果与少一块变更履历。
    /// </summary>
    private byte[]? TryBuildSpecFromTemplate(MotorDocExtra extra, string fileName, int paramStart, bool approval)
    {
        int Row(int offset) => paramStart + offset;

        var cells = new Dictionary<string, string?>
        {
            // 表头：产品名称 / 产品型号 / 客户 / 公司（模板标签在 B 列，值填 D 列，C 列留白避免挤掉标签）
            ["D9"] = Field(extra, MotorDocKeys.ProductName),
            ["D10"] = Field(extra, MotorDocKeys.PartCode),
            ["D11"] = Field(extra, MotorDocKeys.Customer),
            ["D12"] = Field(extra, MotorDocKeys.Company),

            // 一、参数表
            ["C" + Row(1)] = Field(extra, MotorDocKeys.RatedTorque),
            ["C" + Row(2)] = Join(Field(extra, MotorDocKeys.Speed), Field(extra, "speedLevels")),
            ["C" + Row(3)] = Field(extra, MotorDocKeys.RatedPower),
            ["C" + Row(4)] = Field(extra, MotorDocKeys.Noise),
            ["C" + Row(8)] = Format("距离distance: {0} 米", Field(extra, MotorDocKeys.RfDistance)),
            ["C" + Row(12)] = Lithium(extra),
            ["C" + Row(13)] = Field(extra, MotorDocKeys.BatteryEndurance),
            ["C" + Row(15)] = Format("{0} mAh", Field(extra, MotorDocKeys.BatteryCapacity)),
            ["C" + Row(16)] = BatteryTest(extra),
            ["C" + Row(19)] = Field(extra, MotorDocKeys.MotorHead),
            ["C" + Row(20)] = Field(extra, MotorDocKeys.Shaft),
            ["C" + Row(21)] = Field(extra, MotorDocKeys.Cord),
            ["C" + Row(26)] = CordLength(extra),
            ["C" + Row(27)] = Tube(extra),
            ["C" + Row(28)] = Field(extra, MotorDocKeys.MotorLength),
            ["C" + Row(29)] = Field(extra, MotorDocKeys.MotorWeight),
            ["C" + Row(30)] = Field(extra, MotorDocKeys.ProtectionTime),
            ["C" + Row(35)] = Field(extra, MotorDocKeys.Warranty),
            ["C" + Row(36)] = Field(extra, MotorDocKeys.SpecialFunction),
            ["C" + Row(37)] = Field(extra, MotorDocKeys.OtherRequirement),

            // 二、产品使用工况
            ["C" + Row(43)] = Field(extra, MotorDocKeys.WorkingTemp),
            ["C" + Row(44)] = Field(extra, MotorDocKeys.Esd),
            ["C" + Row(45)] = Field(extra, MotorDocKeys.CustomerParts),
            ["C" + Row(46)] = Field(extra, MotorDocKeys.InterfaceDim),
            ["C" + Row(47)] = Field(extra, MotorDocKeys.CustomerLoad)
        };

        // 三、选配配件（模板预置 4 行：遥控器 / 皇冠 / 转轮 / 电池棒）
        for (var i = 0; i < extra.Accessories.Count && i < 4; i++)
        {
            var row = Row(51 + i);
            cells["C" + row] = extra.Accessories[i].Type;
            cells["E" + row] = extra.Accessories[i].Remark;
        }

        // 五、产品附件（模板预置 3 行：标签 / 说明书 / 其它）
        for (var i = 0; i < extra.Attachments.Count && i < 3; i++)
        {
            var row = Row(62 + i);
            cells["C" + row] = extra.Attachments[i].Type;
            cells["E" + row] = extra.Attachments[i].Remark;
        }

        // 六、包装（模板预置 5 行）
        for (var i = 0; i < extra.Packages.Count && i < 5; i++)
        {
            var row = Row(68 + i);
            cells["C" + row] = extra.Packages[i].Size;
            cells["D" + row] = extra.Packages[i].Material;
            cells["E" + row] = extra.Packages[i].Qty;
        }

        // 七、产品操作说明：OperationRows 与模板小节逐行对齐（规格书 108~138）
        for (var i = 0; i < OperationRows.Length; i++)
        {
            var code = OperationRows[i].Code;
            if (code.Length == 0) continue;
            extra.Operations.TryGetValue(code, out var value);
            if (string.IsNullOrWhiteSpace(value)) continue;
            cells["C" + Row(75 + i)] = value;
        }

        var checks = new Dictionary<string, string?>
        {
            ["C" + Row(0)] = Field(extra, MotorDocKeys.InputVoltage),
            ["C" + Row(5)] = Field(extra, MotorDocKeys.RadioFrequency),
            ["C" + Row(6)] = Field(extra, MotorDocKeys.Protocol),
            ["C" + Row(7)] = Field(extra, MotorDocKeys.RadioMode),
            ["C" + Row(9)] = Field(extra, MotorDocKeys.SmartModule),
            ["C" + Row(10)] = Field(extra, MotorDocKeys.Function),
            ["C" + Row(11)] = Field(extra, MotorDocKeys.PowerSupply),
            ["C" + Row(14)] = Field(extra, MotorDocKeys.BatteryType),
            ["C" + Row(17)] = Field(extra, MotorDocKeys.RechargePort),
            ["C" + Row(18)] = Field(extra, MotorDocKeys.FastCharge),
            ["C" + Row(22)] = Field(extra, MotorDocKeys.CordCertified),
            ["C" + Row(23)] = Field(extra, MotorDocKeys.CordColor),
            ["C" + Row(24)] = Field(extra, MotorDocKeys.CordType),
            ["C" + Row(25)] = Field(extra, MotorDocKeys.CordPlug),
            ["C" + Row(31)] = Field(extra, MotorDocKeys.Applications),
            ["C" + Row(32)] = Field(extra, MotorDocKeys.SurfacePrint),
            ["C" + Row(34)] = Field(extra, MotorDocKeys.Certification),
            ["C" + Row(41)] = Field(extra, MotorDocKeys.UsageEnv),
            ["C" + Row(42)] = Field(extra, MotorDocKeys.IpDegree)
        };

        // 选项后面的「其它____」自定义输入：勾完再另起一行补上
        var appends = new Dictionary<string, string?>
        {
            ["C" + Row(0)] = Field(extra, MotorDocKeys.InputVoltage + "Other"),
            ["C" + Row(10)] = Field(extra, "functionOther"),
            ["C" + Row(14)] = Field(extra, "batteryTypeOther"),
            ["C" + Row(23)] = Field(extra, "cordColorOther"),
            ["C" + Row(24)] = Field(extra, "cordTypeOther"),
            ["C" + Row(25)] = Field(extra, "cordPlugOther"),
            ["C" + Row(34)] = Field(extra, "certificationOther"),
            ["C" + Row(41)] = Field(extra, "usageOther"),
            ["C" + Row(42)] = Field(extra, "ipOther")
        };

        if (approval)
        {
            // 承认书：样品检验结果的结论列（E16~E21，模板预置 □ PASS □ FAIL）
            for (var i = 0; i < extra.SampleChecks.Count && i < 6; i++)
                checks["E" + (16 + i)] = extra.SampleChecks[i].Result;
        }
        else
        {
            // 规格书：变更履历表数据行从 23 起（23~29 共 7 行空表），按顺序往下填。
            // 表头在 20~22：A20 标题、A21:C21 表头、D21:E21「变更内容描述」、A22:C22 留空、
            // D22「变更前」、E22「变更后」，所以第一条记录落在 23 行。
            var row = 23;
            foreach (var log in extra.ChangeLog.Take(7))
            {
                cells["A" + row] = log.Date;
                cells["B" + row] = log.Rev;
                cells["C" + row] = log.By;
                cells["D" + row] = log.Before;
                cells["E" + row] = log.After;
                row++;
            }
        }

        return ExcelTemplateWriter.TryFill(TemplatePath(fileName), cells, checks, appends, out var bytes)
            ? bytes
            : null;
    }

    /// <summary>模板不可用时的兜底：从零构建规格书（不含模板版式）。</summary>
    private byte[] BuildGgLegacy(MotorGgModel head, MotorDocExtra extra)
    {
        using var wb = new XLWorkbook();
        var ws = NewSheet(wb, "规格书");

        Title(ws, "产品规格书", "Product Specification Sheet");

        HeadRow(ws, "产品名称 Part name", Field(extra, MotorDocKeys.ProductName));
        HeadRow(ws, "产品型号 Part code", Field(extra, MotorDocKeys.PartCode));
        HeadRow(ws, "客户 Customer", Field(extra, MotorDocKeys.Customer));
        HeadRow(ws, "公司 Company", Field(extra, MotorDocKeys.Company) ?? Company);

        Note(ws, "保密要求：未经同意，不得向第三方透露 (Confidential — do not disclose to third parties without consent)");

        // 变更履历 ECN List
        Section(ws, "变更履历表 ECN List");
        SubHeader(ws, "变更日期 Updated on", "版本 Rev.", "变更人 Updated by", "变更内容 Description of Change");
        if (extra.ChangeLog.Count == 0)
        {
            DataRow(ws, "", "", "", "");
        }
        else
        {
            foreach (var log in extra.ChangeLog)
            {
                var r = ws.LastRowUsed()!.RowNumber() + 1;
                ws.Cell(r, 1).Value = log.Date ?? "";
                ws.Cell(r, 2).Value = log.Rev ?? "";
                ws.Cell(r, 3).Value = log.By ?? "";
                ws.Cell(r, 4).Value = $"{(log.Before ?? "")} → {(log.After ?? "")}";
                ws.Range(r, 4, r, 5).Merge();
                Body(ws.Range(r, 1, r, 5));
            }
        }

        AppendSpecSections(ws, extra, includeSampleChecks: false);
        Finish(ws);
        return Save(wb);
    }

    /// <summary>模板不可用时的兜底：从零构建承认书（不含模板版式）。</summary>
    private byte[] BuildCrLegacy(MotorCrModel head, MotorDocExtra extra)
    {
        using var wb = new XLWorkbook();
        var ws = NewSheet(wb, "承认书");

        Title(ws, "样品承认书", "Sample Approval Sheet");

        HeadRow(ws, "产品名称 Part name", Field(extra, MotorDocKeys.ProductName));
        HeadRow(ws, "产品型号 Part code", Field(extra, MotorDocKeys.PartCode));
        HeadRow(ws, "客户 Customer", Field(extra, MotorDocKeys.Customer));
        HeadRow(ws, "公司 Company", Field(extra, MotorDocKeys.Company) ?? Company);

        // 样品检验结果
        Section(ws, "样品检验结果 Sample Inspection Result");
        var hr = ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(hr, 1).Value = "检验项目 Inspection Item";
        ws.Range(hr, 1, hr, 3).Merge();
        ws.Cell(hr, 4).Value = "结论 Conclusion";
        ws.Range(hr, 4, hr, 5).Merge();
        Body(ws.Range(hr, 1, hr, 5));

        foreach (var item in extra.SampleChecks)
        {
            var r = ws.LastRowUsed()!.RowNumber() + 1;
            ws.Cell(r, 1).Value = item.Name ?? "";
            ws.Range(r, 1, r, 3).Merge();
            ws.Cell(r, 4).Value = string.IsNullOrWhiteSpace(item.Result) ? "" : item.Result;
            ws.Range(r, 4, r, 5).Merge();
            Body(ws.Range(r, 1, r, 5));
        }

        AppendSpecSections(ws, extra, includeSampleChecks: false);
        Finish(ws);
        return Save(wb);
    }

    /// <summary>
    /// 立项单导出：优先套用 wwwroot/Templates/电机立项单.xlsx 模板，
    /// 只把数据写进对应单元格 —— 公司 logo、表单编号、保存期限、字体边框全部原样保留。
    /// 模板缺失或损坏时回退到 <see cref="BuildLxLegacy"/>。
    /// </summary>
    public byte[] BuildLx(MotorLxModel lx)
    {
        var filled = TryBuildLxFromTemplate(lx);
        if (filled is not null) return filled;

        _logger.LogWarning("立项单模板导出未生效，已回退到内置版式：{Reason}",
            ExcelTemplateWriter.LastError ?? "未知原因");
        return BuildLxLegacy(lx);
    }

    /// <summary>模板坐标 ← 单据字段。空值不写，保留模板里的原始说明文案。</summary>
    private byte[]? TryBuildLxFromTemplate(MotorLxModel lx)
    {
        // 模板 C8~C40 里本来就印着「直径______，长度限制」这类填空说明，
        // 空白格直接写值；有说明文字的按句式填进去，别把整段说明冲掉。
        var cells = new Dictionary<string, string?>
        {
            // 表头：需求人 / 提出需求日期 / 要求完成日期 / 客户
            ["B5"] = lx.DemandPerson,
            ["D5"] = lx.TimeToRequest?.ToString("yyyy-MM-dd"),
            ["F5"] = lx.RequiredCompletionTime?.ToString("yyyy-MM-dd"),
            ["B6"] = lx.Customer,

            // 01~33：与模板左侧序号一一对应
            ["C8"] = lx.MotorModelSpec,
            ["C9"] = lx.Description,
            ["C10"] = Join(lx.InputVoltage, lx.InputVoltageOther),
            ["C11"] = lx.RatedTorque,
            ["C12"] = SpeedText(lx),
            ["C13"] = lx.RatedPower,
            ["C14"] = lx.OperatingNoise,
            ["C23"] = TubeText(lx),
            ["C24"] = lx.OuterTubeColor,
            ["C25"] = MotorHeadText(lx),
            ["C26"] = CordText(lx),
            ["C27"] = Format("{0}米", lx.CordLength),
            ["C28"] = lx.Shaft,
            ["C29"] = lx.CrownAndDrive,
            ["C30"] = Format("{0}分钟", lx.MaxRunningTime),
            ["C31"] = lx.RemoteController,
            ["C32"] = Applications(lx),
            ["C33"] = UsageEnvText(lx),
            ["C34"] = MarketText(lx),
            ["C35"] = PrototypeNeededText(lx),
            ["C36"] = PrototypeText(lx),
            ["C37"] = CertificationText(lx),
            ["C38"] = lx.SpecialFunction,
            ["C39"] = lx.DemandChange,
            ["C40"] = lx.OtherRequirement
        };

        // 08~15 这几行是多选：模板里没印选项（空白行），按下面的候选整排画出来，
        // 选中的打 ☑、没选的留 ☐，单据上看得出还有哪些可选项。
        var checks = new Dictionary<string, string?>
        {
            ["C15"] = JoinComma(lx.RemoteFrequency, lx.RemoteFrequencyOther),
            ["C16"] = lx.RemoteTechnology,
            ["C17"] = JoinComma(lx.RemoteProtocol, lx.RemoteProtocolOther),
            ["C18"] = lx.MotorFunction,
            ["C19"] = JoinComma(lx.SmartModule, lx.SmartModuleOther),
            ["C20"] = JoinComma(lx.IpLevel, lx.IpLevelOther),
            ["C21"] = lx.FastCharge,
            ["C22"] = lx.InputPower
        };

        var candidates = new Dictionary<string, string?>
        {
            ["C15"] = "433.92 MHz|447.x MHz|868.x MHz|Other",
            ["C16"] = "None|单向Single-way|双向Bi-directional",
            ["C17"] = "捷昌Standard|客户Customized|其它Other",
            ["C18"] = "None|RS485|干触点Dry Contact|强电开关Power Switch|其它Other",
            ["C19"] = "蓝牙|WiFi|Zigbee|其它Matter",
            ["C20"] = "IP40|IP44|IP45|IP66|IP67|IP68|IP20|IP69K|其它",
            ["C21"] = "是Yes|否No",
            ["C22"] = "AC110V/60Hz|AC230V/50Hz|AC100-240V|Adapter12V/2A|Adapter12V/3A|Adapter12V/4A|"
                      + "Adapter8.4V/1A|Li-onBattery2cell|Li-onBattery3cell|Li-onBattery4cell|Adapter24V/1A"
        };

        return ExcelTemplateWriter.TryFill(TemplatePath("电机立项单.xlsx"), cells, checks, null, candidates, out var bytes)
            ? bytes
            : null;
    }

    /// <summary>使用环境行：模板原文「　　室内　　　　室外　　　　特殊环境：______」，逐项画框。</summary>
    private static string UsageEnvText(MotorLxModel lx)
    {
        var value = lx.UsageEnv;
        var other = string.IsNullOrWhiteSpace(lx.UsageEnvOther) ? "______" : lx.UsageEnvOther!.Trim();
        return $"    {Box(value, "室内")}室内          {Box(value, "室外")}室外         "
               + $"{Box(value, "特殊")}特殊环境：{other} (注明具体环境）";
    }

    /// <summary>是否需提供样机行：模板原文「　　是　　　　　否」。</summary>
    private static string PrototypeNeededText(MotorLxModel lx)
    {
        var value = lx.IsPrototypeNeeded;
        return $"     {Box(value, "是")}是           {Box(value, "否")}否";
    }

    /// <summary>安全认证行：模板原文「　　UL　　CE　　FCC　　ROHS　　其它：______」。</summary>
    private static string CertificationText(MotorLxModel lx)
    {
        var other = string.IsNullOrWhiteSpace(lx.SCRequirementOther) ? "______" : lx.SCRequirementOther!.Trim();
        return $"     {Box(lx.SCRequirementUL, "UL")}UL         {Box(lx.SCRequirementCE, "CE")}CE          "
               + $"{Box(lx.SCRequirementFCC, "FCC")}FCC        {Box(lx.SCRequirementROHS, "ROHS")}ROHS       "
               + $"其它：{other}（注明具体要求）";
    }

    /// <summary>值里点到了这个选项就返回打勾方框 ☑，否则空方框 ☐。</summary>
    private static string Box(string? value, string option)
        => !string.IsNullOrWhiteSpace(value) && value.Contains(option, StringComparison.OrdinalIgnoreCase)
            ? "☑"
            : "☐";

    /// <summary>运行速度行模板是「XXXrpm    出厂默认factory default XXXrpm」。</summary>
    private static string? SpeedText(MotorLxModel lx)
    {
        var def = lx.DefaultRunningSpeed?.Trim();
        var level = lx.RatedLevelSpeed?.Trim();
        if (string.IsNullOrEmpty(def) && string.IsNullOrEmpty(level)) return null;

        var main = string.IsNullOrEmpty(def) ? level! : def;
        var other = string.IsNullOrEmpty(level) ? main : level;
        return $"{main}rpm    出厂默认factory default {other}rpm";
    }

    /// <summary>外管行模板是「直径______，长度限制」。</summary>
    private static string? TubeText(MotorLxModel lx)
    {
        var diameter = lx.OuterTubeDiameter?.Trim();
        var limit = lx.OuterTubeLengthLimit?.Trim();
        if (string.IsNullOrEmpty(diameter) && string.IsNullOrEmpty(limit)) return null;
        return $"直径{(string.IsNullOrEmpty(diameter) ? "______" : diameter + "mm")}，长度限制{(limit is null ? "" : limit + "mm")}";
    }

    /// <summary>行程头行模板是「型号______ ，供电接口： Typec MicroUSB 其他：______」。</summary>
    private static string? MotorHeadText(MotorLxModel lx)
    {
        var model = lx.MotorHead?.Trim();
        var port = Join(lx.ChargingMethod, lx.ChargingMethodNo);
        if (string.IsNullOrEmpty(model) && port is null) return null;
        return $"型号{(string.IsNullOrEmpty(model) ? "______" : model)} ，供电接口：{port ?? " Typec        MicroUSB        其他：______"}";
    }

    /// <summary>电线规格行模板是「线材型号______，线材长度______米，有无特殊要求：______」。</summary>
    private static string? CordText(MotorLxModel lx)
    {
        var spec = Join(lx.LeadWireSpecification, lx.CordType);
        var length = lx.CordLength?.Trim();
        var other = lx.CordOtherRequirements?.Trim();
        if (spec is null && string.IsNullOrEmpty(length) && string.IsNullOrEmpty(other)) return null;

        var parts = new List<string>();
        if (spec is not null) parts.Add("线材型号" + spec);
        if (!string.IsNullOrEmpty(length)) parts.Add("线材长度" + length + "米");
        if (!string.IsNullOrEmpty(other)) parts.Add("有无特殊要求：" + other);
        return string.Join("，", parts);
    }

    /// <summary>预估市场需求行模板是「年度______，数量______」。</summary>
    private static string? MarketText(MotorLxModel lx)
    {
        var year = lx.MarketDemandYear?.Trim();
        var quantity = lx.MarketDemandQuantity?.Trim();
        if (string.IsNullOrEmpty(year) && string.IsNullOrEmpty(quantity)) return null;
        return $"年度{(string.IsNullOrEmpty(year) ? "______" : year)}，数量{(string.IsNullOrEmpty(quantity) ? "______" : quantity)}";
    }

    /// <summary>样机行模板是「样机提供时间______，数量______，预计样机确认时间______」。</summary>
    private static string? PrototypeText(MotorLxModel lx)
    {
        var time = lx.PrototypeProvidedTime?.ToString("yyyy-MM-dd");
        var quantity = lx.PrototypeQuantity?.Trim();
        var confirm = lx.ExpectedPrototypeConfirmTime?.ToString("yyyy-MM-dd");
        if (time is null && string.IsNullOrEmpty(quantity) && confirm is null) return null;

        return $"样机提供时间{(time ?? "______")}，数量{(string.IsNullOrEmpty(quantity) ? "______" : quantity)}，"
               + $"预计样机确认时间{(confirm ?? "______")}";
    }

    /// <summary>模板不可用时的兜底：从零构建工作簿（不含模板里的 logo 与表单标识）。</summary>
    private byte[] BuildLxLegacy(MotorLxModel lx)
    {
        using var wb = new XLWorkbook();
        var ws = NewSheet(wb, "立项单");

        Title(ws, "产品立项单", "Product Development Request");

        HeadRow(ws, "需求人 Demand Person", lx.DemandPerson);
        HeadRow(ws, "提出需求日期", lx.TimeToRequest?.ToString("yyyy-MM-dd"));
        HeadRow(ws, "要求完成日期", lx.RequiredCompletionTime?.ToString("yyyy-MM-dd"));
        HeadRow(ws, "客户 Customer", lx.Customer);
        HeadRow(ws, "客户产品编号 Customer Part Code", lx.CustomerPartCode);
        HeadRow(ws, "捷昌型号 Model", lx.MotorModelSpec);
        HeadRow(ws, "产品描述 Description", lx.Description);

        Section(ws, "一、技术参数 Technical Parameters");
        HeadRow(ws, "输入电压 Input Voltage(V)", Join(lx.InputVoltage, lx.InputVoltageOther));
        HeadRow(ws, "额定扭矩 Rated Torque(N.m)", lx.RatedTorque);
        HeadRow(ws, "运行速度 Speed(rpm&cm/s)",
            Join(lx.DefaultRunningSpeed, lx.RatedLevelSpeed));
        HeadRow(ws, "额定功率 Rated Power(W)", lx.RatedPower);
        HeadRow(ws, "运行噪音 Noise(dBA)", lx.OperatingNoise);
        HeadRow(ws, "遥控频率 Radio Frequency", Join(lx.RemoteFrequency, lx.RemoteFrequencyOther));
        HeadRow(ws, "遥控技术 Radio Mode", lx.RemoteTechnology);
        HeadRow(ws, "遥控协议 Protocol", Join(lx.RemoteProtocol, lx.RemoteProtocolOther));
        HeadRow(ws, "功能 Function", lx.MotorFunction);
        HeadRow(ws, "智能模块 Smart Module", Join(lx.SmartModule, lx.SmartModuleOther));
        HeadRow(ws, "IP 等级 IP Degree", Join(lx.IpLevel, lx.IpLevelOther));
        HeadRow(ws, "快充 Fast Charge", lx.FastCharge);
        HeadRow(ws, "输入电源 Power Supply", lx.InputPower);
        HeadRow(ws, "内置锂电 Internal Lithium Battery",
            Join(lx.BuiltInLithiumWhether, lx.BuiltInLithiumBatteryCell));
        HeadRow(ws, "充电方式 Recharge Port", Join(lx.ChargingMethod, lx.ChargingMethodNo));
        HeadRow(ws, "外管 Motor Tube", Join(lx.OuterTubeDiameter, lx.OuterTubeColor, lx.OuterTubeLengthLimit));
        HeadRow(ws, "行程头 Motor Head", lx.MotorHead);
        HeadRow(ws, "输出轴 Shaft", lx.Shaft);
        HeadRow(ws, "电机引出电线规格 Cord", Join(lx.LeadWireSpecification, lx.CordType, lx.CordLength));
        HeadRow(ws, "配套皇冠转轮 Crown & Drive", lx.CrownAndDrive);
        HeadRow(ws, "持续运行时间 Protection Time", lx.MaxRunningTime);
        HeadRow(ws, "配套控制器 Controller", lx.RemoteController);
        HeadRow(ws, "电机长度 Motor Length(mm)", lx.MotorLength);
        HeadRow(ws, "产品重量 Motor Weight(KG)", lx.MotorWeight);

        Section(ws, "二、应用与市场 Application & Market");
        HeadRow(ws, "产品应用场合 Application", Applications(lx));
        HeadRow(ws, "使用环境 Usage Environment", Join(lx.UsageEnv, lx.UsageEnvOther));
        HeadRow(ws, "预估市场需求 Market Demand", Join(lx.MarketDemandYear, lx.MarketDemandQuantity));
        HeadRow(ws, "是否需提供样机 Prototype Needed", lx.IsPrototypeNeeded);
        HeadRow(ws, "样机提供时间 Prototype Time", lx.PrototypeProvidedTime?.ToString("yyyy-MM-dd"));
        HeadRow(ws, "样机数量 Prototype Qty", lx.PrototypeQuantity);
        HeadRow(ws, "预计样机确认时间", lx.ExpectedPrototypeConfirmTime?.ToString("yyyy-MM-dd"));

        Section(ws, "三、认证与特殊要求 Certification & Requirements");
        HeadRow(ws, "安全认证要求 Certification", Certifications(lx));
        HeadRow(ws, "产品特殊功能 Special Function", lx.SpecialFunction);
        HeadRow(ws, "需求是否有变更 Demand Change", lx.DemandChange);
        HeadRow(ws, "客户特殊要求 Other Requirement", lx.OtherRequirement);

        Section(ws, "四、流程信息 Flow");
        HeadRow(ws, "版本 Version", lx.VersionNo);
        HeadRow(ws, "研发工程师 Engineer", Join(lx.EngineerCode, EmpName(lx.EngineerCode)));
        HeadRow(ws, "提交时间 Submit Time", lx.SubmitTime?.ToString("yyyy-MM-dd HH:mm"));
        HeadRow(ws, "创建人 Created By", Join(lx.CreatePersonCode, EmpName(lx.CreatePersonCode)));
        HeadRow(ws, "创建时间 Created At", lx.CreateTime?.ToString("yyyy-MM-dd HH:mm"));

        Finish(ws);
        return Save(wb);
    }

    // ==================== 规格书一~七节（GG / CR 共用）====================

    private void AppendSpecSections(IXLWorksheet ws, MotorDocExtra extra, bool includeSampleChecks)
    {
        Section(ws, "一、产品功性能要求及参数规格表 Product Functional - Performance Requirements & Parameter Specification Sheet");
        SubHeader(ws, "参数名称 Parameter", "", "", "参数说明 Parameter Description");

        HeadRow(ws, "输入电压 Input Voltage(V)", Join(Field(extra, MotorDocKeys.InputVoltage), Field(extra, MotorDocKeys.InputVoltage + "Other")));
        HeadRow(ws, "额定扭矩 Rated Torque(N.m)", Field(extra, MotorDocKeys.RatedTorque));
        HeadRow(ws, "运行速度 Speed(rpm&cm/s)", Join(Field(extra, MotorDocKeys.Speed), Field(extra, "speedLevels")));
        HeadRow(ws, "额定功率 Rated Power(W)", Field(extra, MotorDocKeys.RatedPower));
        HeadRow(ws, "运行噪音 Operation Noise(dBA)", Field(extra, MotorDocKeys.Noise));
        HeadRow(ws, "遥控频率 Radio Frequency", Join(Field(extra, MotorDocKeys.RadioFrequency), Field(extra, MotorDocKeys.RfDistance)));
        HeadRow(ws, "遥控协议 Protocol", Field(extra, MotorDocKeys.Protocol));
        HeadRow(ws, "遥控技术 Radio Mode", Field(extra, MotorDocKeys.RadioMode));
        HeadRow(ws, "RF 信号距离 Smart Module Distance", Field(extra, MotorDocKeys.RfDistance));
        HeadRow(ws, "智能模块 Smart Module", Field(extra, MotorDocKeys.SmartModule));
        HeadRow(ws, "功能 Function", Join(Field(extra, MotorDocKeys.Function), Field(extra, "functionOther")));
        HeadRow(ws, "输入电源 Power Supply", Field(extra, MotorDocKeys.PowerSupply));
        HeadRow(ws, "内置锂电 Internal Lithium Battery", Join(Field(extra, MotorDocKeys.InternalBattery), Field(extra, "batteryCells"), Field(extra, "batteryMah")));
        HeadRow(ws, "电池续航 Battery Endurance", Field(extra, MotorDocKeys.BatteryEndurance));
        HeadRow(ws, "电池类型 Battery Type", Join(Field(extra, MotorDocKeys.BatteryType), Field(extra, "batteryTypeOther")));
        HeadRow(ws, "电池额定容量 Battery Rated Capacity", Field(extra, MotorDocKeys.BatteryCapacity));
        HeadRow(ws, "负载 Load", Field(extra, "batteryTestLoad"));
        HeadRow(ws, "工作制 Duty Cycle", Field(extra, "batteryTestDuty"));
        HeadRow(ws, "运行高度 Travel Height", Field(extra, "batteryTestHeight"));
        HeadRow(ws, "充电可使用次数 Operation Cycles", Field(extra, "batteryTestCycles"));
        HeadRow(ws, "充电方式 Recharge Port", Field(extra, MotorDocKeys.RechargePort));
        HeadRow(ws, "快充 Fast Charge", Field(extra, MotorDocKeys.FastCharge));
        HeadRow(ws, "行程头 Motor Head", Field(extra, MotorDocKeys.MotorHead));
        HeadRow(ws, "输出轴 Shaft", Field(extra, MotorDocKeys.Shaft));
        HeadRow(ws, "线材型号 / 规格 Cord", Field(extra, MotorDocKeys.Cord));
        HeadRow(ws, "线材认证 Cord Certified", Field(extra, MotorDocKeys.CordCertified));
        HeadRow(ws, "线材颜色 Color", Join(Field(extra, MotorDocKeys.CordColor), Field(extra, "cordColorOther")));
        HeadRow(ws, "线材类型 Cord Type", Join(Field(extra, MotorDocKeys.CordType), Field(extra, "cordTypeOther")));
        HeadRow(ws, "线材是否带插头 Cord Plug", Join(Field(extra, MotorDocKeys.CordPlug), Field(extra, "cordPlugOther")));
        HeadRow(ws, "线材长度 Cord Length", Join(Field(extra, MotorDocKeys.CordLength), Field(extra, "cordLengthOther")));
        HeadRow(ws, "外管 Motor Tube", Join(Field(extra, "tubeDiameter"), Field(extra, "tubeColor"), Field(extra, "tubeLengthLimit")));
        HeadRow(ws, "长度 Motor Length(MM)", Field(extra, MotorDocKeys.MotorLength));
        HeadRow(ws, "产品重量 Motor Weight(KG)", Field(extra, MotorDocKeys.MotorWeight));
        HeadRow(ws, "持续运行时间 Protection Time", Field(extra, MotorDocKeys.ProtectionTime));
        HeadRow(ws, "产品应用场合 Application Occasions", Field(extra, MotorDocKeys.Applications));
        HeadRow(ws, "认证要求 Certification Requirements", Join(Field(extra, MotorDocKeys.Certification), Field(extra, "certificationOther")));
        HeadRow(ws, "质保期限 Warranty Period", Field(extra, MotorDocKeys.Warranty));
        HeadRow(ws, "产品特殊功能 Special Function", Field(extra, MotorDocKeys.SpecialFunction));
        HeadRow(ws, "客户特殊要求 Other Requirement", Field(extra, MotorDocKeys.OtherRequirement));

        Section(ws, "二、产品使用工况 Product Operating Conditions");
        HeadRow(ws, "使用环境 Application", Join(Field(extra, MotorDocKeys.UsageEnv), Field(extra, "usageOther")));
        HeadRow(ws, "IP 等级 IP Degree", Join(Field(extra, MotorDocKeys.IpDegree), Field(extra, "ipOther")));
        HeadRow(ws, "工作温度 Working Temperature", Field(extra, MotorDocKeys.WorkingTemp));
        HeadRow(ws, "静电防护等级 ESD Protection Level", Field(extra, MotorDocKeys.Esd));
        HeadRow(ws, "客户配套件 Customer matching parts", Field(extra, MotorDocKeys.CustomerParts));
        HeadRow(ws, "客户组装接口尺寸 Customer assembly interface dimensions", Field(extra, MotorDocKeys.InterfaceDim));
        HeadRow(ws, "客户使用载荷 Customer usage load", Field(extra, MotorDocKeys.CustomerLoad));

        Section(ws, "三、选配配件 Optional Accessories");
        SubHeader(ws, "序号 No.", "零件 Component", "型号 Type", "备注 Remark");
        foreach (var (row, i) in extra.Accessories.Select((x, i) => (x, i)))
        {
            var r = ws.LastRowUsed()!.RowNumber() + 1;
            ws.Cell(r, 1).Value = (i + 1).ToString("00");
            ws.Cell(r, 2).Value = row.Name ?? "";
            ws.Cell(r, 3).Value = row.Type ?? "";
            ws.Cell(r, 4).Value = row.Remark ?? "";
            ws.Range(r, 4, r, 5).Merge();
            Body(ws.Range(r, 1, r, 5));
        }

        Section(ws, "四、产品尺寸图 Product Dimension Drawing");
        HeadRow(ws, "外形尺寸图 Overall Dimension Drawing", "（见随附图纸）");
        HeadRow(ws, "电控系统图 Electrical Control System Diagram", "（见随附图纸）");

        Section(ws, "五、产品附件 Production Accessories");
        SubHeader(ws, "序号 No.", "零件 Component", "型号 Type", "备注 Remark");
        foreach (var (row, i) in extra.Attachments.Select((x, i) => (x, i)))
        {
            var r = ws.LastRowUsed()!.RowNumber() + 1;
            ws.Cell(r, 1).Value = (i + 1).ToString("00");
            ws.Cell(r, 2).Value = row.Name ?? "";
            ws.Cell(r, 3).Value = row.Type ?? "";
            ws.Cell(r, 4).Value = row.Remark ?? "";
            ws.Range(r, 4, r, 5).Merge();
            Body(ws.Range(r, 1, r, 5));
        }

        Section(ws, "六、包装 Package");
        SubHeader(ws, "项目 Item", "尺寸 Size(mm)", "", "材质 Material", "内含数量 Package Qty");
        foreach (var row in extra.Packages)
        {
            var r = ws.LastRowUsed()!.RowNumber() + 1;
            ws.Cell(r, 1).Value = row.Name ?? "";
            ws.Cell(r, 2).Value = row.Size ?? "";
            ws.Cell(r, 3).Value = row.Material ?? "";
            ws.Cell(r, 4).Value = row.Qty ?? "";
            Body(ws.Range(r, 1, r, 4));
        }

        Section(ws, "七、产品操作说明 Product Operating Instructions");
        foreach (var (code, label) in OperationRows)
        {
            if (code.Length == 0)
            {
                var sr = ws.LastRowUsed()!.RowNumber() + 1;
                ws.Cell(sr, 1).Value = label;
                ws.Range(sr, 1, sr, 5).Merge();
                var sRange = ws.Range(sr, 1, sr, 5);
                sRange.Style.Font.Bold = true;
                sRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                sRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                continue;
            }

            extra.Operations.TryGetValue(code, out var value);
            HeadRow(ws, label, value);
        }
    }

    private static readonly (string Code, string Label)[] OperationRows =
    [
        ("", "① 控制器按键与运行 Controller Keys & Operation"),
        ("keyHandler", "按键与通道定义 Key & Channel Definition"),
        ("opMode", "运行模式 Operating Mode"),
        ("", "② 行程设置 Limit Setting"),
        ("direction", "换向 Direction Reversal"),
        ("setLimits", "设置行程 Set Limits"),
        ("boundary", "边界行程/微调 Boundary Limit / Fine Adjustment"),
        ("favorite", "最佳行程点 Favorite Position"),
        ("", "③ 控制器对码 Controller Pairing"),
        ("addPairing", "添码 Add Controller"),
        ("delPairing", "删码 Delete Controller"),
        ("maxPaired", "配对存储上限 Max Paired Controllers"),
        ("", "④ 运行特性 Operating Features"),
        ("speedLevelsOp", "速度与挡位 Speed & Levels"),
        ("softStart", "缓启缓停 Soft Start / Soft Stop"),
        ("warehouse", "仓储模式 Warehouse Mode"),
        ("childLock", "童锁 Child Lock"),
        ("positionRun", "位置/百分比运行 Position / Percentage Run"),
        ("", "⑤ 遇阻与安全 Obstacle Detection & Safety"),
        ("obstacleBehavior", "遇阻行为 Obstacle Behavior"),
        ("obstacleSensitivity", "遇阻模式与灵敏度 Obstacle Mode & Sensitivity"),
        ("", "⑥ 智能接入 Smart Connectivity"),
        ("smartJoin", "智能配网 Smart Commissioning"),
        ("networkReset", "重置网络 Network Reset / Unbind"),
        ("", "⑦ 状态提示 Status Indication"),
        ("ledCodes", "指示灯 LED Codes"),
        ("lowBattery", "低压/充电提示 Low Battery / Charging Indication"),
        ("runProtection", "运行保护/断电记忆 Run Protection / Power-loss Memory"),
        ("", "⑧ 恢复与复位 Reset & Restore"),
        ("factoryReset", "恢复出厂 Factory Reset"),
        ("setupButton", "设置键分级速查 Setup Button Quick Reference")
    ];

    // ==================== 版式工具 ====================

    private static IXLWorksheet NewSheet(XLWorkbook wb, string name)
    {
        var ws = wb.Worksheets.Add(name);
        ws.Column(1).Width = 26;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 26;
        ws.Column(4).Width = 26;
        ws.Column(5).Width = 26;
        ws.Style.Font.FontName = "微软雅黑";
        ws.Style.Font.FontSize = 9;
        return ws;
    }

    private static void Title(IXLWorksheet ws, string cn, string en)
    {
        var r1 = ws.LastRowUsed() is null ? 1 : ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(r1, 1).Value = cn;
        ws.Range(r1, 1, r1, 5).Merge();

        var r2 = r1 + 1;
        ws.Cell(r2, 1).Value = en;
        ws.Range(r2, 1, r2, 5).Merge();

        foreach (var r in new[] { r1, r2 })
        {
            var range = ws.Range(r, 1, r, 5);
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = r == r1 ? 15 : 10;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Row(r).Height = r == r1 ? 28 : 18;
        }
    }

    private static void Section(IXLWorksheet ws, string text)
    {
        var r = ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(r, 1).Value = text;
        ws.Range(r, 1, r, 5).Merge();
        var range = ws.Range(r, 1, r, 5);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        range.Style.Alignment.WrapText = true;
        ws.Row(r).Height = 20;
    }

    private static void Note(IXLWorksheet ws, string text)
    {
        var r = ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(r, 1).Value = text;
        ws.Range(r, 1, r, 5).Merge();
        var range = ws.Range(r, 1, r, 5);
        range.Style.Font.Italic = true;
        range.Style.Font.FontSize = 8;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    /// <summary>标签 + 值：标签合并 A:B，值合并 C:E。</summary>
    private static void HeadRow(IXLWorksheet ws, string label, string? value)
    {
        var r = ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(r, 1).Value = label;
        ws.Range(r, 1, r, 2).Merge();
        ws.Cell(r, 3).Value = value ?? "";
        ws.Range(r, 3, r, 5).Merge();
        Body(ws.Range(r, 1, r, 5));
    }

    private static void SubHeader(IXLWorksheet ws, params string[] cells)
    {
        var r = ws.LastRowUsed()!.RowNumber() + 1;
        for (var i = 0; i < cells.Length && i < 5; i++)
            ws.Cell(r, i + 1).Value = cells[i];

        // 列数不足 5 时把尾部合并补齐，保持边框完整
        if (cells.Length < 5)
            ws.Range(r, Math.Max(1, cells.Length), r, 5).Merge();

        var range = ws.Range(r, 1, r, 5);
        Body(range);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void DataRow(IXLWorksheet ws, string c1, string c2, string c3, string c4)
    {
        var r = ws.LastRowUsed()!.RowNumber() + 1;
        ws.Cell(r, 1).Value = c1;
        ws.Cell(r, 2).Value = c2;
        ws.Cell(r, 3).Value = c3;
        ws.Cell(r, 4).Value = c4;
        ws.Range(r, 4, r, 5).Merge();
        Body(ws.Range(r, 1, r, 5));
    }

    private static void Body(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Alignment.WrapText = true;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void Finish(IXLWorksheet ws)
    {
        ws.SheetView.FreezeRows(2);
        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.Margins.Top = 0.4;
        ws.PageSetup.Margins.Bottom = 0.4;
        ws.PageSetup.Margins.Left = 0.4;
        ws.PageSetup.Margins.Right = 0.4;
    }

    private static byte[] Save(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ==================== 小工具 ====================

    private static string? Field(MotorDocExtra extra, string key)
        => extra.Fields.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

    private static string? Join(params string?[] parts)
    {
        var list = parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).ToList();
        return list.Count == 0 ? null : string.Join(" / ", list);
    }

    /// <summary>多选字段用逗号连接：勾选逻辑按逗号拆项，用「 / 」连会把「其它」值当成一项吞掉。</summary>
    private static string? JoinComma(params string?[] parts)
    {
        var list = parts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).ToList();
        return list.Count == 0 ? null : string.Join(",", list);
    }

    /// <summary>模板里有固定句式的格子：值空着就返回 null，保留模板原文。</summary>
    private static string? Format(string template, string? value)
        => string.IsNullOrWhiteSpace(value) ? null : string.Format(template, value.Trim());

    /// <summary>内置锂电：是/否 + 节数 + 容量，模板原文是「□是Yes □否No 2节cell，2600 mAh」。</summary>
    private static string? Lithium(MotorDocExtra extra)
    {
        var flag = Field(extra, MotorDocKeys.InternalBattery) ?? "";
        var cells = Field(extra, "batteryCells");
        var mah = Field(extra, "batteryMah");
        if (flag.Length == 0 && cells is null && mah is null) return null;

        var yes = flag.Contains("是", StringComparison.Ordinal);
        var no = flag.Contains("否", StringComparison.Ordinal);
        if (!yes && !no) yes = true;

        var head = $"{(yes ? "☑" : "☐")}是 Yes  {(no ? "☑" : "☐")}否 No";
        var detail = Join(cells is null ? null : cells + " 节 cell", mah is null ? null : mah + " mAh");
        return detail is null ? head : head + "    " + detail.Replace(" / ", "，");
    }

    /// <summary>成品电机电池续航测试：四行说明，模板里本来就是一整格多行文本。</summary>
    private static string? BatteryTest(MotorDocExtra extra)
    {
        var load = Field(extra, "batteryTestLoad");
        var duty = Field(extra, "batteryTestDuty");
        var height = Field(extra, "batteryTestHeight");
        var cycles = Field(extra, "batteryTestCycles");
        if (load is null && duty is null && height is null && cycles is null) return null;

        var lines = new List<string>();
        if (load is not null) lines.Add("负载Load：" + load);
        if (duty is not null) lines.Add("工作制Duty Cycle：" + duty);
        if (height is not null) lines.Add("运行高度Travel Height：" + height);
        if (cycles is not null) lines.Add("运行次数Operation Cycles：" + cycles);
        return string.Join("\n", lines);
    }

    /// <summary>线材长度：长度 + 有无特殊要求。</summary>
    private static string? CordLength(MotorDocExtra extra)
    {
        var length = Field(extra, MotorDocKeys.CordLength);
        var other = Field(extra, "cordLengthOther");
        if (length is null && other is null) return null;

        var lines = new List<string>();
        if (length is not null) lines.Add($"线材长度Cord Length {length} 米M");
        if (other is not null) lines.Add("有无特殊要求Other requirement：" + other);
        return string.Join("\n", lines);
    }

    /// <summary>外管：直径 / 颜色 / 长度限制，只写填了的部分。</summary>
    private static string? Tube(MotorDocExtra extra)
    {
        var diameter = Field(extra, "tubeDiameter");
        var color = Field(extra, "tubeColor");
        var limit = Field(extra, "tubeLengthLimit");
        if (diameter is null && color is null && limit is null) return null;

        var parts = new List<string>();
        if (diameter is not null) parts.Add($"直径Diameter {diameter}mm");
        if (color is not null) parts.Add($"颜色Color {color}");
        if (limit is not null) parts.Add($"长度限制lengthlimit {limit}mm");
        return string.Join("，", parts);
    }

    private static string? Applications(MotorLxModel lx)
    {
        var list = new[]
            {
                lx.Application1, lx.Application2, lx.Application3, lx.Application4, lx.Application5,
                lx.Application6, lx.Application7, lx.Application8, lx.Application9, lx.Application10
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToList();
        return list.Count == 0 ? null : string.Join("、", list);
    }

    private static string? Certifications(MotorLxModel lx)
    {
        var list = new[]
            {
                lx.SCRequirementUL, lx.SCRequirementCE, lx.SCRequirementFCC,
                lx.SCRequirementROHS, lx.SCRequirementOtherC, lx.SCRequirementOther
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToList();
        return list.Count == 0 ? null : string.Join("、", list);
    }

    private static string Sanitize(string raw)
    {
        var text = raw.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            text = text.Replace(c, '_');
        return text.Replace('/', '_').Replace('\\', '_');
    }
}
