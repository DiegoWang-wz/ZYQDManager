using System.Text.Json;

namespace ZYQDManager.DataDtos;

public sealed class CentralApiOptions
{
    public const string SectionName = "CentralApi";
    public const string AppSystemKey = "ZYQDManager";
    public const string HttpClientName = "CentralApi";

    /// <summary>开票汇总页路由（与 JcCommonApi Controls 键一致）</summary>
    public const string InvoiceKpRoute = "/invoice-kp";

    public string BaseUrl { get; set; } = "";
    public bool IgnoreSslCertificateErrors { get; set; } = true;
    public string ApiResourceRoot { get; set; } = "api/zh";

    public string Relative(string pathAfterRoot)
    {
        var root = (ApiResourceRoot ?? "api/zh").Trim('/').Trim();
        var sub = (pathAfterRoot ?? "").TrimStart('/').Trim();
        return string.IsNullOrEmpty(sub) ? root : $"{root}/{sub}";
    }

    public static string NormalizeControlKey(string? route)
    {
        var s = (route ?? "").Trim();
        if (s.Length == 0) return "";
        return s.Trim('/').ToLowerInvariant();
    }
}

public sealed class JcCommonApiEnvelope<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }
}

public sealed class LoginRequestDto
{
    public string EmpNo { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class LoginResponseDataDto
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public string EmpNo { get; set; } = "";
}

public sealed class StoredCentralAuth
{
    public string Token { get; set; } = "";
    public string EmpNo { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public string Role { get; set; } = "";

    /// <summary>页面路由 → 权限（键已规范化为小写去斜杠，如 invoice-kp）</summary>
    public Dictionary<string, ControlPermissionApiDto> Controls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ControlPermissionApiDto
{
    public bool Visible { get; set; }
    public bool Disabled { get; set; }
}

public sealed class CentralAdminSelfApiDto
{
    public bool CanManageUsers { get; set; }
    public bool IsSuperAdmin { get; set; }
    public string[] ManagedSystems { get; set; } = [];
}

public sealed class SystemPermissionsForSystemApiDto
{
    public string EmpNo { get; set; } = "";
    public string SystemName { get; set; } = "";
    public JsonElement Permissions { get; set; }
}

public sealed class UserSummaryApiDto
{
    public string EmpNo { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class UserDetailApiDto
{
    public string EmpNo { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public Dictionary<string, SystemPermissionEntryApiDto> SystemPermissions { get; set; } = new();
    public Dictionary<string, JsonElement> UserInformation { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class SystemPermissionEntryApiDto
{
    public string Role { get; set; } = "";
    public Dictionary<string, ControlPermissionApiDto>? Controls { get; set; }
}

public sealed class CreateCentralUserRequestDto
{
    public string EmpNo { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Password { get; set; } = "";
    public Dictionary<string, SystemPermissionEntryApiDto> SystemPermissions { get; set; } = new();
    public bool IsActive { get; set; }
}

public sealed class UpdateCentralUserRequestDto
{
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class UserProfileDto
{
    public long Id { get; set; }
    public string EmpNo { get; set; } = "";
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public sealed class ZyqdUserInformationDto
{
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>操作员也可查看全部客户；管理员始终为 true。</summary>
    public bool ViewAllCustomers { get; set; }

    /// <summary>报价签名名称（替代旧表 T_ZYQD_Quotation_signature.signature）</summary>
    public string? Signature { get; set; }

    /// <summary>报价签名图片路径（替代旧表 signature_url，一般为 /Img/...）</summary>
    public string? SignatureUrl { get; set; }
}

public sealed class JoinSystemRequestDto
{
    public string EmpNo { get; set; } = "";
    public string Password { get; set; } = "";
    public string SystemName { get; set; } = CentralApiOptions.AppSystemKey;
    public string DisplayName { get; set; } = "";
    public ZyqdUserInformationDto? UserInformation { get; set; }
}

public sealed class JoinSystemResponseDataDto
{
    public string EmpNo { get; set; } = "";
    public bool Created { get; set; }
    public bool PermissionAdded { get; set; }
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class VerifyUserEmailRequestDto
{
    public string EmpNo { get; set; } = "";
    public string SystemName { get; set; } = CentralApiOptions.AppSystemKey;
    public string Email { get; set; } = "";
}

public sealed class ResetPasswordRequestDto
{
    public string EmpNo { get; set; } = "";
    public string SystemName { get; set; } = CentralApiOptions.AppSystemKey;
    public string Email { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public sealed class UserInformationForSystemApiDto
{
    public string EmpNo { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string SystemName { get; set; } = "";
    public JsonElement Information { get; set; }
}

public sealed class SaveUserInformationRequestDto
{
    public ZyqdUserInformationDto Information { get; set; } = new();
    public string? DisplayName { get; set; }
}

public sealed class CustomerOptionDto
{
    public string Code { get; set; } = "";
    public string? Name { get; set; }
    public string Label =>
        string.IsNullOrWhiteSpace(Name) || string.Equals(Name, Code, StringComparison.OrdinalIgnoreCase)
            ? Code
            : $"{Code} {Name}";
}

public sealed class ManagedUserRowDto
{
    public string EmpNo { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; }
    public string Role { get; set; } = "";
    public bool ViewAllCustomers { get; set; }
    public bool CanViewInvoiceKp { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Signature { get; set; }
    public string? SignatureUrl { get; set; }
    public List<CustomerOptionDto> Customers { get; set; } = new();
    public string CustomersText =>
        ViewAllCustomers
            ? "全部客户"
            : Customers.Count == 0
                ? "未分配"
                : string.Join("、", Customers.Select(x => x.Code));
}

/// <summary>报价签名下拉项（人员档案汇总，替代旧 T_ZYQD_Quotation_signature）</summary>
public sealed class QuotationSignatureOptionDto
{
    public string EmpNo { get; set; } = "";
    public string Signature { get; set; } = "";
    public string SignatureUrl { get; set; } = "";
}

public sealed class SaveManagedUserDto
{
    public string EmpNo { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Password { get; set; }
    public string Role { get; set; } = "operator";
    public bool IsActive { get; set; }
    public bool ViewAllCustomers { get; set; }
    public bool CanViewInvoiceKp { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Signature { get; set; }
    public string? SignatureUrl { get; set; }
    public List<CustomerOptionDto> Customers { get; set; } = new();
    public bool IsCreate { get; set; }
}
