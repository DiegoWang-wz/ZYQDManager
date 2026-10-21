using MudBlazor;

namespace ZYQDManager.Utilities;

/// <summary>编辑页导出前：有未保存修改时询问是否先保存。</summary>
public static class UnsavedExportGuard
{
    /// <param name="exportFromDatabase">true=导出走库（如 PI/装箱单）；false=导出走当前页面内存（如报价/检测）。</param>
    /// <returns>true=继续导出；false=取消。SaveFirst=用户选了「保存并导出」。</returns>
    public static async Task<(bool Continue, bool SaveFirst)> AskAsync(
        IDialogService dialog,
        bool exportFromDatabase)
    {
        var msg = exportFromDatabase
            ? "当前有未保存的修改，导出将使用已保存的数据。是否先保存？"
            : "当前有未保存的修改。是否先保存再导出？";

        var choice = await dialog.ShowMessageBoxAsync(
            "有未保存的修改",
            msg,
            yesText: "保存并导出",
            noText: "直接导出",
            cancelText: "取消");

        if (choice is null)
            return (false, false);
        if (choice == true)
            return (true, true);
        return (true, false);
    }
}
