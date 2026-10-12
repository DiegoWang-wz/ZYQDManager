using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ZYQDManager.Services;

/// <summary>
/// 记录应用内导航栈，供顶栏「返回上一页」使用（非硬编码列表 URL）。
/// </summary>
public sealed class NavigationHistoryService : IDisposable
{
    private readonly NavigationManager _nav;
    private readonly LastListReturnService _lastList;
    private readonly List<string> _stack = new();
    private bool _goingBack;
    private bool _started;

    public NavigationHistoryService(NavigationManager nav, LastListReturnService lastList)
    {
        _nav = nav;
        _lastList = lastList;
    }

    public bool CanGoBack => _stack.Count > 1;

    public event Action? Changed;

    public void EnsureStarted()
    {
        if (_started) return;
        _started = true;
        _nav.LocationChanged += OnLocationChanged;
        PushOrReplace(CurrentRelative());
    }

    public void GoBack()
    {
        EnsureStarted();
        if (!CanGoBack) return;

        _goingBack = true;
        _stack.RemoveAt(_stack.Count - 1);
        var target = _stack[^1];
        _lastList.MarkReturning();
        Changed?.Invoke();
        _nav.NavigateTo(ToAbsolute(target), forceLoad: false);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (_goingBack)
        {
            _goingBack = false;
            Changed?.Invoke();
            return;
        }

        // 菜单/链接导航：取消「返回恢复」，避免误用上一次列表快照
        _lastList.CancelPendingRestore();
        PushOrReplace(CurrentRelative());
        Changed?.Invoke();
    }

    private void PushOrReplace(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            uri = "/";

        // 登录页等不进栈；进入业务页时清掉登录残留
        if (IsPublic(uri))
        {
            _stack.Clear();
            return;
        }

        _stack.RemoveAll(IsPublic);

        if (_stack.Count == 0)
        {
            _stack.Add(uri);
            return;
        }

        var last = _stack[^1];
        if (string.Equals(last, uri, StringComparison.OrdinalIgnoreCase))
            return;

        // 同路径仅 query 变化（如单据类型切换 replace）时覆盖栈顶，避免污染返回栈
        if (string.Equals(PathOnly(last), PathOnly(uri), StringComparison.OrdinalIgnoreCase))
        {
            _stack[^1] = uri;
            return;
        }

        _stack.Add(uri);
        if (_stack.Count > 80)
            _stack.RemoveAt(0);
    }

    private static bool IsPublic(string uri)
    {
        var path = PathOnly(uri).Trim('/').ToLowerInvariant();
        return path is "" or "login" or "error";
    }

    private string CurrentRelative()
    {
        var rel = _nav.ToBaseRelativePath(_nav.Uri);
        if (string.IsNullOrWhiteSpace(rel))
            return "/";
        return rel.StartsWith('/') ? rel : "/" + rel;
    }

    private static string PathOnly(string uri)
    {
        var i = uri.IndexOf('?', StringComparison.Ordinal);
        return i < 0 ? uri : uri[..i];
    }

    private string ToAbsolute(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || relative == "/")
            return "/";
        return relative.StartsWith('/') ? relative : "/" + relative;
    }

    public void Dispose()
    {
        if (!_started) return;
        _nav.LocationChanged -= OnLocationChanged;
    }
}
