using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ZYQDManager.Services;

/// <summary>在新浏览器标签页打开表单/明细；顶栏菜单等列表切换仍用 NavigationManager。</summary>
public sealed class TabNavigationService(IJSRuntime js, NavigationManager nav)
{
    public async ValueTask OpenAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        var href = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                   || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url.Trim()
            : nav.ToAbsoluteUri(url.Trim()).ToString();

        // 直接调浏览器自带的 window.open，不依赖 site.js 是否已发布 zyqdOpenTab
        try
        {
            await js.InvokeVoidAsync("open", href, "_blank");
        }
        catch (JSException)
        {
            nav.NavigateTo(href);
        }
    }
}
