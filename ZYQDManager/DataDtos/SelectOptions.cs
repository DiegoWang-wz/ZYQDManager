namespace ZYQDManager.DataDtos;

public sealed class SelectOptionItem
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";

    public string Display => string.IsNullOrWhiteSpace(Label) ? Value : Label;
}

public sealed class SelectOptions
{
    public const string SectionName = "SelectOptions";
    public const string FileName = "appsettings.SelectOptions.json";

    public string DefaultTradeTerm { get; set; } = "FOB";
    public string DefaultTemplate { get; set; } = "JC";
    public string ForceJckClientNo { get; set; } = "10008831";
    public List<string> OrderTypes { get; set; } = [];
    public Dictionary<string, string> OrderTypeTemplates { get; set; } = new(StringComparer.Ordinal);
    public List<string> TradeTerms { get; set; } = [];
    public List<SelectOptionItem> PaymentMethods { get; set; } = [];

    public IReadOnlyList<string> ResolvedOrderTypes()
        => OrderTypes.Count > 0 ? OrderTypes : Defaults.OrderTypes;

    public IReadOnlyList<string> ResolvedTradeTerms()
        => TradeTerms.Count > 0 ? TradeTerms : Defaults.TradeTerms;

    public IReadOnlyList<SelectOptionItem> ResolvedPaymentMethods()
    {
        var src = PaymentMethods.Count > 0 ? PaymentMethods : Defaults.PaymentMethods;
        var list = new List<SelectOptionItem>(src.Count);
        for (var i = 0; i < src.Count; i++)
        {
            var item = src[i];
            var raw = string.IsNullOrWhiteSpace(item.Label) ? item.Value : item.Label;
            var text = StripLeadingIndex(raw);
            var value = string.IsNullOrWhiteSpace(item.Value) ? text : item.Value.Trim();
            list.Add(new SelectOptionItem
            {
                Value = value,
                Label = $"{i + 1}.{text}"
            });
        }
        return list;
    }

    private static string StripLeadingIndex(string? text)
    {
        var s = (text ?? "").Trim();
        var i = 0;
        while (i < s.Length && char.IsDigit(s[i]))
            i++;
        if (i > 0 && i < s.Length && s[i] == '.')
            return s[(i + 1)..].TrimStart();
        return s;
    }

    public static SelectOptions Defaults { get; } = new()
    {
        DefaultTradeTerm = "FOB",
        DefaultTemplate = "JC",
        ForceJckClientNo = "10008831",
        OrderTypes =
        [
            "标准订单",
            "贸易订单（1300）",
            "贸易订单（1300-2001）",
            "寄售订单（1300-2001）"
        ],
        OrderTypeTemplates = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["标准订单"] = "JC",
            ["贸易订单（1300）"] = "JCK",
            ["贸易订单（1300-2001）"] = "JS",
            ["寄售订单（1300-2001）"] = "JS"
        },
        TradeTerms = ["EXW", "FOB", "C&F", "CIF", "DAP", "DDU", "DDP"]
    };
}
