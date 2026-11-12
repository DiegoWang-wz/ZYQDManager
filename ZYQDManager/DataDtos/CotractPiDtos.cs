using Microsoft.Extensions.Options;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.DataDtos;

public sealed class CotractPiQueryDto
{
    public string DocumentType { get; set; } = "客户PI";
    public string FilterColumn { get; set; } = "客户编号";
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
    public static IReadOnlyList<string> CustomsPi
        => CustomerPi.Select(ToCustomsPiType).ToList();
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

    /// <summary>客户编号规范化（短号补 10000000，见 <see cref="ClientCodeHelper"/>）。</summary>
    public static string NormalizeClientNo(string? clientNo)
        => ClientCodeHelper.Expand(clientNo);

    public static bool IsForceJckClient(string? clientNo)
    {
        var force = (Opt.ForceJckClientNo ?? "").Trim();
        return force.Length > 0 && NormalizeClientNo(clientNo) == NormalizeClientNo(force);
    }

    /// <summary>10008831：强制贸易订单（1300）/ 进出口模板。报关 PI、客户 CI 带对应后缀。</summary>
    public static string ForceJckOrderType(string? currentOrderType)
    {
        const string trade = "贸易订单（1300）";
        if (IsCustomerCi(currentOrderType)) return ToCustomerCiType(trade);
        if (IsCustomsPi(currentOrderType)) return ToCustomsPiType(trade);
        return trade;
    }

    public const string OrderStandard = "标准订单";
    public const string OrderTrade1300 = "贸易订单（1300）";
    public const string OrderTrade1300_2001 = "贸易订单（1300-2001）";
    public const string OrderConsignment1300_2001 = "寄售订单（1300-2001）";
    /// <summary>与老系统报关 PI 写死的 J-star Bill To / Ship To 一致。</summary>
    public const string JstarPartyName = "J-Star Motion Corporation";
    public const string JstarPartyAddress = "13617 Woodlawn Hills DrCedar Springs, Ml 49319";
    public const string JstarPartyTel = "Attn:Nelly Li/Tel: 6162168596";
    public const string ConsignmentCustomsInvoiceName = "JS21(J-Star Motion Corporation)";

    public static bool IsJsTradeOrder(string? orderType)
    {
        var key = BaseOrderType(orderType);
        return key.Contains("1300-2001", StringComparison.Ordinal)
               && key.Contains("贸易", StringComparison.Ordinal)
               && !key.Contains("寄售", StringComparison.Ordinal);
    }

    public static bool IsJsConsignmentOrder(string? orderType)
    {
        var key = BaseOrderType(orderType);
        return key.Contains("寄售", StringComparison.Ordinal)
               && key.Contains("1300-2001", StringComparison.Ordinal);
    }

    public static bool IsJstarFamilyOrder(string? orderType)
        => IsJsTradeOrder(orderType) || IsJsConsignmentOrder(orderType);

    public static string ResolveTemplateType(string? orderType, string? clientNo)
    {
        if (IsForceJckClient(clientNo))
            return "JCK";

        var key = BaseOrderType(orderType);
        // 报关 PI：J-star 两类抬头改为捷昌进出口
        if (IsCustomsPi(orderType) && IsJstarFamilyOrder(orderType))
            return "JCK";

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

    public static string ToCustomsPiType(string? orderType)
    {
        var baseType = BaseOrderType(orderType);
        if (baseType.Length == 0)
            baseType = CustomerPi.Count > 0 ? CustomerPi[0] : "标准订单";
        return baseType + "-报关PI";
    }

    /// <summary>多张客户 PI 合成报关 PI：客户、订单类型、贸易方式必须一致。</summary>
    public static string? CustomsMergeBlockReason(IReadOnlyList<ZyqdCotractPiModel> docs)
    {
        if (docs.Count <= 1) return null;
        var client = NormalizeClientNo(docs[0].ClientNo);
        var order = (docs[0].var6 ?? "").Trim();
        var trade = (docs[0].BILL_TO_FOB ?? "").Trim();
        foreach (var doc in docs.Skip(1))
        {
            var sameClient = string.Equals(NormalizeClientNo(doc.ClientNo), client, StringComparison.OrdinalIgnoreCase);
            var sameOrder = string.Equals((doc.var6 ?? "").Trim(), order, StringComparison.Ordinal);
            var sameTrade = string.Equals((doc.BILL_TO_FOB ?? "").Trim(), trade, StringComparison.OrdinalIgnoreCase);
            if (!sameClient || !sameOrder || !sameTrade)
                return "请选择同一客户、同一订单类型、同一贸易方式的客户 PI";
        }
        return null;
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

    public static bool IsCustomsPi(string? orderType)
        => DocumentKind(orderType) == "报关PI";

    public static string InvoiceTitle(string? orderType)
        => IsCustomerCi(orderType) ? "COMMERCIAL INVOICE" : "PROFORMA INVOICE";

    /// <summary>客户 PI 发票号前缀：标准订单 JC，贸易订单（1300） JCK，两种 1300-2001 为 JS。</summary>
    public static string InvoicePrefix(string? orderType)
    {
        var key = BaseOrderType(orderType);
        if (key == OrderTrade1300_2001 || key == OrderConsignment1300_2001)
            return "JS";
        if (key == OrderTrade1300)
            return "JCK";
        return "JC";
    }

    /// <summary>客户 CI、报关 PI 发票号前缀一律 JC。客户 PI 仍按订单类型。</summary>
    public static string DocumentInvoicePrefix(string? orderType)
        => IsCustomerCi(orderType) || IsCustomsPi(orderType) ? "JC" : InvoicePrefix(orderType);

    public static string InvoiceNamePart(string? orderType, string? clientName)
    {
        if (IsCustomsPi(orderType) && IsJsConsignmentOrder(orderType))
            return ConsignmentCustomsInvoiceName;
        return (clientName ?? "").Trim();
    }

    /// <summary>改发票号前缀，保留年份和后面的日期、名称。客户 CI、报关 PI 固定 JC。JCK 要先于 JC 判断。</summary>
    public static string ApplyInvoicePrefix(string? invoiceNo, string? orderType)
    {
        var prefix = DocumentInvoicePrefix(orderType);
        var s = (invoiceNo ?? "").Trim();
        if (s.Length < 4 || !char.IsDigit(s[0]) || !char.IsDigit(s[1]))
            return s;
        var yy = s[..2];
        var rest = s[2..];
        string tail;
        if (rest.StartsWith("JCK", StringComparison.OrdinalIgnoreCase))
            tail = rest[3..];
        else if (rest.StartsWith("JS", StringComparison.OrdinalIgnoreCase))
            tail = rest[2..];
        else if (rest.StartsWith("JC", StringComparison.OrdinalIgnoreCase))
            tail = rest[2..];
        else
            return s;
        return yy + prefix + tail;
    }

    public static bool NeedsCustomsDiscount(string? orderType)
        => IsCustomsPi(orderType) && IsJstarFamilyOrder(orderType);

    public static double MapCrmPriceGroup(string? priceGroup)
        => (priceGroup ?? "").Trim() switch
        {
            "10" => 0.68,
            "20" => 0.70,
            "30" => 0.72,
            "40" => 0.92,
            "50" => 0.98,
            "80" => 0.95,
            _ => 1
        };

    public static bool CustomsUsesJstarBill(string? orderType)
        => IsCustomsPi(orderType) && IsJstarFamilyOrder(orderType);

    public static bool CustomsUsesJstarShip(string? orderType)
        => IsCustomsPi(orderType) && IsJsConsignmentOrder(orderType);

    public static void ApplyJstarPartyToBill(ZyqdCotractPiModel row)
    {
        row.BILL_TO_info1 = JstarPartyName;
        row.BILL_TO_info3 = JstarPartyAddress;
        row.BILL_TO_info4 = JstarPartyTel;
    }

    public static void ApplyJstarPartyToShip(ZyqdCotractPiModel row)
    {
        row.SHIP_TO_info1 = JstarPartyName;
        row.SHIP_TO_info3 = JstarPartyAddress;
        row.SHIP_TO_info4 = JstarPartyTel;
    }

    public static void ApplyCustomsPartyOverrides(ZyqdCotractPiModel row, bool forceCustoms = false)
    {
        if (!forceCustoms && !IsCustomsPi(row.var6))
            return;
        if (IsJsTradeOrder(row.var6))
        {
            ApplyJstarPartyToBill(row);
            return;
        }
        if (IsJsConsignmentOrder(row.var6))
        {
            ApplyJstarPartyToBill(row);
            ApplyJstarPartyToShip(row);
        }
    }

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

    /// <summary>第 15 条：发货 50% / 到货 10 天 50%，走特殊两行布局。</summary>
    public const int SplitBalancePayTypeId = 15;

    public static string SplitBalancePaymentMethod
    {
        get
        {
            var v = (Opt.SplitBalancePaymentMethod ?? "").Trim();
            return v.Length > 0
                ? v
                : "UPON DESPATCH 50%，WITHIN 10 DAYS AFTER ARRIVAL 50%";
        }
    }

    public static bool IsSplitBalancePaymentMethod(string? paymentMethod)
    {
        var selected = NormalizePaymentKey(paymentMethod);
        if (selected.Length == 0) return false;
        var target = NormalizePaymentKey(SplitBalancePaymentMethod);
        return selected == target
               || (selected.Contains("UPONDESPATCH", StringComparison.Ordinal)
                   && selected.Contains("WITHIN10DAYSAFTERARRIVAL", StringComparison.Ordinal));
    }

    public static bool IsSplitBalancePayType(int payTypeId, string? description)
        => payTypeId == SplitBalancePayTypeId || IsSplitBalancePaymentMethod(description);

    /// <summary>付款方式为第 15 条时拆两行各 50%；不再按客户编号判断。</summary>
    public static bool ShouldSplitBalancePayment(string? paymentMethod)
        => IsSplitBalancePaymentMethod(paymentMethod);

    public static bool ShouldSplitBalancePayment(string? clientNo, string? paymentMethod)
        => IsSplitBalancePaymentMethod(paymentMethod);

    public static (double First, double Second) SplitBalanceAmounts(double total)
    {
        var first = Math.Round(total * 0.5, 2, MidpointRounding.AwayFromZero);
        var second = Math.Round(total - first, 2, MidpointRounding.AwayFromZero);
        return (first, second);
    }

    /// <summary>第 15 条付款方式落库时定金为 0，尾款按总金额。</summary>
    public static void ApplySplitPaymentDefaults(ZyqdCotractPiModel header)
    {
        if (!IsSplitBalancePaymentMethod(header.var3)) return;
        header.Payment_ratio = 0;
        header.Payment_ratio_Ext = 0;
        header.balance_before_delivery = Math.Round(header.TOTAL, 2);
    }

    private static string NormalizePaymentKey(string? text)
    {
        var s = (text ?? "").Trim().ToUpperInvariant();
        if (s.Length == 0) return "";
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
        }
        return sb.ToString();
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
    /// <summary>这一行物料来自哪张客户 PI 的发票号。</summary>
    public string? SourceInvoice { get; set; }
    /// <summary>这一行物料来自哪张客户 PI 的 DocumentGuid。</summary>
    public string? SourceGuid { get; set; }
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
                Bank6 = "NOTE: OUR FULL UNABBREVIATED NAME MUST APPEAR IN THE WIRE PAPERWORK."
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
                Bank6 = "SWIFT: BKCHCNBJ92D\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0TEL:0575-86041909"
            };
        }

        var currencyBlank = string.IsNullOrWhiteSpace(currencyUnit);
        var account = curr == "C$" ? "295046100018800004048"
            : currencyBlank || curr is "$" or "€" ? "295046100146300002686"
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
            Bank6 = "SWIFT: COMM CN SH SXG\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0TEL:0575-81166066"
        };
    }
}

public static class CotractPiFilterFields
{
    /// <summary>与合同列表表头对应。Key/Label 同为中文表头名。</summary>
    public static readonly (string Key, string Label)[] All =
    [
        ("客户编号", "客户编号"),
        ("客户名称", "客户名称"),
        ("订单类型", "订单类型"),
        ("需求人", "需求人"),
        ("发票号", "发票号"),
        ("PO号", "PO号"),
        ("创建日期", "创建日期"),
        ("修改人", "修改人"),
        ("修改时间", "修改时间")
    ];

    public static bool IsDateFilter(string? filterColumn)
        => filterColumn is "创建日期" or "修改时间";
}
