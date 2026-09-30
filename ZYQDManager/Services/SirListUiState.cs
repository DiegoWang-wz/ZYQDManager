namespace ZYQDManager.Services;

/// <summary>
/// 检测报告列表页 UI 快照（写入 localStorage，返回列表时恢复分页与筛选）
/// </summary>
public sealed class SirListUiState
{
    public int Page { get; set; }
    public int PageSize { get; set; } = 20;
    public string FilterColumn { get; set; } = "客户";
    public string? Keyword { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
}
