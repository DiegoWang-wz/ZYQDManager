using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 老系统文件加解密：http://192.168.3.240:16751/FileOperation
/// EncodeFile / decodeFile / DeleteFile，参数与 EM_SunshadeDataSource.BLL.FileDecode 一致。
/// </summary>
public class FileOperationService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly FileOperationOptions _opt;

    public FileOperationService(IHttpClientFactory httpFactory, IOptions<FileOperationOptions> options)
    {
        _httpFactory = httpFactory;
        _opt = options.Value;
    }

    public FileOperationOptions Options => _opt;

    public Task<string> EncodeFileAsync(string filePath, CancellationToken ct = default)
        => PostAsync("/FileOperation/EncodeFile", filePath, ct);

    public Task<string> DecodeFileAsync(string filePath, CancellationToken ct = default)
        => PostAsync("/FileOperation/decodeFile", filePath, ct);

    public Task<string> DeleteFileAsync(string filePath, CancellationToken ct = default)
        => PostAsync("/FileOperation/DeleteFile", filePath, ct);

    public async Task<byte[]> ReadDecryptedPdfAsync(string pdfPath, ILogger? log = null, CancellationToken ct = default)
    {
        var bytes = await File.ReadAllBytesAsync(pdfPath, ct);
        if (LooksLikePdf(bytes))
            return bytes;

        var localMsg = await DecodeFileAsync(pdfPath, ct);
        log?.LogInformation("本地 PDF 解密 {Path} => {Msg}", pdfPath, localMsg);
        bytes = await File.ReadAllBytesAsync(pdfPath, ct);
        if (LooksLikePdf(bytes))
            return bytes;

        var sharePath = _opt.UploadFullPath("pi-" + Guid.NewGuid().ToString("N") + ".pdf");
        var dir = Path.GetDirectoryName(sharePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.Copy(pdfPath, sharePath, true);
        try
        {
            var shareMsg = await DecodeFileAsync(sharePath, ct);
            log?.LogInformation("共享盘 PDF 解密 {Path} => {Msg}", sharePath, shareMsg);
            bytes = await File.ReadAllBytesAsync(sharePath, ct);
            if (!LooksLikePdf(bytes))
                throw new InvalidOperationException("PDF 解密后仍无法打开（可能仍被公司加密）。解密返回：" + (shareMsg ?? localMsg));
            return bytes;
        }
        finally
        {
            try { File.Delete(sharePath); } catch { /* ignore */ }
        }
    }

    public static bool LooksLikePdf(byte[] bytes)
        => bytes.Length >= 5
           && bytes[0] == (byte)'%'
           && bytes[1] == (byte)'P'
           && bytes[2] == (byte)'D'
           && bytes[3] == (byte)'F';

    private async Task<string> PostAsync(string relativePath, string filePath, CancellationToken ct)
    {
        try
        {
            var salt = Md5Legacy.FileOperationSalt(filePath, _opt.Salt);
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(_opt.Code), "Code");
            content.Add(new StringContent(salt), "saltCode");
            content.Add(new StringContent(filePath), "fileSrc");

            var http = _httpFactory.CreateClient(FileOperationOptions.HttpClientName);
            using var resp = await http.PostAsync(relativePath, content, ct);
            return await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
