namespace ZYQDManager.DataDtos;

public sealed class LegacyFilesOptions
{
    public const string SectionName = "LegacyFiles";

    /// <summary>老站图片 HTTP 根。预览、导出、导入落盘都走这里，与 EquipmentManager 同一套 /Img/。</summary>
    public string BaseUrl { get; set; } = Utilities.LegacyFileUrl.DefaultBase;

    public string ResolvedBaseUrl()
    {
        var url = (BaseUrl ?? "").Trim().TrimEnd('/');
        return url.Length == 0 ? Utilities.LegacyFileUrl.DefaultBase : url;
    }
}
