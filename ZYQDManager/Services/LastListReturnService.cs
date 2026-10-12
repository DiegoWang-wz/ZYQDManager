namespace ZYQDManager.Services;

/// <summary>
/// 仅保留「上一次离开的列表」UI 快照，供顶栏返回时恢复；菜单进入列表不恢复。
/// </summary>
public sealed class LastListReturnService
{
    private string? _listKey;
    private PagedListUiState? _state;
    private bool _pendingRestore;

    public void Capture(string listKey, PagedListUiState state)
    {
        var key = NormalizeKey(listKey);
        if (key.Length == 0 || state is null) return;
        _listKey = key;
        _state = Clone(state);
    }

    /// <summary>顶栏「返回」即将导航前回写标记。</summary>
    public void MarkReturning() => _pendingRestore = true;

    /// <summary>非返回导航时取消待恢复，避免菜单进列表误用旧快照。</summary>
    public void CancelPendingRestore() => _pendingRestore = false;

    /// <summary>若正从返回进入且 key 匹配，取出快照并清空；否则返回 null。</summary>
    public PagedListUiState? TakeIfMatch(string listKey)
    {
        if (!_pendingRestore) return null;
        var key = NormalizeKey(listKey);
        if (key.Length == 0 || !string.Equals(_listKey, key, StringComparison.OrdinalIgnoreCase))
            return null;

        _pendingRestore = false;
        var snap = _state;
        _state = null;
        _listKey = null;
        return snap;
    }

    private static string NormalizeKey(string? listKey)
    {
        var k = (listKey ?? "").Trim();
        if (k.Length == 0) return "";
        return k.StartsWith('/') ? k : "/" + k;
    }

    private static PagedListUiState Clone(PagedListUiState s) => new()
    {
        Page = s.Page,
        PageSize = s.PageSize,
        FilterColumn = s.FilterColumn,
        Keyword = s.Keyword,
        DateStart = s.DateStart,
        DateEnd = s.DateEnd,
        Extra = s.Extra,
        Flag1 = s.Flag1,
        Flag2 = s.Flag2,
    };
}
