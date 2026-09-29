namespace ZYQDManager.DataModels;

/// <summary>电机承认书 T_ZYQD_Motor_CR</summary>
public class MotorCrModel
{
    public int Id { get; set; }
    public string ProjectGuid { get; set; } = "";
    public string DocGuid { get; set; } = "";
    public string VersionNo { get; set; } = "vn";
    public bool IsDraft { get; set; } = true;
    public DateTime? ForwardedAt { get; set; }
    public string? Status { get; set; }
    public string? SourceLxDocGuid { get; set; }
    public string? SourceGgDocGuid { get; set; }

    public string? Customer { get; set; }
    public string? DemandPerson { get; set; }
    public string? CustomerPartCode { get; set; }
    public string? MotorModelSpec { get; set; }
    public string? Description { get; set; }
    public string? ProductDescription { get; set; }
    public DateTime? TimeToRequest { get; set; }
    public DateTime? RequiredCompletionTime { get; set; }

    public string? InputVoltage { get; set; }
    public string? InputVoltageOther { get; set; }
    public string? RatedPower { get; set; }
    public string? RatedTorque { get; set; }
    public string? RatedCurrentNoLoad { get; set; }
    public string? RatedCurrentFullLoad { get; set; }

    public string? ChargingMethod { get; set; }
    public string? ChargingMethodNo { get; set; }
    public string? JstPlugV { get; set; }
    public string? JstPlugA { get; set; }

    public string? LeadWireSpecification { get; set; }
    public string? CordCertified { get; set; }
    public string? CordColor { get; set; }
    public string? CordColorOther { get; set; }
    public string? CordType { get; set; }
    public string? CordTypeOther { get; set; }
    public string? CordPlug { get; set; }
    public string? CordLength { get; set; }
    public string? CordOtherRequirements { get; set; }
    public string? WireLength { get; set; }

    public string? RatedLevelSpeed { get; set; }
    public string? DefaultRunningSpeed { get; set; }

    public string? RemoteFrequency { get; set; }
    public string? RemoteFrequencyOther { get; set; }
    public string? RemoteDistance { get; set; }
    public string? RemoteTechnology { get; set; }
    public string? RemoteProtocol { get; set; }
    public string? RemoteProtocolOther { get; set; }

    public string? MotorFunction { get; set; }
    public string? FunctionOther { get; set; }

    public string? SmartModule { get; set; }
    public string? SmartModuleOther { get; set; }
    public string? SignalReceivingDistance { get; set; }
    public string? SignalReceivingProtocol { get; set; }

    public string? IpLevel { get; set; }
    public string? IpLevelOther { get; set; }
    public string? OperatingNoise { get; set; }
    public string? TestingStandard { get; set; }
    public string? FastCharge { get; set; }

    public string? InputPower { get; set; }
    public string? BuiltInLithiumWhether { get; set; }
    public string? BuiltInLithiumBatteryCell { get; set; }
    public string? BuiltInLithiumBatteryMah { get; set; }

    public string? BatteryEnduranceDesc { get; set; }
    public string? BatteryType { get; set; }
    public string? BatteryTypeOther { get; set; }
    public string? BatteryRatedCapacity { get; set; }
    public string? BatteryTestLoad { get; set; }
    public string? BatteryTestDutyCycle { get; set; }
    public string? BatteryTestTravelHeight { get; set; }
    public string? BatteryTestCycles { get; set; }

    public string? OuterTubeDiameter { get; set; }
    public string? OuterTubeColor { get; set; }
    public string? OuterTubeLengthLimit { get; set; }
    public string? MotorWeight { get; set; }
    public string? MotorLength { get; set; }
    public string? MaxRunningTime { get; set; }

    public string? MaxWidth { get; set; }
    public string? MaxHeight { get; set; }
    public string? PayloadWeight { get; set; }

    public string? MotorHead { get; set; }
    public string? Shaft { get; set; }
    public string? OperationMethod { get; set; }
    public string? ProgramCode { get; set; }
    public string? CrownAndDrive { get; set; }
    public string? RemoteController { get; set; }
    public string? UsageEnv { get; set; }
    public string? UsageOther { get; set; }
    public string? MarketDemandYear { get; set; }
    public string? MarketDemandQuantity { get; set; }

    public string? IsPrototypeNeeded { get; set; }
    public DateTime? PrototypeProvidedTime { get; set; }
    public string? PrototypeQuantity { get; set; }
    public DateTime? ExpectedPrototypeConfirmTime { get; set; }

    public string? SCRequirementUL { get; set; }
    public string? SCRequirementCE { get; set; }
    public string? SCRequirementFCC { get; set; }
    public string? SCRequirementROSH { get; set; }
    public string? SCRequirementOtherC { get; set; }
    public string? SCRequirementOther { get; set; }

    public string? SpecialFunction { get; set; }
    public string? PackageMaxSize { get; set; }
    public string? PackageMaxWeight { get; set; }
    public string? PackageOther { get; set; }
    public string? OtherRequirement { get; set; }

    public string? SilkScreenDrawingType { get; set; }

    public string? Application1 { get; set; }
    public string? Application2 { get; set; }
    public string? Application3 { get; set; }
    public string? Application4 { get; set; }
    public string? Application5 { get; set; }
    public string? Application6 { get; set; }
    public string? Application7 { get; set; }
    public string? Application8 { get; set; }
    public string? Application9 { get; set; }
    public string? Application10 { get; set; }
    public string? ApplicationOccasion { get; set; }

    public string? PowerSupplyNote { get; set; }
    public string? IllustrateOther { get; set; }

    public string? DemandChange { get; set; }
    public string? SampleConfirmation { get; set; }
    public string? SampleSales { get; set; }
    public string? UnderControl { get; set; }

    public string? MotorHeadModel { get; set; }
    public string? MotorHeadColor { get; set; }
    public string? MotorHeadRemark { get; set; }

    public string? ShaftModel { get; set; }
    public string? ShaftRemark { get; set; }

    public string? CrownModel { get; set; }
    public string? CrownMaterial { get; set; }
    public string? CrownColor { get; set; }
    public string? CrownRemark { get; set; }

    public string? DriveAdaptorModel { get; set; }
    public string? DriveAdaptorMaterial { get; set; }
    public string? DriveAdaptorColor { get; set; }
    public string? DriveAdaptorRemark { get; set; }

    public string? LabelModel { get; set; }
    public string? LabelRemark { get; set; }

    public string? ManualModel { get; set; }
    public string? ManualMaterial { get; set; }
    public string? ManualRemark { get; set; }

    public string? BatteryModel { get; set; }
    public string? BatteryRemark { get; set; }

    public string? PackageSize { get; set; }
    public string? PackageMaterial { get; set; }
    public string? PackageQuantity { get; set; }

    public string? MasterCartonSize { get; set; }
    public string? MasterCartonMaterial { get; set; }
    public string? MasterCartonQuantity { get; set; }

    public string? ShippingMarkSize { get; set; }
    public string? ShippingMarkMaterial { get; set; }
    public string? ShippingMarkQuantity { get; set; }

    public string? PalletSize { get; set; }
    public string? PalletMaterial { get; set; }
    public string? PalletQuantity { get; set; }

    public string? PalletShippingMarkSize { get; set; }
    public string? PalletShippingMarkMaterial { get; set; }
    public string? PalletShippingMarkQuantity { get; set; }

    public string? DocRemark { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? CreatePersonCode { get; set; }
    public string? CreatePersonName { get; set; }
    public string? UpdatePersonCode { get; set; }
    public string? UpdatePersonName { get; set; }
    public string DeleteFlag { get; set; } = "";
    public DateTime? DeleteTime { get; set; }
    public string? DeletePersonCode { get; set; }
}
