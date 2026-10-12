using Microsoft.Extensions.Localization;
using MudBlazor;

namespace ZYQDManager.Utilities;

/// <summary>MudBlazor 控件文案中文化（分页等）。</summary>
public sealed class ChineseMudLocalizer : MudLocalizer
{
    private static readonly Dictionary<string, string> Zh = new(StringComparer.Ordinal)
    {
        ["Rows per page:"] = "每页行数：",
        ["{first_item}-{last_item} of {all_items}"] = "显示第 {first_item} 到第 {last_item} 条，共 {all_items} 条",
    };

    public override LocalizedString this[string key]
    {
        get
        {
            if (Zh.TryGetValue(key, out var value))
                return new LocalizedString(key, value);
            return new LocalizedString(key, key, resourceNotFound: true);
        }
    }
}
