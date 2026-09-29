namespace ZYQDManager.DataDtos;

public sealed class AdminNotifyOptions
{
    public const string SectionName = "AdminNotify";

    /// <summary>额外固定收件人；会与人员管理里角色为管理员且填了邮箱的账号合并。</summary>
    public string[] Emails { get; set; } = [];

    /// <summary>找不到管理员邮箱时的兜底收件人。</summary>
    public string FallbackEmail { get; set; } = "Sunlight_Management_BU@jiecang.com";
}

public sealed class NewUserNotifyDto
{
    public string EmpNo { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string PersonnelUrl { get; set; } = "";
    /// <summary>用户自助注册 / 管理员新建</summary>
    public string Source { get; set; } = "用户自助注册";
    public bool PendingReview { get; set; } = true;
}
