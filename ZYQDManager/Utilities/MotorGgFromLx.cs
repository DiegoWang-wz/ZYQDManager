using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;

namespace ZYQDManager.Utilities;

/// <summary>
/// 立项单 → 规格书的字段映射。
///
/// 两处都必须走这里，否则口径会漂移：
/// 1. <see cref="Services.MotorFlowService.SubmitLxAsync"/> 提交立项单时自动建规格书草稿；
/// 2. 规格书页上的「重新带入立项单」。
/// 曾经就是两套逻辑 —— 提交时只带了客户/型号/品名三个字段，
/// 页面上手动带入才带全，导致研发打开草稿看到大片空白。
///
/// 写入原则：只填空字段（SetIfEmpty），研发已经填过的不覆盖。
/// </summary>
public static class MotorGgFromLx
{
    /// <summary>表头字段（客户、需求人、日期等）从立项单搬过来。</summary>
    public static void FillHead(MotorGgModel gg, MotorLxModel lx)
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

    /// <summary>正文参数（MotorDocExtra.Fields）从立项单搬过来。</summary>
    public static void Fill(MotorDocExtra extra, MotorLxModel lx)
    {
        SetIfEmpty(extra, MotorDocKeys.ProductName, lx.Description);
        SetIfEmpty(extra, MotorDocKeys.PartCode, lx.MotorModelSpec);
        SetIfEmpty(extra, MotorDocKeys.Customer, lx.Customer);
        SetIfEmpty(extra, MotorDocKeys.InputVoltage, lx.InputVoltage);
        SetIfEmpty(extra, MotorDocKeys.RatedTorque, lx.RatedTorque);
        SetIfEmpty(extra, MotorDocKeys.Speed,
            string.IsNullOrWhiteSpace(lx.DefaultRunningSpeed) ? lx.RatedLevelSpeed : lx.DefaultRunningSpeed);
        SetIfEmpty(extra, MotorDocKeys.RatedPower, lx.RatedPower);
        SetIfEmpty(extra, MotorDocKeys.Noise, lx.OperatingNoise);
        SetIfEmpty(extra, MotorDocKeys.RadioFrequency, lx.RemoteFrequency);
        SetIfEmpty(extra, MotorDocKeys.Protocol, lx.RemoteProtocol);
        SetIfEmpty(extra, MotorDocKeys.RadioMode, lx.RemoteTechnology);
        SetIfEmpty(extra, MotorDocKeys.RfDistance, lx.RemoteDistance);
        SetIfEmpty(extra, MotorDocKeys.SmartModule, lx.SmartModule);
        SetIfEmpty(extra, MotorDocKeys.Function, lx.MotorFunction);
        SetIfEmpty(extra, MotorDocKeys.PowerSupply, lx.InputPower);
        SetIfEmpty(extra, MotorDocKeys.InternalBattery, lx.BuiltInLithiumWhether);
        SetIfEmpty(extra, MotorDocKeys.MotorHead, lx.MotorHead);
        SetIfEmpty(extra, MotorDocKeys.Shaft, lx.Shaft);
        SetIfEmpty(extra, MotorDocKeys.CordLength, lx.CordLength);
        SetIfEmpty(extra, MotorDocKeys.ProtectionTime, lx.MaxRunningTime);
        SetIfEmpty(extra, MotorDocKeys.Applications, lx.Application1);
        SetIfEmpty(extra, MotorDocKeys.UsageEnv, lx.UsageEnv);
        SetIfEmpty(extra, MotorDocKeys.IpDegree, lx.IpLevel);
        SetIfEmpty(extra, MotorDocKeys.SpecialFunction, lx.SpecialFunction);
        SetIfEmpty(extra, MotorDocKeys.OtherRequirement, lx.OtherRequirement);
        SetIfEmpty(extra, "tubeDiameter", lx.OuterTubeDiameter);
        SetIfEmpty(extra, "tubeColor", lx.OuterTubeColor);
        SetIfEmpty(extra, "tubeLengthLimit", lx.OuterTubeLengthLimit);
        SetIfEmpty(extra, MotorDocKeys.MotorLength, lx.MotorLength);
        SetIfEmpty(extra, MotorDocKeys.MotorWeight, lx.MotorWeight);
        SetIfEmpty(extra, MotorDocKeys.RechargePort, lx.ChargingMethod);
        SetIfEmpty(extra, MotorDocKeys.FastCharge, lx.FastCharge);
        SetIfEmpty(extra, "usageOther", lx.UsageEnvOther);
        SetIfEmpty(extra, "ipOther", lx.IpLevelOther);

        var certs = string.Join(",",
            new[] { lx.SCRequirementUL, lx.SCRequirementCE, lx.SCRequirementFCC, lx.SCRequirementROHS }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));
        SetIfEmpty(extra, MotorDocKeys.Certification, certs);
        SetIfEmpty(extra, "certificationOther", lx.SCRequirementOther);

        var cord = string.Join("，", new[] { lx.LeadWireSpecification, lx.CordType }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim()));
        SetIfEmpty(extra, MotorDocKeys.Cord, cord);
    }

    private static void SetIfEmpty(MotorDocExtra extra, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!extra.Fields.ContainsKey(key) || string.IsNullOrWhiteSpace(extra.Fields[key]))
            extra.Fields[key] = value!.Trim();
    }
}
