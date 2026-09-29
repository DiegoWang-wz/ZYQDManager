namespace ZYQDManager.DataDtos;

public sealed class FileOperationOptions
{
    public const string SectionName = "FileOperation";
    public const string HttpClientName = "FileOperation";

    public string BaseUrl { get; set; } = "http://192.168.3.240:16751";
    public string Code { get; set; } = "JC0bbda95ce3c44c779a10d1a890c3c8a6";
    public string Salt { get; set; } = "330624";
    public string ShareRoot { get; set; } = @"\\192.168.3.240\品质登记";
    public string TemplateFileName { get; set; } = "遮阳基础数据导入模板.xlsx";
    public string UploadFolder { get; set; } = "upLoad";

    public string ResolvedBaseUrl()
    {
        var url = (BaseUrl ?? "").Trim().TrimEnd('/');
        return url.Length == 0 ? "http://192.168.3.240:16751" : url;
    }

    public string TemplateFullPath()
        => Path.Combine((ShareRoot ?? "").Trim(), TemplateFileName);

    public string UploadFullPath(string fileName)
        => Path.Combine((ShareRoot ?? "").Trim(), UploadFolder, fileName);
}
