using Microsoft.AspNetCore.Components;

namespace ZYQDManager.Utilities;

/// <summary>
/// 邮件等对外链接使用的站点根地址（部署后固定为公网/内网访问入口）。
/// </summary>
public static class AppPublicUrl
{
    public const string ConfigKey = "App:PublicBaseUrl";

    /// <summary>
    /// 生成绝对 URL。优先用配置 <c>App:PublicBaseUrl</c>；未配置时回退 NavigationManager。
    /// </summary>
    public static string Absolute(IConfiguration config, NavigationManager nav, string relativePath)
    {
        var path = (relativePath ?? "").Trim().TrimStart('/');
        var configured = (config[ConfigKey] ?? "").Trim().TrimEnd('/');
        if (configured.Length > 0)
            return path.Length == 0 ? configured : $"{configured}/{path}";

        return nav.ToAbsoluteUri(path).AbsoluteUri;
    }
}
