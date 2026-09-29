using System.Net;

namespace ZYQDManager.Utilities;

/// <summary>
/// 系统邮件统一版式：问候语、说明、信息表、可选按钮、自动发送声明。
/// </summary>
public static class MailTemplate
{
    public const string AppName = "遮阳管理系统";

    public static string Subject(string title)
        => $"【{AppName}】{title}";

    public static string Greeting(string? displayName)
    {
        var name = Html(displayName);
        return string.IsNullOrEmpty(name) ? "您好：" : $"您好，{name}：";
    }

    public static string Wrap(
        string intro,
        IEnumerable<(string Label, string? Value)>? rows = null,
        string? afterHtml = null,
        string? buttonText = null,
        string? buttonUrl = null,
        string? greetingHtml = null)
    {
        var greeting = string.IsNullOrWhiteSpace(greetingHtml) ? "您好：" : greetingHtml.Trim();
        var table = BuildTable(rows);
        var extra = string.IsNullOrWhiteSpace(afterHtml) ? "" : afterHtml;
        var link = BuildButton(buttonText, buttonUrl);

        return $"""
            <div style="font-family:'Microsoft YaHei',Segoe UI,Arial,sans-serif;font-size:14px;color:#333;line-height:1.7;">
              <p>{greeting}</p>
              <p>{Html(intro)}</p>
              {table}
              {extra}
              {link}
              <p style="margin-top:24px;color:#888;font-size:12px;">本邮件由{AppName}自动发送，请勿直接回复。</p>
            </div>
            """;
    }

    public static string RegisterCode(string? displayName, string empNo, string? email, string code)
        => Wrap(
            "您正在注册遮阳管理系统，请使用以下验证码完成邮箱验证。",
            [
                ("工号", empNo),
                ("姓名", displayName),
                ("邮箱", email),
                ("有效期", "10 分钟")
            ],
            afterHtml: CodeBox(code) + "<p>如非本人操作，请忽略本邮件。</p>",
            greetingHtml: Greeting(displayName));

    public static string ResetCode(string empNo, string? email, string code)
        => Wrap(
            "您正在找回遮阳管理系统登录密码，请使用以下验证码完成验证。",
            [
                ("工号", empNo),
                ("邮箱", email),
                ("有效期", "10 分钟")
            ],
            afterHtml: CodeBox(code) + "<p>如非本人操作，请忽略本邮件。</p>");

    public static string RegisterSubmitted(
        string? displayName, string empNo, string? department, string? email, string? phone)
        => Wrap(
            "您的账号已提交遮阳管理系统注册，当前待管理员审核启用后即可登录。",
            UserRows(empNo, displayName, department, email, phone, DateTime.Now),
            afterHtml: "<p>账号当前为<strong>未启用</strong>状态。请耐心等待管理员核验，启用后即可使用工号和密码登录。</p>",
            greetingHtml: Greeting(displayName));

    public static string AccountOpened(
        string? displayName,
        string empNo,
        string? department,
        string? email,
        string? phone,
        string? loginUrl)
        => Wrap(
            "您的账号已开通遮阳管理系统。",
            UserRows(empNo, displayName, department, email, phone, DateTime.Now),
            afterHtml: "<p>请使用统一账号工号和密码登录。</p>",
            buttonText: string.IsNullOrWhiteSpace(loginUrl) ? null : "前往登录",
            buttonUrl: loginUrl,
            greetingHtml: Greeting(displayName));

    public static string AccountEnabled(
        string? displayName,
        string empNo,
        string? department,
        string? email,
        string? phone,
        string? loginUrl)
        => Wrap(
            "您的遮阳管理系统账号已由管理员启用，现在可以使用工号和密码登录。",
            [
                ..UserRows(empNo, displayName, department, email, phone, DateTime.Now),
                ("状态", "已启用")
            ],
            afterHtml: "<p>请使用统一账号工号和密码登录。</p>",
            buttonText: string.IsNullOrWhiteSpace(loginUrl) ? null : "前往登录",
            buttonUrl: loginUrl,
            greetingHtml: Greeting(displayName));

    public static string AccountDisabled(
        string? displayName,
        string empNo,
        string? department,
        string? email,
        string? phone)
        => Wrap(
            "您的遮阳管理系统账号已被管理员禁用，暂时无法登录。",
            [
                ..UserRows(empNo, displayName, department, email, phone, DateTime.Now),
                ("状态", "已禁用")
            ],
            afterHtml: "<p>如需恢复访问，请联系系统管理员。</p>",
            greetingHtml: Greeting(displayName));

    public static string Html(string? value)
        => WebUtility.HtmlEncode((value ?? "").Trim());

    private static IEnumerable<(string Label, string? Value)> UserRows(
        string empNo, string? displayName, string? department, string? email, string? phone, DateTime time)
        =>
        [
            ("工号", empNo),
            ("姓名", displayName),
            ("部门", department),
            ("邮箱", email),
            ("电话", phone),
            ("时间", time.ToString("yyyy-MM-dd HH:mm"))
        ];

    private static string CodeBox(string code)
        => $"""
            <p style="margin:16px 0 8px 0;">验证码：</p>
            <p style="margin:0 0 12px 0;">
              <span style="display:inline-block;padding:8px 16px;background:#1976d2;color:#fff;font-size:22px;letter-spacing:6px;font-weight:700;border-radius:4px;">{Html(code)}</span>
            </p>
            """;

    private static string BuildTable(IEnumerable<(string Label, string? Value)>? rows)
    {
        if (rows is null) return "";
        var list = rows.ToList();
        if (list.Count == 0) return "";
        return "<table style=\"border-collapse:collapse;width:100%;max-width:560px;margin:12px 0;\">"
               + string.Concat(list.Select(x => Row(x.Label, x.Value)))
               + "</table>";
    }

    private static string Row(string label, string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        return $"""
            <tr>
              <td style="width:96px;padding:6px 12px;border:1px solid #e5e5e5;background:#f7f7f7;color:#666;">{Html(label)}</td>
              <td style="padding:6px 12px;border:1px solid #e5e5e5;">{Html(text)}</td>
            </tr>
            """;
    }

    private static string BuildButton(string? text, string? url)
    {
        var href = (url ?? "").Trim();
        if (string.IsNullOrEmpty(href) || string.IsNullOrWhiteSpace(text))
            return "";
        return $"""
            <p style="margin:20px 0 8px 0;">
              <a href="{Html(href)}" style="display:inline-block;padding:8px 16px;background:#1976d2;color:#fff;text-decoration:none;border-radius:4px;">{Html(text)}</a>
            </p>
            <p style="margin:0;color:#888;font-size:12px;">如按钮无法打开，请复制以下地址到浏览器：<br/>{Html(href)}</p>
            """;
    }
}
