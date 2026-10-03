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

    /// <summary>
    /// 上传加密文件到共享盘 upLoad，调用老解密服务后读回字节并清理临时文件。
    /// </summary>
    public async Task<(byte[] Bytes, string ApiMessage)> DecryptUploadAsync(
        Stream content,
        string originalFileName,
        CancellationToken ct = default)
    {
        if (content is null)
            throw new InvalidOperationException("文件为空");

        var safeName = SanitizeFileName(originalFileName);
        var shareName = "decode-" + Guid.NewGuid().ToString("N") + "-" + safeName;
        var sharePath = _opt.UploadFullPath(shareName);
        var dir = Path.GetDirectoryName(sharePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using (var fs = new FileStream(sharePath, FileMode.Create, FileAccess.Write, FileShare.None))
            await content.CopyToAsync(fs, ct);

        if (!File.Exists(sharePath) || new FileInfo(sharePath).Length == 0)
            throw new InvalidOperationException("上传到共享盘失败或文件为空");

        try
        {
            var before = await File.ReadAllBytesAsync(sharePath, ct);
            var msg = await DecodeFileAsync(sharePath, ct);
            var after = await File.ReadAllBytesAsync(sharePath, ct);
            if (after.Length == 0)
                throw new InvalidOperationException("解密后文件为空。服务返回：" + (msg ?? ""));

            // 内容完全没变时给出提示，仍允许下载（可能本就未加密）
            if (before.Length == after.Length && before.AsSpan().SequenceEqual(after))
                msg = (msg ?? "").Trim() + "（文件内容未变化，可能未加密或解密未生效）";

            return (after, msg ?? "");
        }
        finally
        {
            try { File.Delete(sharePath); } catch { /* ignore */ }
            try { await DeleteFileAsync(sharePath, ct); } catch { /* ignore */ }
        }
    }

    private static string SanitizeFileName(string? name)
    {
        var n = Path.GetFileName(name ?? "").Trim();
        if (n.Length == 0)
            n = "file.bin";
        foreach (var c in Path.GetInvalidFileNameChars())
            n = n.Replace(c, '_');
        if (n.Length > 80)
        {
            var ext = Path.GetExtension(n);
            var stem = Path.GetFileNameWithoutExtension(n);
            n = stem[..Math.Min(60, stem.Length)] + ext;
        }
        return n;
    }

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
