using ZYQDManager.DataModels;

namespace ZYQDManager.DataDtos;

public sealed class SirQueryDto
{
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public string? FilterColumn { get; set; }
    public string? Keyword { get; set; }
    public string? SortLabel { get; set; }
    public bool SortDesc { get; set; } = true;
}

public static class SirFilterFields
{
    public static readonly (string Key, string Label)[] All =
    [
        ("客户", "客户"),
        ("产品型号", "产品型号"),
        ("产品名称", "产品名称"),
        ("业务员", "业务员"),
        ("报告类型", "报告类型"),
    ];
}

public sealed class SirDocumentDto
{
    public ZyqdSirMainModel Main { get; set; } = new();
    public List<ZyqdSirDetailModel> Details { get; set; } = [];
    public List<ZyqdSirSampleForFilingModel> Samples { get; set; } = [];
}

public static class SirReportTypes
{
    public static readonly (string Value, string Label)[] Tests =
    [
        ("remote", "遥控器测试"),
        ("remote_new", "遥控器测试(新)"),
        ("motor", "电机测试"),
        ("putter", "推杆测试"),
    ];

    public static IReadOnlyList<string> ProductsFor(string? reportType) => reportType switch
    {
        "motor" =>
        [
            "基础版电机",
            "JCA-内置RF控制机械行程交流管状电机",
            "JCA-内置RF控制电子行程交流管状电机",
            "JCA-无RF电子行程交流管状电机",
            "JCA-无RF机械行程交流管状电机",
            "JCC-锂电池款开合帘电机",
            "JCC-内置开关电源款开合帘电机",
            "JCC-AE款开合帘电机",
            "JCD-LE款直流管状电机",
            "JCD-TE款直流管状电机",
            "JCD-AE款直流管状电机",
            "常规JCV系列管状电机",
            "内置锂电池款JCV系列管状电机",
            "内置开关电源款JCV系列管状电机",
        ],
        "remote" => ["基础版遥控器"],
        "remote_new" =>
        [
            "充电款单向遥控器",
            "充电款双向遥控器",
            "一次电池款单向遥控器",
            "一次电池款双向遥控器",
        ],
        "putter" => ["双推杆遮阳棚", "单推杆遮阳棚"],
        _ => ["基础版遥控器"],
    };

    public static string DefaultProduct(string? reportType) => ProductsFor(reportType).FirstOrDefault() ?? "基础版遥控器";

    public static string FormatReportType(string? v) => Tests.FirstOrDefault(x => x.Value == v).Label ?? (v ?? "-");
}
