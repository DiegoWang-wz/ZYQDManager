using Microsoft.Extensions.Options;

namespace ZYQDManager.DataDtos;

public sealed class CotractPiQueryDto
{
    public string DocumentType { get; set; } = "客户PI";
    public string FilterColumn { get; set; } = "Customer";
    public string? Keyword { get; set; }
    public string? ClientNo { get; set; }
    public bool RequireClientNo { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public bool ViewAll { get; set; }
    public string? ViewerEmpNo { get; set; }
    public string? SortLabel { get; set; }
    public bool SortDescending { get; set; } = true;
}

public static class CotractPiTypes
{
    private static IOptionsMonitor<SelectOptions>? _options;

    public static void Bind(IOptionsMonitor<SelectOptions> options) => _options = options;

    private static SelectOptions Opt => _options?.CurrentValue ?? SelectOptions.Defaults;

    public static IReadOnlyList<string> CustomerPi => Opt.ResolvedOrderTypes();
    public static IReadOnlyList<string> CustomerCi
        => CustomerPi.Select(ToCustomerCiType).ToList();
    public static IReadOnlyList<string> TradeTerms => Opt.ResolvedTradeTerms();
    public static IReadOnlyList<SelectOptionItem> PaymentMethods => Opt.ResolvedPaymentMethods();
    public static string DefaultTradeTerm
    {
        get
        {
            var v = (Opt.DefaultTradeTerm ?? "").Trim();
            return v.Length > 0 ? v : "FOB";
        }
    }

    public static string NormalizeClientNo(string? clientNo)
    {
        var s = (clientNo ?? "").Trim();
        if (s.Length == 4 && s.All(char.IsDigit))
            return "1000" + s;
        return s;
    }

    public static string ResolveTemplateType(string? orderType, string? clientNo)
    {
        var force = (Opt.ForceJckClientNo ?? "").Trim();
        if (force.Length > 0 && NormalizeClientNo(clientNo) == force)
            return "JCK";

        var key = BaseOrderType(orderType);
        if (key.Length > 0 && Opt.OrderTypeTemplates.TryGetValue(key, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
            return mapped.Trim();

        var fallback = (Opt.DefaultTemplate ?? "").Trim();
        return fallback.Length > 0 ? fallback : "JC";
    }

    public static string BaseOrderType(string? orderType)
    {
        var key = (orderType ?? "").Trim();
        const string ci = "-客户CI";
        const string bg = "-报关PI";
        if (key.EndsWith(ci, StringComparison.Ordinal))
            return key[..^ci.Length];
        if (key.EndsWith(bg, StringComparison.Ordinal))
            return key[..^bg.Length];
        return key;
    }

    public static string ToCustomerCiType(string? orderType)
    {
        var baseType = BaseOrderType(orderType);
        if (baseType.Length == 0)
            baseType = CustomerPi.Count > 0 ? CustomerPi[0] : "标准订单";
        return baseType + "-客户CI";
    }

    public static string DocumentKind(string? orderType)
    {
        var s = orderType ?? "";
        if (s.Contains("报关PI", StringComparison.Ordinal))
            return "报关PI";
        if (s.Contains("CI", StringComparison.Ordinal))
            return "客户CI";
        return "PI";
    }

    public static bool IsCustomerCi(string? orderType)
        => DocumentKind(orderType) == "客户CI";

    public static string InvoiceTitle(string? orderType)
        => IsCustomerCi(orderType) ? "COMMERCIAL INVOICE" : "PROFORMA INVOICE";

    public static string InvoicePrefix(string templateType)
        => templateType == "JCK" ? "JC" : templateType;

    public static string ResolveVar12(string templateType)
        => templateType is "JCK" or "JC" ? "JC" : "JS";

    public static string TemplateFileName(string templateType) => templateType switch
    {
        "JS" => "PI合同模板英文.xlsx",
        "JCK" => "PI合同模板中文-捷昌进出口.xlsx",
        _ => "PI合同模板中文.xlsx"
    };

    public static string FormatDate(DateTime? value)
        => value?.ToString("yyyy-MM-dd") ?? "";

    public static string FormatDate(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0) return "";
        s = s.Replace('/', '-').Replace('.', '-');
        if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d)
            || DateTime.TryParse(s, out d))
            return d.ToString("yyyy-MM-dd");
        return s;
    }

    /// <summary>避免 Razor 对 DateTime?/string? 重载解析失败。</summary>
    public static string FormatDateValue(object? value) => value switch
    {
        null => "",
        DateTime dt => FormatDate((DateTime?)dt),
        _ => FormatDate(Convert.ToString(value))
    };

    public static string CurrencySymbol(string? currencyUnit)
    {
        var u = (currencyUnit ?? "").Trim().ToUpperInvariant();
        return u switch
        {
            "USD" => "$",
            "EUR" => "€",
            "CAD" => "C$",
            _ => "¥"
        };
    }

    /// <summary>FOB/EXW 无运费保费；C&amp;F 仅运费；CIF/DAP/DDU/DDP 两者都有。</summary>
    public static bool ShowShippingFee(string? tradeTerm)
        => ResolveFeeVisibility(tradeTerm).Ship;

    public static bool ShowInsurance(string? tradeTerm)
        => ResolveFeeVisibility(tradeTerm).Insure;

    public static (bool Ship, bool Insure) ResolveFeeVisibility(string? tradeTerm)
    {
        var t = (tradeTerm ?? "").Trim().ToUpperInvariant();
        if (t.Length == 0)
            return (false, false);
        if (HasTerm(t, "CIF", "CIP", "DAP", "DDU", "DDP", "DAT", "DPU"))
            return (true, true);
        if (HasTerm(t, "C&F", "CFR", "CNF"))
            return (true, false);
        return (false, false);
    }

    private static bool HasTerm(string trade, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (trade == key || trade.StartsWith(key + " ", StringComparison.Ordinal)
                || trade.StartsWith(key + ",", StringComparison.Ordinal)
                || trade.StartsWith(key + "-", StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}

public sealed class CrmClientDto
{
    public string? ClientNo { get; set; }
    public string? ClientName { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? Tel { get; set; }
    public string? FullName { get; set; }
    public string? Mail { get; set; }
    public string? ShortName { get; set; }

    public string CombinedAddress => $"{Country} {Address}".Trim();
    public string CombinedTel => $"Attn:{FullName}/Tel: {Tel}";
    public string OptionLabel
    {
        get
        {
            var name = (ClientName ?? "").Trim();
            var addr = (Address ?? "").Trim();
            if (name.Length > 0 && addr.Length > 0) return $"{name} // {addr}";
            if (name.Length > 0) return name;
            if (addr.Length > 0) return addr;
            var no = (ClientNo ?? "").Trim();
            return no.Length > 0 ? no : "收货地址";
        }
    }
}

public sealed class CotractPiLineDto
{
    public string Uid { get; set; } = Guid.NewGuid().ToString("N");
    public string? materiaCust { get; set; }
    public string? materialNo { get; set; }
    public string? materialName { get; set; }
    public string? materialModel { get; set; }
    public string? materiaSpecs { get; set; }
    public string? materiaPowered { get; set; }
    public string? Notes { get; set; }
    public double qty { get; set; }
    public string? var7 { get; set; }
    public string? currency_unit { get; set; }
    public double price { get; set; }
    public double origin_price { get; set; }
    public string? var8 { get; set; }
    public double ext { get; set; }
}

public sealed class CotractPiCompanyInfo
{
    public string Title1 { get; init; } = "";
    public string Title2 { get; init; } = "";
    public string Title3 { get; init; } = "";
    public string Title4 { get; init; } = "";
    public string Bank1 { get; init; } = "";
    public string Bank2 { get; init; } = "";
    public string Bank3 { get; init; } = "";
    public string Bank4 { get; init; } = "";
    public string Bank5 { get; init; } = "";
    public string Bank6 { get; init; } = "";
    public bool ShowTitle4 { get; init; } = true;

    public static CotractPiCompanyInfo For(string templateType, string? currencyUnit)
    {
        var curr = CotractPiTypes.CurrencySymbol(currencyUnit);
        if (templateType == "JS")
        {
            return new CotractPiCompanyInfo
            {
                Title1 = "J-Star Motion Corporation",
                Title2 = "ADD: 13617 Woodlawn Hills Dr, Cedar Springs, MI 49319",
                Title3 = "TEL: 617-319-9610",
                Title4 = "",
                ShowTitle4 = false,
                Bank1 = "SELLER'S BANK INFORMATION:",
                Bank2 = "BENEFICIARY: J-Star Motion Corporation",
                Bank3 = "ADDRESS: 13617 Woodlawn Hills Dr, Cedar Springs, MI 49319",
                Bank4 = "THE ADVISING BANK: JP Morgan Chase",
                Bank5 = "ADDRESS: 270 Park Avenue, New York, NY 10017",
                Bank6 = "NOTE: 5YR WARRANTY ON ALL MOTOR TYPES"
            };
        }

        if (templateType == "JCK")
        {
            return new CotractPiCompanyInfo
            {
                Title1 = "新昌县捷昌进出口有限公司",
                Title2 = "XINCHANG JIECANG IMPORT AND EXPORT CO.,LTD",
                Title3 = "ADD: N0. 19, Xintao Road Qixing Street, Xinchang County, Zhejiang, China 312500",
                Title4 = "TEL: 0086-575-8628 7989; FAX: 0086-575-8629 2500",
                Bank1 = $"SELLER'S BANK INFORMATION({curr}):",
                Bank2 = "BENEFICIARY:XINCHANG JIECANG IMPORT AND EXPORT CO.,LTD",
                Bank3 = "ADDRESS: N0. 19, Xintao Road Qixing Street, Xinchang County, Zhejiang, China",
                Bank4 = "THE ADVISING BANK: BANK OF CHINA XINCHANG SUB-BRANCH",
                Bank5 = "BANK ACCOUNT: 368870956982",
                Bank6 = "SWIFT: BKCHCNBJ92D      TEL:0575-86041909"
            };
        }

        var account = curr == "C$" ? "295046100018800004048"
            : curr is "$" or "€" ? "295046100146300002686"
            : "295046100018010079541";
        return new CotractPiCompanyInfo
        {
            Title1 = "浙江捷昌线性驱动科技股份有限公司",
            Title2 = "Zhejiang Jiecang Linear Motion Technology Co.,Ltd",
            Title3 = "ADD: No 2, Laisheng Road, Qixing Street, Xinchang County, Zhejiang, China 312500",
            Title4 = "TEL: 0086-575-8628 7986; FAX: 0086-575-8629 7960",
            Bank1 = "SELLER'S BANK INFORMATION:",
            Bank2 = "BENEFICIARY: ZHEJIANG JIECANG LINEAR MOTION TECHNOLOGY CO.,LTD",
            Bank3 = "ADDRESS: PROVINCIAL HIGH TECH PARK,XINCHANG COUNTY,ZHEJIANG,CHINA.",
            Bank4 = "THE ADVISING BANK: BANK OF COMMUNICATIONS,SHAOXING BRANCH",
            Bank5 = $"BANK ACCOUNT: {account}",
            Bank6 = "SWIFT: COMM CN SH SXG      TEL:0575-81166066"
        };
    }
}

public static class CotractPiFilterFields
{
    public static readonly (string Key, string Label)[] All =
    [
        ("Customer", "客户"),
        ("INVOICE_NO", "INVOICE NO"),
        ("Person", "需求人"),
        ("var6", "订单类型"),
        ("BILL_TO_PO_REF", "PO号")
    ];
}
