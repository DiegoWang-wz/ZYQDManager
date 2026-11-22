using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;

namespace ZYQDManager.Services;

/// <summary>
/// 项目单据里的示意图/图片上传，与其他功能块一致：文件通过 HTTP 传到 3.240 老站，
/// 库里（ExtraJson）只存老站返回的 /Img/... 站点根相对路径，预览用 LegacyFileUrl 还原成完整 URL。
/// </summary>
public class MotorImageService
{
    private const string LegacyHttpClient = "LegacyFiles";
    private const string LegacyImgUploadPath = "/T_ZYQD_Cotract_Base/Add_Cotract_Base_img";
    private const long MaxBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly string _imageBase;

    public MotorImageService(IHttpClientFactory httpFactory, IOptions<LegacyFilesOptions> legacyFiles)
    {
        _httpFactory = httpFactory;
        _imageBase = legacyFiles.Value.ResolvedBaseUrl();
    }

    /// <summary>把图片发到 3.240 老站，返回 /Img/... 相对路径，直接写进 ExtraJson。</summary>
    public async Task<string> SaveAsync(
        Stream content, string? originalName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var ext = Path.GetExtension((originalName ?? "").Trim());
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("图片只接受 jpg / jpeg / png / gif / webp / bmp 格式");

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        if (ms.Length == 0) throw new InvalidOperationException("图片文件内容为空");
        if (ms.Length > MaxBytes) throw new InvalidOperationException("图片不能超过 10MB");

        var fileName = Guid.NewGuid().ToString("N") + ext;
        var uploadUrl = _imageBase + LegacyImgUploadPath + "?ID=&file_text=file1";
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(ms.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeFromExt(ext));
        form.Add(fileContent, "file1", fileName);

        var http = _httpFactory.CreateClient(LegacyHttpClient);
        using var resp = await http.PostAsync(uploadUrl, form, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"老站图片上传失败 HTTP {(int)resp.StatusCode}：{TrimBody(body)}");

        var path = ParseLegacyUploadPath(body);
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("老站图片上传未返回路径：" + TrimBody(body));

        return path.Trim();
    }

    /// <summary>老站没有开放图片删除接口，留空实现；换图后旧文件由服务器清理策略处理。</summary>
    public void TryDelete(string? url)
    {
    }

    private static string MediaTypeFromExt(string ext) => ext.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".webp" => "image/webp",
        _ => "image/png"
    };

    private static string? ParseLegacyUploadPath(string body)
    {
        var json = (body ?? "").Trim();
        if (json.Length == 0) return null;
        if (json.StartsWith('"') && json.EndsWith('"'))
        {
            try { json = JsonSerializer.Deserialize<string>(json) ?? json; }
            catch { /* 不是二次编码 */ }
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name.Equals("filePath", StringComparison.OrdinalIgnoreCase))
                    return prop.Value.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string TrimBody(string body)
    {
        var t = (body ?? "").Trim().Replace('\r', ' ').Replace('\n', ' ');
        return t.Length <= 200 ? t : t[..200];
    }
}
