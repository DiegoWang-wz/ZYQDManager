using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 记录本系统管理员邮箱，并在有人新建/注册时发确认邮件。
/// </summary>
public class AdminEmailStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly ConcurrentDictionary<string, byte> _emails = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _filePath;
    private readonly ILogger<AdminEmailStore> _logger;

    public AdminEmailStore(IWebHostEnvironment env, ILogger<AdminEmailStore> logger)
    {
        _logger = logger;
        var dir = Path.Combine(env.ContentRootPath, "logs");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "admin-notify-emails.json");
        Load();
    }

    public IReadOnlyCollection<string> Snapshot() => _emails.Keys.ToArray();

    public void Replace(IEnumerable<string?> emails)
    {
        var next = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var email in emails)
        {
            var v = (email ?? "").Trim();
            if (IsEmail(v))
                next.Add(v);
        }

        var changed = false;
        foreach (var old in _emails.Keys.ToArray())
        {
            if (next.Contains(old)) continue;
            if (_emails.TryRemove(old, out _))
                changed = true;
        }

        foreach (var email in next)
        {
            if (_emails.TryAdd(email, 0))
                changed = true;
        }

        if (changed)
            Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var list = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_filePath));
            if (list is null) return;
            foreach (var email in list)
            {
                var v = (email ?? "").Trim();
                if (IsEmail(v))
                    _emails.TryAdd(v, 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取管理员通知邮箱缓存失败");
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_emails.Keys.OrderBy(x => x).ToList(), Json));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "写入管理员通知邮箱缓存失败");
        }
    }

    private static bool IsEmail(string value)
        => value.Length >= 5 && value.Contains('@') && value.Contains('.');
}

public class AdminNotifyService
{
    private readonly IEmailService _email;
    private readonly AdminEmailStore _store;
    private readonly AdminNotifyOptions _opt;
    private readonly ILogger<AdminNotifyService> _logger;

    public AdminNotifyService(
        IEmailService email,
        AdminEmailStore store,
        IOptions<AdminNotifyOptions> options,
        ILogger<AdminNotifyService> logger)
    {
        _email = email;
        _store = store;
        _opt = options.Value ?? new AdminNotifyOptions();
        _logger = logger;
    }

    public void RememberAdmins(IEnumerable<ManagedUserRowDto> rows)
    {
        _store.Replace(rows
            .Where(x => IsSystemAdmin(x.Role, x.EmpNo))
            .Select(x => x.Email));
    }

    public async Task NotifyNewUserAsync(NewUserNotifyDto info, CancellationToken ct = default)
    {
        var empNo = (info.EmpNo ?? "").Trim();
        if (string.IsNullOrEmpty(empNo))
            return;

        var name = (info.DisplayName ?? "").Trim();
        var who = string.IsNullOrEmpty(name) ? empNo : $"{empNo} {name}";
        var to = ResolveRecipients();
        if (to.Count == 0)
        {
            _logger.LogWarning("新用户 {EmpNo} 注册后无法通知管理员：没有可用邮箱", empNo);
            return;
        }

        var subject = MailTemplate.Subject(info.PendingReview
            ? $"新用户待审核：{who}"
            : $"新用户已登记：{who}");

        var result = await _email.SendAsync(new SendEmailDto
        {
            To = to,
            Subject = subject,
            Body = BuildBody(info, empNo, name),
            IsHtml = true
        }, ct);

        if (result.Code != 1)
            _logger.LogWarning("管理员通知邮件发送失败：{Message}", result.Message);
    }

    private static string BuildBody(NewUserNotifyDto info, string empNo, string name)
    {
        var url = (info.PersonnelUrl ?? "").Trim();
        var action = info.PendingReview
            ? "该账号当前为<strong>未启用</strong>状态，请核验身份后启用，并按需分配角色与客户。启用后申请人方可登录。"
            : "该账号已登记。请核验信息，并按需分配角色与客户。";
        var source = string.IsNullOrWhiteSpace(info.Source) ? "用户自助注册" : info.Source.Trim();
        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        return MailTemplate.Wrap(
            "遮阳管理系统收到一条新用户申请，请及时处理。",
            [
                ("工号", empNo),
                ("姓名", name),
                ("部门", info.Department),
                ("邮箱", info.Email),
                ("电话", info.Phone),
                ("来源", source),
                ("申请时间", time)
            ],
            afterHtml: $"<p>{action}</p>",
            buttonText: string.IsNullOrEmpty(url) ? null : "进入人员管理",
            buttonUrl: url);
    }

    private List<string> ResolveRecipients()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in _opt.Emails ?? [])
        {
            var v = (e ?? "").Trim();
            if (IsEmail(v)) set.Add(v);
        }

        foreach (var e in _store.Snapshot())
            set.Add(e);

        if (set.Count == 0)
        {
            var fallback = (_opt.FallbackEmail ?? "").Trim();
            if (IsEmail(fallback))
                set.Add(fallback);
        }

        return set.ToList();
    }

    private static bool IsSystemAdmin(string? role, string empNo)
        => string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)
           || string.Equals(empNo, "JcAdmin", StringComparison.OrdinalIgnoreCase);

    private static bool IsEmail(string value)
        => value.Length >= 5 && value.Contains('@') && value.Contains('.');
}
