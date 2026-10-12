using ZYQDManager.Services;

namespace ZYQDManager.Utilities;

/// <summary>
/// 列表 UI 快照：写入「上一次」槽位，仅顶栏返回时按 key 取回。
/// </summary>
public static class ListUiStateStore
{
    public static class Keys
    {
        public const string Sir = "/sir";
        public const string Quotation = "/quotation";
        public const string QuotationBase = "/quotation-base";
        public const string CotractPi = "/contract";
        public const string CotractBase = "/contract-base";
        public const string CotractPacking = "/contract-pk";
        public const string Project = "/project-list";
    }

    public static PagedListUiState? Take(LastListReturnService store, string listKey)
        => store.TakeIfMatch(listKey);

    public static void Capture(LastListReturnService store, string listKey, PagedListUiState snap)
        => store.Capture(listKey, snap);
}
