using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataModels;

namespace ZYQDManager.Services;

public class MotorLxService
{
    private readonly SqlServerDbContext _db;

    public MotorLxService(SqlServerDbContext db)
    {
        _db = db;
    }

    public Task<MotorLxModel?> GetByDocGuidAsync(string docGuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(docGuid))
            return Task.FromResult<MotorLxModel?>(null);

        return _db.MotorLx.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DocGuid == docGuid && x.DeleteFlag != "X", ct);
    }

    /// <summary>
    /// 新建：首次保存才生成 ProjectGuid/DocGuid 并插入。
    /// 更新：按 DocGuid 更新草稿内容。
    /// </summary>
    public async Task<(string ProjectGuid, string DocGuid, bool Created)> SaveDraftAsync(
        MotorLxModel input,
        string? empNo,
        string? empName,
        CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var isNew = string.IsNullOrWhiteSpace(input.DocGuid) || string.IsNullOrWhiteSpace(input.ProjectGuid);

        if (isNew)
        {
            var row = new MotorLxModel
            {
                ProjectGuid = Guid.NewGuid().ToString("N"),
                DocGuid = Guid.NewGuid().ToString("N"),
                VersionNo = "vn",
                IsDraft = true,
                Status = "草稿",
                CreateTime = now,
                UpdateTime = now,
                CreatePersonCode = empNo,
                CreatePersonName = empName,
                UpdatePersonCode = empNo,
                UpdatePersonName = empName,
                DeleteFlag = ""
            };
            CopyBusinessFields(input, row);
            _db.MotorLx.Add(row);
            await _db.SaveChangesAsync(ct);
            return (row.ProjectGuid, row.DocGuid, true);
        }

        var existing = await _db.MotorLx
            .FirstOrDefaultAsync(x => x.DocGuid == input.DocGuid && x.DeleteFlag != "X", ct)
            ?? throw new InvalidOperationException("未找到立项单，可能已删除");

        CopyBusinessFields(input, existing);
        existing.UpdateTime = now;
        existing.UpdatePersonCode = empNo;
        existing.UpdatePersonName = empName;
        if (string.IsNullOrWhiteSpace(existing.Status))
            existing.Status = existing.IsDraft ? "草稿" : existing.Status;

        await _db.SaveChangesAsync(ct);
        return (existing.ProjectGuid, existing.DocGuid, false);
    }

    private static void CopyBusinessFields(MotorLxModel src, MotorLxModel dest)
    {
        dest.Customer = src.Customer;
        dest.DemandPerson = src.DemandPerson;
        dest.CustomerPartCode = src.CustomerPartCode;
        dest.MotorModelSpec = src.MotorModelSpec;
        dest.Description = src.Description;
        dest.TimeToRequest = src.TimeToRequest;
        dest.RequiredCompletionTime = src.RequiredCompletionTime;

        dest.InputVoltage = src.InputVoltage;
        dest.InputVoltageOther = src.InputVoltageOther;
        dest.RatedCurrentNoLoad = src.RatedCurrentNoLoad;
        dest.RatedCurrentFullLoad = src.RatedCurrentFullLoad;
        dest.RatedPower = src.RatedPower;
        dest.RatedTorque = src.RatedTorque;
        dest.RatedLevelSpeed = src.RatedLevelSpeed;
        dest.DefaultRunningSpeed = src.DefaultRunningSpeed;
        dest.OperatingNoise = src.OperatingNoise;
        dest.FastCharge = src.FastCharge;
        dest.MaxRunningTime = src.MaxRunningTime;
        dest.MotorHead = src.MotorHead;
        dest.Shaft = src.Shaft;
        dest.OuterTubeDiameter = src.OuterTubeDiameter;
        dest.OuterTubeColor = src.OuterTubeColor;
        dest.OuterTubeLengthLimit = src.OuterTubeLengthLimit;
        dest.MotorWeight = src.MotorWeight;
        dest.MotorLength = src.MotorLength;
        dest.OperationMethod = src.OperationMethod;
        dest.ProgramCode = src.ProgramCode;
        dest.CrownAndDrive = src.CrownAndDrive;
        dest.RemoteController = src.RemoteController;
        dest.ChargingMethod = src.ChargingMethod;
        dest.ChargingMethodNo = src.ChargingMethodNo;
        dest.JstPlugV = src.JstPlugV;
        dest.JstPlugA = src.JstPlugA;

        dest.LeadWireSpecification = src.LeadWireSpecification;
        dest.CordCertified = src.CordCertified;
        dest.CordColor = src.CordColor;
        dest.CordColorOther = src.CordColorOther;
        dest.CordType = src.CordType;
        dest.CordTypeOther = src.CordTypeOther;
        dest.CordPlug = src.CordPlug;
        dest.CordLength = src.CordLength;
        dest.CordOtherRequirements = src.CordOtherRequirements;
        dest.WireLength = src.WireLength;

        dest.RemoteFrequency = src.RemoteFrequency;
        dest.RemoteFrequencyOther = src.RemoteFrequencyOther;
        dest.MotorFunction = src.MotorFunction;
        dest.RemoteDistance = src.RemoteDistance;
        dest.RemoteTechnology = src.RemoteTechnology;
        dest.RemoteProtocol = src.RemoteProtocol;
        dest.RemoteProtocolOther = src.RemoteProtocolOther;
        dest.SmartModule = src.SmartModule;
        dest.SmartModuleOther = src.SmartModuleOther;
        dest.SignalReceivingDistance = src.SignalReceivingDistance;
        dest.SignalReceivingProtocol = src.SignalReceivingProtocol;

        dest.BuiltInLithiumWhether = src.BuiltInLithiumWhether;
        dest.BuiltInLithiumBatteryCell = src.BuiltInLithiumBatteryCell;
        dest.BuiltInLithiumBattery2 = src.BuiltInLithiumBattery2;
        dest.InputPower = src.InputPower;
        dest.IpLevel = src.IpLevel;
        dest.IpLevelOther = src.IpLevelOther;
        dest.UsageEnv = src.UsageEnv;
        dest.UsageEnvOther = src.UsageEnvOther;

        dest.Application1 = src.Application1;
        dest.Application2 = src.Application2;
        dest.Application3 = src.Application3;
        dest.Application4 = src.Application4;
        dest.Application5 = src.Application5;
        dest.Application6 = src.Application6;
        dest.Application7 = src.Application7;
        dest.Application8 = src.Application8;
        dest.Application9 = src.Application9;
        dest.Application10 = src.Application10;

        dest.MaxWidth = src.MaxWidth;
        dest.MaxHeight = src.MaxHeight;
        dest.PayloadWeight = src.PayloadWeight;
        dest.MarketDemandYear = src.MarketDemandYear;
        dest.MarketDemandQuantity = src.MarketDemandQuantity;
        dest.IsPrototypeNeeded = src.IsPrototypeNeeded;
        dest.PrototypeProvidedTime = src.PrototypeProvidedTime;
        dest.PrototypeQuantity = src.PrototypeQuantity;
        dest.ExpectedPrototypeConfirmTime = src.ExpectedPrototypeConfirmTime;

        dest.SCRequirementUL = src.SCRequirementUL;
        dest.SCRequirementCE = src.SCRequirementCE;
        dest.SCRequirementFCC = src.SCRequirementFCC;
        dest.SCRequirementROHS = src.SCRequirementROHS;
        dest.SCRequirementOtherC = src.SCRequirementOtherC;
        dest.SCRequirementOther = src.SCRequirementOther;

        dest.SpecialFunction = src.SpecialFunction;
        dest.PackageMaxSize = src.PackageMaxSize;
        dest.PackageMaxWeight = src.PackageMaxWeight;
        dest.PackageOther = src.PackageOther;
        dest.OtherRequirement = src.OtherRequirement;
        dest.DemandChange = src.DemandChange;
        dest.Remark = src.Remark;
    }
}
