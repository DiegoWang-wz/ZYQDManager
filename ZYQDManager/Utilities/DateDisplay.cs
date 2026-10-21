namespace ZYQDManager.Utilities;

/// <summary>
/// 界面日期展示：有时分秒则显示完整时间，仅日期（00:00:00）则只显示年月日。
/// 业务存盘字段（如 BILL_TO_DATE）仍用各自 FormatDate，不要改成这个。
/// </summary>
public static class DateDisplay
{
    public static string Format(DateTime? value, string empty = "—")
    {
        if (value is null || value.Value == default)
            return empty;

        var dt = value.Value;
        if (dt.TimeOfDay == TimeSpan.Zero)
            return dt.ToString("yyyy-MM-dd");
        return dt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static string Format(DateTime value, string empty = "—")
        => Format((DateTime?)value, empty);

    public static string Format(string? value, string empty = "—")
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0)
            return empty;

        s = s.Replace('/', '-').Replace('.', '-');
        if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d)
            || DateTime.TryParse(s, out d))
            return Format(d, empty);

        return s;
    }
}
