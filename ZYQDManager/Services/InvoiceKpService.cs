using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sap.Data.Hana;
using ZYQDManager.DataDtos;

namespace ZYQDManager.Services;

public class InvoiceKpService
{
    private static readonly string[] SeriesPrefix5 =
    [
        "JCD15", "JCD20", "JCD24", "JCD25", "JCD28", "JCD30", "JCD35", "JCD45",
        "JCA35", "JCA45", "JCC50", "JCC60", "JCC70", "JCV24", "JCV30", "JCV40"
    ];

    private static readonly string[] SeriesPrefix6 =
    [
        "JC35W5", "JC35N6", "JC35W2", "JC35N8"
    ];

    private readonly InvoiceKpOptions _options;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<InvoiceKpService> _logger;

    public InvoiceKpService(
        IOptions<InvoiceKpOptions> options,
        IHttpClientFactory httpFactory,
        ILogger<InvoiceKpService> logger)
    {
        _options = options.Value;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<InvoiceKpQueryResult> QuerySummaryAsync(InvoiceKpQueryRequest req, CancellationToken ct = default)
    {
        if (!TryParseDateRange(req.DateStart, req.DateEnd, out var start, out var end))
            throw new ArgumentException("开票日期格式无效，请使用 yyyy-MM-dd");

        var connStr = ResolveHanaConnectionString();
        if (string.IsNullOrEmpty(connStr))
            throw new InvalidOperationException("未配置 InvoiceKp:HanaConnectionString");

        var dsSap = start.ToString("yyyyMMdd");
        var deSap = end.ToString("yyyyMMdd");
        var ds = start.ToString("yyyy-MM-dd");
        var de = end.ToString("yyyy-MM-dd");

        var details = await Task.Run(() => LoadInvoiceDetails(connStr, dsSap, deSap), ct);
        ApplyCustomerScope(details, req.EmpNo, req.IsAdmin);

        var keyword = (req.CustomerKeyword ?? "").Replace(" ", "", StringComparison.Ordinal);
        if (keyword.Length > 0)
        {
            details = details.Where(x =>
            {
                var kunnr = (x.Kunnr ?? "").Replace(" ", "", StringComparison.Ordinal);
                var name = (x.Name1 ?? "").Replace(" ", "", StringComparison.Ordinal);
                return kunnr.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                       || name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        var rates = await LoadUsdRatesAsync(ds, de, ct);
        foreach (var item in details)
        {
            item.FkdatYyyymm = ToYearMonth(item.Fkdat);
            item.ProductSeries = ResolveProductSeries(item.Arktx);
            item.Waerk = (item.Waerk ?? "").Trim();
            item.Name1 = (item.Name1 ?? "").Trim();
        }

        var rows = details
            .Select(d =>
            {
                var rate = rates.FirstOrDefault(r =>
                    string.Equals((r.Tcurr ?? "").Trim(), d.Waerk, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(r.ZgdatuYyyymm, d.FkdatYyyymm, StringComparison.Ordinal));
                var ukurs = rate?.Ukurs is > 0 ? rate.Ukurs : 1d;
                var amountUsd = string.Equals(d.Waerk, "USD", StringComparison.OrdinalIgnoreCase)
                    ? d.Amount
                    : d.Amount / ukurs;
                return new
                {
                    d.Name1,
                    d.FkdatYyyymm,
                    d.ProductSeries,
                    d.Fkimg,
                    AmountUsd = amountUsd
                };
            })
            .GroupBy(x => new { x.Name1, x.FkdatYyyymm, x.ProductSeries })
            .Select(g => new InvoiceKpSummaryRow
            {
                CustomerName = g.Key.Name1 ?? "",
                Month = g.Key.FkdatYyyymm ?? "",
                ProductSeries = g.Key.ProductSeries ?? "",
                Qty = g.Sum(x => x.Fkimg),
                AmountUsd = g.Sum(x => x.AmountUsd)
            })
            // 与旧系统一致：不按名称重排，保持汇总后的自然顺序，便于对照
            .ToList();

        return new InvoiceKpQueryResult
        {
            Rows = rows,
            TotalQty = rows.Sum(x => x.Qty),
            TotalAmountUsd = rows.Sum(x => x.AmountUsd)
        };
    }

    private string ResolveHanaConnectionString()
    {
        var native = (_options.HanaConnectionString ?? "").Trim();
        if (!string.IsNullOrEmpty(native))
            return native;

        // 兼容旧配置键（若仍写在 appsettings 里）
        return (_options.HanaOdbcConnectionString ?? "").Trim();
    }

    private void ApplyCustomerScope(List<InvoiceDetailRow> list, string empNo, bool isAdmin)
    {
        if (isAdmin) return;
        var id = (empNo ?? "").Trim();
        if (string.Equals(id, "JcAdmin", StringComparison.OrdinalIgnoreCase))
            return;
        if (!_options.CustomerScopeByEmpNo.TryGetValue(id, out var allowed) || allowed is null || allowed.Length == 0)
            return;

        var set = new HashSet<string>(
            allowed.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
            StringComparer.OrdinalIgnoreCase);
        list.RemoveAll(x => !set.Contains((x.Kunnr ?? "").Trim()));
    }

    private static List<InvoiceDetailRow> LoadInvoiceDetails(string connStr, string dsSap, string deSap)
    {
        // 与旧 BLL_ZYQD_KP 相同：遮阳销售办事处 VKBUR=0007、账户组 Z001，已过账未作废发票，扣减 ZWT2
        if (dsSap.Length != 8 || deSap.Length != 8
            || !dsSap.All(char.IsDigit) || !deSap.All(char.IsDigit))
            throw new ArgumentException("开票日期范围无效");

        var sql = $"""
            select
              LTRIM(a.KUNNR, 0) as "KUNNR",
              a.NAME1 as "NAME1",
              a.FKDAT as "FKDAT",
              LTRIM(a.VBELN, 0) as "VBELN",
              LTRIM(a.POSNR, 0) as "POSNR",
              LTRIM(a.MATNR, 0) as "MATNR",
              REPLACE(a.ARKTX, '"', '""') as "ARKTX",
              TO_DOUBLE(a.NETWR - IFNULL(b.KWERT, 0)) as "AMOUNT_USD",
              a.WAERK as "WAERK",
              TO_DOUBLE(IFNULL(a.FKIMG, 0)) as "FKIMG"
            from (
              SELECT uc.KUNNR, uc.NAME1, VBRK.FKDAT, VBRK.VBELN, VBRP.POSNR, VBRP.MATNR, VBRP.ARKTX,
                     VBRP.NETWR, VBRK.WAERK, VBRP.FKIMG
              FROM (
                SELECT DISTINCT KNA1.KUNNR, KNA1.NAME1, KNA1.NAME2
                FROM KNA1 INNER JOIN KNVV ON KNA1.KUNNR = KNVV.KUNNR
                WHERE KNVV.VKBUR = '0007' AND KNA1.KTOKD = 'Z001'
              ) uc
              LEFT JOIN VBRK ON uc.KUNNR = VBRK.KUNRG
              LEFT JOIN VBRP ON VBRK.VBELN = VBRP.VBELN
              WHERE VBRK.FKDAT >= '{dsSap}' AND VBRK.FKDAT <= '{deSap}'
                AND VBRK.RFBSK = 'C' AND VBRK.FKSTO <> 'X'
                AND (VBRK.SFAKN IS NULL OR VBRK.SFAKN = '')
            ) as a
            left join (
              SELECT VBRK.VBELN, VBRP.POSNR, SUM(PRCD_ELEMENTS.KWERT) as "KWERT"
              FROM (
                SELECT DISTINCT KNA1.KUNNR, KNA1.NAME1, KNA1.NAME2
                FROM KNA1 INNER JOIN KNVV ON KNA1.KUNNR = KNVV.KUNNR
                WHERE KNVV.VKBUR = '0007' AND KNA1.KTOKD = 'Z001'
              ) uc
              LEFT JOIN VBRK ON uc.KUNNR = VBRK.KUNRG
              LEFT JOIN VBRP ON VBRK.VBELN = VBRP.VBELN
              LEFT JOIN PRCD_ELEMENTS
                ON VBRK.KNUMV = PRCD_ELEMENTS.KNUMV AND PRCD_ELEMENTS.KPOSN = VBRP.POSNR
              WHERE VBRK.FKDAT >= '{dsSap}' AND VBRK.FKDAT <= '{deSap}'
                AND VBRK.RFBSK = 'C' AND VBRK.FKSTO <> 'X'
                AND (VBRK.SFAKN IS NULL OR VBRK.SFAKN = '')
                AND PRCD_ELEMENTS.KSCHL = 'ZWT2' AND PRCD_ELEMENTS.KINAK = ''
              GROUP BY VBRK.VBELN, VBRP.POSNR
            ) as b on a.VBELN = b.VBELN and a.POSNR = b.POSNR
            """;

        var list = new List<InvoiceDetailRow>();
        using var conn = new HanaConnection(connStr);
        conn.Open();
        using var cmd = new HanaCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var map = BuildColumnMap(reader);
        while (reader.Read())
        {
            list.Add(new InvoiceDetailRow
            {
                Kunnr = GetString(reader, map, "KUNNR"),
                Name1 = GetString(reader, map, "NAME1"),
                Fkdat = GetString(reader, map, "FKDAT"),
                Arktx = GetString(reader, map, "ARKTX"),
                Waerk = GetString(reader, map, "WAERK"),
                // 兼容旧别名 Amount_per_month_USD
                Amount = GetDouble(reader, map, "AMOUNT_USD", "Amount_per_month_USD", "NETWR"),
                Fkimg = GetDouble(reader, map, "FKIMG")
            });
        }

        return list;
    }

    private async Task<List<RateRow>> LoadUsdRatesAsync(string ds, string de, CancellationToken ct)
    {
        var result = new List<RateRow>();
        var baseUrl = (_options.RateApiBaseUrl ?? "").Trim().TrimEnd('/');
        var path = (_options.RateApiPath ?? "").Trim().TrimStart('/');
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(path))
            return result;

        try
        {
            var client = _httpFactory.CreateClient(InvoiceKpOptions.RateHttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, path);
            var user = _options.RateApiUser ?? "";
            var pwd = _options.RateApiPassword ?? "";
            if (!string.IsNullOrEmpty(user))
            {
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pwd}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
            }

            request.Content = new StringContent(
                $"{{\"ZBDAT\":\"{ds}\",\"ZEDAT\":\"{de}\"}}",
                Encoding.UTF8,
                "application/json");

            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("汇率接口失败 {Status}: {Body}", (int)response.StatusCode, body);
                return result;
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("T_RATE", out var ratesEl))
                return result;

            foreach (var el in ratesEl.EnumerateArray())
            {
                var kurst = el.TryGetProperty("KURST", out var k) ? k.GetString() ?? "" : "";
                var fcurr = el.TryGetProperty("FCURR", out var f) ? f.GetString() ?? "" : "";
                if (!string.Equals(kurst, "M", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(fcurr, "USD", StringComparison.OrdinalIgnoreCase))
                    continue;

                var tcurr = el.TryGetProperty("TCURR", out var t) ? t.GetString() ?? "" : "";
                var zgdatu = el.TryGetProperty("ZGDATU", out var z) ? z.GetString() ?? "" : "";
                var ukurs = 1d;
                if (el.TryGetProperty("UKURS", out var u))
                {
                    if (u.ValueKind == JsonValueKind.Number)
                        ukurs = u.GetDouble();
                    else if (u.ValueKind == JsonValueKind.String
                             && double.TryParse(u.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                        ukurs = parsed;
                }

                result.Add(new RateRow
                {
                    Tcurr = tcurr,
                    Ukurs = ukurs,
                    ZgdatuYyyymm = ToYearMonth(zgdatu)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载汇率失败，将按汇率 1 处理非 USD 金额");
        }

        return result;
    }

    private static string ResolveProductSeries(string? arktx)
    {
        var text = arktx ?? "";
        if (SeriesPrefix5.Any(p => text.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return text.Length >= 5 ? text[..5] : text;
        if (text.StartsWith("JCHR35W", StringComparison.OrdinalIgnoreCase))
            return "Remote";
        if (SeriesPrefix6.Any(p => text.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return text.Length >= 6 ? text[..6] : text;
        return "Accessories";
    }

    private static bool TryParseDateRange(string? dateStart, string? dateEnd, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;
        if (!TryParseDate(dateStart, out start) || !TryParseDate(dateEnd, out end))
            return false;
        if (end < start) (start, end) = (end, start);
        start = start.Date;
        end = end.Date;
        return true;
    }

    private static bool TryParseDate(string? value, out DateTime date)
    {
        date = default;
        var s = (value ?? "").Trim();
        if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;
        if (DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date))
            return true;
        return false;
    }

    private static string ToYearMonth(string? dateStr)
    {
        var s = (dateStr ?? "").Trim();
        if (s.Length == 0) return "";

        // HANA 可能直接给出 yyyyMMdd / yyyy-MM-dd / DateTime.ToString
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
            || DateTime.TryParse(s, CultureInfo.GetCultureInfo("zh-CN"), DateTimeStyles.None, out dt)
            || DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
            return dt.ToString("yyyyMM");

        var digits = new string(s.Where(char.IsDigit).ToArray());
        if (digits.Length >= 6 && (digits.StartsWith("19", StringComparison.Ordinal) || digits.StartsWith("20", StringComparison.Ordinal)))
            return digits[..6];

        var cleaned = s.Replace(".", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal)
            .Replace("/", "", StringComparison.Ordinal);
        if (cleaned.Length >= 6 && cleaned.Take(6).All(char.IsDigit))
            return cleaned[..6];
        return "";
    }

    private static Dictionary<string, int> BuildColumnMap(IDataRecord reader)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = (reader.GetName(i) ?? "").Trim();
            if (name.Length == 0) continue;
            map[name] = i;
        }

        return map;
    }

    private static bool TryGetOrdinal(Dictionary<string, int> map, out int ordinal, params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (map.TryGetValue(name.Trim(), out ordinal))
                return true;
        }

        ordinal = -1;
        return false;
    }

    private static string GetString(IDataRecord reader, Dictionary<string, int> map, params string[] names)
    {
        if (!TryGetOrdinal(map, out var ordinal, names))
            return "";
        if (reader.IsDBNull(ordinal))
            return "";
        // 与旧系统一致：先 ToString 再使用，避免 HANA Core 自定义数值类型转换异常
        return Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim() ?? "";
    }

    private static double GetDouble(IDataRecord reader, Dictionary<string, int> map, params string[] names)
    {
        if (!TryGetOrdinal(map, out var ordinal, names))
            return 0;
        if (reader.IsDBNull(ordinal))
            return 0;

        var value = reader.GetValue(ordinal);
        switch (value)
        {
            case null:
                return 0;
            case double d:
                return d;
            case float f:
                return f;
            case decimal m:
                return (double)m;
            case int i:
                return i;
            case long l:
                return l;
            case short s:
                return s;
            case byte b:
                return b;
        }

        // HANA Core 常见：先 GetDecimal / GetDouble，失败再按字符串解析（兼容旧 DataTable 路径）
        try
        {
            return Convert.ToDouble(reader.GetDecimal(ordinal), CultureInfo.InvariantCulture);
        }
        catch
        {
            // ignore
        }

        try
        {
            return reader.GetDouble(ordinal);
        }
        catch
        {
            // ignore
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
        if (text.Length == 0)
            text = Convert.ToString(value, CultureInfo.CurrentCulture)?.Trim() ?? "";
        if (text.Length == 0)
            return 0;

        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out parsed))
            return parsed;
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
            return parsed;
        return 0;
    }

    private sealed class InvoiceDetailRow
    {
        public string Kunnr { get; set; } = "";
        public string Name1 { get; set; } = "";
        public string Fkdat { get; set; } = "";
        public string Arktx { get; set; } = "";
        public string Waerk { get; set; } = "";
        public double Amount { get; set; }
        public double Fkimg { get; set; }
        public string FkdatYyyymm { get; set; } = "";
        public string ProductSeries { get; set; } = "";
    }

    private sealed class RateRow
    {
        public string Tcurr { get; set; } = "";
        public double Ukurs { get; set; }
        public string ZgdatuYyyymm { get; set; } = "";
    }
}
