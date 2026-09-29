namespace ZYQDManager.DataDtos;

public class SendEmailDto
{
    /// <summary>收件人，至少一个。</summary>
    public List<string> To { get; set; } = new();

    public List<string>? Cc { get; set; }

    public List<string>? Bcc { get; set; }

    public string Subject { get; set; } = "";

    public string Body { get; set; } = "";

    public bool IsHtml { get; set; } = true;
}
