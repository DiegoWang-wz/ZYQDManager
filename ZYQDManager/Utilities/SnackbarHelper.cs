using MudBlazor;

namespace ZYQDManager.Utilities;

/// <summary>
/// 每个用户 Circuit 独立的提示服务（Scoped）。
/// 注意：不要做成 static/Singleton，否则会在 Blazor Server 下串台。
/// </summary>
public sealed class SnackbarHelper
{
    private readonly ISnackbar _snackbar;

    public SnackbarHelper(ISnackbar snackbar)
    {
        _snackbar = snackbar;
    }

    /// <summary>
    /// 通用顶部提示
    /// </summary>
    public void Show(string message, Severity severity, int timeout = 2000)
    {
        _snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;
        _snackbar.Add(message, severity, config =>
        {
            config.ShowTransitionDuration = 300;
            config.HideTransitionDuration = 300;
            config.VisibleStateDuration = timeout;
            config.RequireInteraction = false;
            config.ShowCloseIcon = true;
            config.SnackbarVariant = Variant.Filled;
        });
    }

    /// <summary>
    /// 底部提示
    /// </summary>
    public void ShowBottom(string message, Severity severity, int timeout = 2000)
    {
        _snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomCenter;
        _snackbar.Add(message, severity, config =>
        {
            config.ShowTransitionDuration = 300;
            config.HideTransitionDuration = 300;
            config.VisibleStateDuration = timeout;
            config.RequireInteraction = false;
            config.ShowCloseIcon = true;
            config.SnackbarVariant = Variant.Filled;
        });
    }

    public void Clear() => _snackbar.Clear();
}