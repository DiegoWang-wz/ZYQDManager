using System.Globalization;

namespace ZYQDManager.Utilities;

/// <summary>
/// 客户编号补全：业务上常省略前导，短号按 10000000+n 补成 8 位完整客户号。
/// 例：12696→10012696，10178→10010178；已是 ≥10000000 的完整号则原样返回。
/// </summary>
public static class ClientCodeHelper
{
    public const long BaseNo = 10_000_000L;
    public const long MaxFullNo = 99_999_999L;

    /// <summary>规范化客户号；空串返回空；无法识别的数字原样返回修剪后文本。</summary>
    public static string Expand(string? clientNo)
    {
        var t = (clientNo ?? "").Trim();
        if (t.Length == 0) return "";
        return TryExpand(t, out var code) ? code : t;
    }

    /// <summary>尝试将短号/完整号规范为客户编号。</summary>
    public static bool TryExpand(string? token, out string code)
    {
        code = "";
        var t = (token ?? "").Trim();
        if (t.Length == 0) return false;
        if (!long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n <= 0)
            return false;

        long full;
        if (n < BaseNo)
            full = BaseNo + n;
        else if (n <= MaxFullNo)
            full = n;
        else
            return false;

        code = full.ToString(CultureInfo.InvariantCulture);
        return code.Length <= 50;
    }
}
