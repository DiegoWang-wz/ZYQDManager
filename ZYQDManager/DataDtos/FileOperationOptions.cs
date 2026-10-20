namespace ZYQDManager.DataDtos;

public sealed class FileOperationOptions
{
    public const string SectionName = "FileOperation";
    public const string HttpClientName = "FileOperation";

    public string BaseUrl { get; set; } = "http://192.168.3.246:16751";
    public string Code { get; set; } = "JC0bbda95ce3c44c779a10d1a890c3c8a6";
    public string Salt { get; set; } = "330624";
    public string ShareRoot { get; set; } = @"D:\Files";
    public string UploadFolder { get; set; } = "";

    public string ResolvedBaseUrl()
    {
        var url = (BaseUrl ?? "").Trim().TrimEnd('/');
        return url.Length == 0 ? "http://192.168.3.246:16751" : url;
    }

    public string UploadFullPath(string fileName)
        => string.IsNullOrWhiteSpace(UploadFolder)
            ? Path.Combine((ShareRoot ?? "").Trim(), fileName)
            : Path.Combine((ShareRoot ?? "").Trim(), UploadFolder.Trim(), fileName);
}
