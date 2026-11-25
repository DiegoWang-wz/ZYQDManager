using System.Net.Http.Headers;
using System.Text.Json;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 客户回传的签字版 PDF：和项目单据里的图片走同一条通道 ——
/// HTTP 上传到 3.240 老站的 Add_Cotract_Base_img，由老站自己写到磁盘。
///
/// 老站这个接口不限制扩展名，返回 /Img/yyyyMM/yyyyMMdd/{guid}.pdf；
/// 库里 SignFileId 就存这个站点根相对路径，
/// 页面用 LegacyFileUrl 还原成 http://192.168.3.240:3788/Img/... 供浏览器打开。
///
/// 之前用 FileStream 直接写 \\192.168.3.240\ZYQDManger\PDF（UNC），
/// 需要应用进程账号有共享写权限，图片能传而 PDF 不能传就是卡在这里；改 HTTP 后不再依赖 SMB。
/// </summary>
public class RemoteSignFileService
{
    private const string LegacyHttpClient = "LegacyFiles";
    private const string DefaultUploadPath = "/T_ZYQD_Cotract_Base/Add_Cotract_Base_img";
    private const long MaxBytes = 20 * 1024 * 1024;

    private readonly IHttpClientFactory _httpFactory;
    private readonly string _uploadUrl;

    public RemoteSignFileService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;

        var baseUrl = (config["LegacyFiles:BaseUrl"] ?? LegacyFileUrl.DefaultBase).Trim().TrimEnd('/');
        var path = (config["RemoteSignFiles:UploadPath"] ?? DefaultUploadPath).Trim();
        if (!path.StartsWith('/')) path = "/" + path;

        // ID 传空：老站只落文件、不回写合同表；file_text 对应它 Request.Files 的字段名
        _uploadUrl = baseUrl + path + "?ID=&file_text=file1";
    }

    public async Task<(string Url, string OriginalName)> SavePdfAsync(
        Stream content, string? originalName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var name = Path.GetFileName((originalName ?? "").Trim());
        if (name.Length == 0) name = "sign.pdf";

        if (!string.Equals(Path.GetExtension(name), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("签字版只接受 PDF 文件");

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        if (ms.Length == 0) throw new InvalidOperationException("PDF 文件内容为空");
        if (ms.Length > MaxBytes) throw new InvalidOperationException("签字版不能超过 20MB");

        var fileName = Guid.NewGuid().ToString("N") + ".pdf";

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(ms.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file1", fileName);

        var http = _httpFactory.CreateClient(LegacyHttpClient);
        using var resp = await http.PostAsync(_uploadUrl, form, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"签字版上传失败 HTTP {(int)resp.StatusCode}：{TrimBody(body)}");

        var path = ParseUploadPath(body);
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("老站签字版上传未返回路径：" + TrimBody(body));

        return (path.Trim(), name);
    }

    /// <summary>
    /// 老站没有开放删除接口，留空实现；换图后旧文件由服务器清理策略处理。
    /// 旧数据里的 /ZYQDManger/PDF/... 也不再尝试 UNC 删除 —— 这正是之前报权限错的地方。
    /// </summary>
    public void TryDelete(string? url)
    {
    }

    private static string? ParseUploadPath(string body)
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

