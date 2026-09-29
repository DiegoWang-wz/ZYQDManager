using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using ZYQDManager.DataDtos;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

public interface IEmailService
{
    Task<ApiResponse<bool>> SendAsync(SendEmailDto dto, CancellationToken ct = default);
}

public class EmailService : IEmailService
{
    private const string Host = "smtphz.qiye.163.com";
    private const int Port = 465;
    private const string UserName = "Sunlight_Management_BU@jiecang.com";
    private const string Password = "a3fYTGv7QbJq@8Td";
    private const string FromAddress = "Sunlight_Management_BU@jiecang.com";
    private const string FromName = "遮阳表单系统管理员";

    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> SendAsync(SendEmailDto dto, CancellationToken ct = default)
    {
        if (dto == null)
            return ApiResponse<bool>.BadRequest("参数不能为空");

        var toList = NormalizeAddresses(dto.To);
        if (toList.Count == 0)
            return ApiResponse<bool>.BadRequest("收件人不能为空");

        if (string.IsNullOrWhiteSpace(dto.Subject))
            return ApiResponse<bool>.BadRequest("邮件主题不能为空");

        var ccList = NormalizeAddresses(dto.Cc);
        var bccList = NormalizeAddresses(dto.Bcc);

        try
        {
            var mime = BuildMime(toList, ccList, bccList, dto.Subject.Trim(), dto.Body ?? "", dto.IsHtml);
            await SendSmtpAsync(toList.Concat(ccList).Concat(bccList).Distinct(StringComparer.OrdinalIgnoreCase), mime, ct);
            _logger.LogInformation("邮件已发送 Subject={Subject} To={To}", dto.Subject.Trim(), string.Join(",", toList));
            return ApiResponse<bool>.Ok(true, "发送成功");
        }
        catch (Exception ex)
        {
            var root = ex.Root();
            _logger.LogError(ex, "SendAsync failed, subject={Subject}, root={Root}", dto.Subject, root.Message);
            return ApiResponse<bool>.Fail($"发送失败：{root.Message}", 0);
        }
    }

    private static async Task SendSmtpAsync(IEnumerable<string> recipients, string mime, CancellationToken ct)
    {
        using var tcp = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        var token = timeoutCts.Token;

        await tcp.ConnectAsync(Host, Port, token);
        await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = Host
        }, token);

        await ExpectAsync(ssl, 220, token);
        await SendCommandAsync(ssl, $"EHLO {Host}", 250, token);
        await SendCommandAsync(ssl, "AUTH LOGIN", 334, token);
        await SendCommandAsync(ssl, ToBase64(UserName), 334, token);
        await SendCommandAsync(ssl, ToBase64(Password), 235, token);
        await SendCommandAsync(ssl, $"MAIL FROM:<{FromAddress}>", 250, token);
        foreach (var addr in recipients)
            await SendCommandAsync(ssl, $"RCPT TO:<{addr}>", 250, token);
        await SendCommandAsync(ssl, "DATA", 354, token);
        await WriteLineAsync(ssl, mime.Replace("\n.", "\n..") + "\r\n.", token);
        await ExpectAsync(ssl, 250, token);
        await SendCommandAsync(ssl, "QUIT", 221, token);
    }

    private static string BuildMime(List<string> to, List<string> cc, List<string> bcc, string subject, string body, bool isHtml)
    {
        var sb = new StringBuilder();
        sb.Append("From: ").Append(EncodeHeader(FromName)).Append(" <").Append(FromAddress).Append(">\r\n");
        sb.Append("To: ").Append(string.Join(", ", to)).Append("\r\n");
        if (cc.Count > 0)
            sb.Append("Cc: ").Append(string.Join(", ", cc)).Append("\r\n");
        if (bcc.Count > 0)
            sb.Append("Bcc: ").Append(string.Join(", ", bcc)).Append("\r\n");
        sb.Append("Subject: ").Append(EncodeHeader(subject)).Append("\r\n");
        sb.Append("MIME-Version: 1.0\r\n");
        sb.Append("Content-Type: ").Append(isHtml ? "text/html" : "text/plain").Append("; charset=utf-8\r\n");
        sb.Append("Content-Transfer-Encoding: base64\r\n\r\n");
        sb.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(body)));
        return sb.ToString();
    }

    private static async Task SendCommandAsync(SslStream ssl, string command, int expected, CancellationToken ct)
    {
        await WriteLineAsync(ssl, command, ct);
        await ExpectAsync(ssl, expected, ct);
    }

    private static async Task WriteLineAsync(SslStream ssl, string text, CancellationToken ct)
    {
        var bytes = Encoding.ASCII.GetBytes(text + "\r\n");
        await ssl.WriteAsync(bytes, ct);
        await ssl.FlushAsync(ct);
    }

    private static async Task ExpectAsync(SslStream ssl, int expected, CancellationToken ct)
    {
        var reply = await ReadReplyAsync(ssl, ct);
        if (reply.Code != expected)
            throw new InvalidOperationException($"SMTP {expected} 失败：{reply.Code} {reply.Text}");
    }

    private static async Task<(int Code, string Text)> ReadReplyAsync(SslStream ssl, CancellationToken ct)
    {
        var buffer = new byte[4096];
        var sb = new StringBuilder();
        while (true)
        {
            var n = await ssl.ReadAsync(buffer, ct);
            if (n <= 0) break;
            sb.Append(Encoding.ASCII.GetString(buffer, 0, n));
            var text = sb.ToString();
            var lines = text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) continue;
            var last = lines[^1];
            if (last.Length >= 4 && char.IsDigit(last[0]) && last[3] == ' ')
            {
                var code = int.Parse(last[..3]);
                return (code, text.Trim());
            }
        }

        throw new InvalidOperationException("SMTP 无响应");
    }

    private static string EncodeHeader(string text)
        => "=?utf-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(text)) + "?=";

    private static string ToBase64(string text)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    private static List<string> NormalizeAddresses(IEnumerable<string>? addresses)
    {
        if (addresses == null) return [];
        return addresses
            .Select(x => (x ?? "").Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
