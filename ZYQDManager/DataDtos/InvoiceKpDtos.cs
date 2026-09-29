namespace ZYQDManager.DataDtos;

public sealed class InvoiceKpOptions
{
    public const string SectionName = "InvoiceKp";
    public const string RateHttpClientName = "InvoiceRateApi";

    /// <summary>Sap.Data.Hana 连接串，例：Server=192.168.81.114:30065;UserID=...;Password=...</summary>
    public string HanaConnectionString { get; set; } = "";

    /// <summary>兼容旧配置键（ODBC）；优先使用 HanaConnectionString</summary>
    public string HanaOdbcConnectionString { get; set; } = "";

    public string RateApiBaseUrl { get; set; } = "http://192.168.3.160:38000";
    public string RateApiPath { get; set; } = "jc_cn_hq/SAP_ALL/API_1XpWk";
    public string RateApiUser { get; set; } = "";
    public string RateApiPassword { get; set; } = "";

    /// <summary>按工号限制可见客户编码（KUNNR，去前导 0）；与页面 Controls 权限无关</summary>
    public Dictionary<string, string[]> CustomerScopeByEmpNo { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["930154"] =
        [
            "10006063", "10007742", "10008452", "10005331", "10005993", "10006724", "10008383",
            "10007357", "10008433", "10007107", "10007641", "10008120", "10008978", "10005329",
            "10006917", "10009403", "10009541", "10007745", "10006761", "10009006", "10007021",
            "10006222", "10004708", "10006464", "10006235", "10009003", "10006975", "10008831",
            "10009070", "10005986", "10007725", "10006958", "10007203"
        ]
    };
}

public sealed class InvoiceKpQueryRequest
{
    /// <summary>开始月份 yyyy-MM</summary>
    public string MonthStart { get; set; } = "";

    /// <summary>结束月份 yyyy-MM</summary>
    public string MonthEnd { get; set; } = "";

    /// <summary>客户编码/名称关键词</summary>
    public string? CustomerKeyword { get; set; }

    /// <summary>当前登录工号（用于权限与客户范围）</summary>
    public string EmpNo { get; set; } = "";

    /// <summary>是否系统管理员（管理员可查看）</summary>
    public bool IsAdmin { get; set; }
}

public sealed class InvoiceKpSummaryRow
{
    public string CustomerName { get; set; } = "";
    public string Month { get; set; } = "";
    public string ProductSeries { get; set; } = "";
    public double Qty { get; set; }
    public double AmountUsd { get; set; }
}

public sealed class InvoiceKpQueryResult
{
    public List<InvoiceKpSummaryRow> Rows { get; set; } = [];
    public double TotalQty { get; set; }
    public double TotalAmountUsd { get; set; }
}
