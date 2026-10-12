namespace ZYQDManager.Services;

/// <summary>
/// 通用分页列表 UI 快照；仅由 LastListReturnService 在顶栏返回时恢复一次。
/// </summary>
public sealed class PagedListUiState
{
    public int Page { get; set; }
    public int PageSize { get; set; } = 20;
    public string? FilterColumn { get; set; }
    public string? Keyword { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    /// <summary>扩展字段：报告类型、单据/品类类型等。</summary>
    public string? Extra { get; set; }
    public bool? Flag1 { get; set; }
    public bool? Flag2 { get; set; }
}
