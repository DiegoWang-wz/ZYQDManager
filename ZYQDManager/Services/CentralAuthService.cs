using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

public class CentralAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly LocalStorageHelper _localStorage;
    private readonly CentralApiOptions _api;
    private readonly ILogger<CentralAuthService> _logger;

    public CentralAuthService(
        IHttpClientFactory httpFactory,
        LocalStorageHelper localStorage,
        IOptions<CentralApiOptions> options,
        ILogger<CentralAuthService> logger)
    {
        _http = httpFactory.CreateClient(CentralApiOptions.HttpClientName);
        _localStorage = localStorage;
        _api = options.Value;
        _logger = logger;
    }

    public async Task<StoredCentralAuth?> GetStoredAsync(CancellationToken ct = default)
    {
        try
        {
            return await _localStorage.GetAsync<StoredCentralAuth?>(StorageKeys.CentralAuth, ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsLoggedInAsync(CancellationToken ct = default)
    {
        var s = await GetStoredAsync(ct);
        return s is not null && !string.IsNullOrWhiteSpace(s.Token) && s.ExpiresAtUtc > DateTime.UtcNow;
    }

    public async Task<(bool Ok, string? Message)> LoginAsync(string empNo, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_api.BaseUrl))
            return (false, "未配置 CentralApi:BaseUrl");

        var loginPath = _api.Relative("auth/login");
        try
        {
            using var resp = await _http.PostAsJsonAsync(loginPath, new LoginRequestDto
            {
                EmpNo = empNo.Trim(),
                Password = password
            }, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<JcCommonApiEnvelope<LoginResponseDataDto>>(body, JsonOptions);
            if (parsed is null)
                return (false, "无法解析登录响应");
            if (parsed.Code != 200 || parsed.Data is null)
                return (false, string.IsNullOrWhiteSpace(parsed.Message) ? "登录失败" : parsed.Message);

            var data = parsed.Data;
            var storedEmpNo = string.IsNullOrWhiteSpace(data.EmpNo) ? empNo.Trim() : data.EmpNo.Trim();
            var (role, controls) = await LoadAppPermissionsAsync(data.Token, storedEmpNo, ct);
            if (string.IsNullOrWhiteSpace(role))
                return (false, "该账号无权登录遮阳系统（缺少 ZYQDManager 权限）");

            var stored = new StoredCentralAuth
            {
                Token = data.Token,
                EmpNo = storedEmpNo,
                ExpiresAtUtc = data.ExpiresAtUtc,
                Role = role,
                Controls = controls
            };
            await _localStorage.SetAsync(StorageKeys.CentralAuth, stored, ct);
            await _localStorage.SetOperatorIdAsync(storedEmpNo, ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoginAsync failed");
            return (false, "登录请求失败：" + ex.Root().Message);
        }
    }

    public async Task<(bool Ok, JoinSystemResponseDataDto? Data, string? Message)> JoinSystemAsync(
        string empNo,
        string password,
        string displayName,
        ZyqdUserInformationDto info,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_api.BaseUrl))
            return (false, null, "未配置 CentralApi:BaseUrl");

        var path = _api.Relative("auth/join-system");
        try
        {
            using var resp = await _http.PostAsJsonAsync(path, new JoinSystemRequestDto
            {
                EmpNo = empNo.Trim(),
                Password = password,
                SystemName = CentralApiOptions.AppSystemKey,
                DisplayName = (displayName ?? "").Trim(),
                UserInformation = info
            }, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<JcCommonApiEnvelope<JoinSystemResponseDataDto>>(body, JsonOptions);
            if (parsed is null)
                return (false, null, "无法解析注册响应");
            if (parsed.Code != 200 || parsed.Data is null)
                return (false, null, string.IsNullOrWhiteSpace(parsed.Message) ? "注册失败" : parsed.Message);
            return (true, parsed.Data, parsed.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JoinSystemAsync failed");
            return (false, null, "注册请求失败：" + ex.Root().Message);
        }
    }

    public async Task<(bool Ok, string? Message)> VerifyUserEmailAsync(
        string empNo, string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_api.BaseUrl))
            return (false, "未配置 CentralApi:BaseUrl");

        var path = _api.Relative("auth/verify-user-email");
        try
        {
            using var resp = await _http.PostAsJsonAsync(path, new VerifyUserEmailRequestDto
            {
                EmpNo = empNo.Trim(),
                SystemName = CentralApiOptions.AppSystemKey,
                Email = email.Trim()
            }, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<JcCommonApiEnvelope<object?>>(body, JsonOptions);
            if (parsed is null)
                return (false, "无法解析校验响应");
            if (parsed.Code != 200)
                return (false, string.IsNullOrWhiteSpace(parsed.Message) ? "工号与邮箱不匹配" : parsed.Message);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifyUserEmailAsync failed");
            return (false, "校验请求失败：" + ex.Root().Message);
        }
    }

    public async Task<(bool Ok, string? Message)> ResetPasswordAsync(
        string empNo, string email, string newPassword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_api.BaseUrl))
            return (false, "未配置 CentralApi:BaseUrl");

        var path = _api.Relative("auth/reset-password");
        try
        {
            using var resp = await _http.PostAsJsonAsync(path, new ResetPasswordRequestDto
            {
                EmpNo = empNo.Trim(),
                SystemName = CentralApiOptions.AppSystemKey,
                Email = email.Trim(),
                NewPassword = newPassword
            }, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<JcCommonApiEnvelope<object?>>(body, JsonOptions);
            if (parsed is null)
                return (false, "无法解析重置响应");
            if (parsed.Code != 200)
                return (false, string.IsNullOrWhiteSpace(parsed.Message) ? "重置密码失败" : parsed.Message);
            return (true, parsed.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResetPasswordAsync failed");
            return (false, "重置密码请求失败：" + ex.Root().Message);
        }
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        await _localStorage.RemoveAsync(StorageKeys.CentralAuth, ct);
        await _localStorage.RemoveAsync(StorageKeys.OperatorId, ct);
    }

    public async Task<string?> GetCurrentEmpNoAsync(CancellationToken ct = default)
    {
        var auth = await GetStoredAsync(ct);
        var empNo = (auth?.EmpNo ?? "").Trim();
        if (empNo.Length > 0)
            return empNo;
        return await _localStorage.GetOperatorIdAsync(ct);
    }

    public bool CanManageUsers(StoredCentralAuth? auth)
    {
        if (auth is null) return false;
        return string.Equals(auth.Role, "admin", StringComparison.OrdinalIgnoreCase)
               || string.Equals(auth.EmpNo, "JcAdmin", StringComparison.OrdinalIgnoreCase);
    }

    public bool CanViewAllCustomers(StoredCentralAuth? auth, ZyqdUserInformationDto? info)
    {
        if (CanManageUsers(auth)) return true;
        return info?.ViewAllCustomers == true;
    }

    /// <summary>
    /// 页面是否可见。admin / JcAdmin 全开；operator 看 JcCommonApi Controls（键为路由 URL）。
    /// </summary>
    public bool IsPageVisible(StoredCentralAuth? auth, string route)
    {
        if (auth is null) return false;
        if (CanManageUsers(auth)) return true;

        var key = CentralApiOptions.NormalizeControlKey(route);
        if (key.Length == 0) return false;
        if (auth.Controls is null || auth.Controls.Count == 0) return false;

        foreach (var kv in auth.Controls)
        {
            if (!string.Equals(CentralApiOptions.NormalizeControlKey(kv.Key), key, StringComparison.OrdinalIgnoreCase))
                continue;
            return kv.Value.Visible;
        }

        return false;
    }

    public bool IsPageDisabled(StoredCentralAuth? auth, string route)
    {
        if (auth is null) return true;
        if (CanManageUsers(auth)) return false;

        var key = CentralApiOptions.NormalizeControlKey(route);
        if (key.Length == 0) return true;
        if (auth.Controls is null || auth.Controls.Count == 0) return true;

        foreach (var kv in auth.Controls)
        {
            if (!string.Equals(CentralApiOptions.NormalizeControlKey(kv.Key), key, StringComparison.OrdinalIgnoreCase))
                continue;
            return !kv.Value.Visible || kv.Value.Disabled;
        }

        return true;
    }

    /// <summary>重新从 JcCommonApi 拉取本系统 Role + Controls 并写回本地缓存。</summary>
    public async Task RefreshPermissionsAsync(CancellationToken ct = default)
    {
        var auth = await GetStoredAsync(ct);
        if (auth is null || string.IsNullOrWhiteSpace(auth.Token) || string.IsNullOrWhiteSpace(auth.EmpNo))
            return;

        var (role, controls) = await LoadAppPermissionsAsync(auth.Token, auth.EmpNo, ct);
        if (string.IsNullOrWhiteSpace(role))
            return;

        auth.Role = role;
        auth.Controls = controls;
        await _localStorage.SetAsync(StorageKeys.CentralAuth, auth, ct);
    }

    private async Task<(string? Role, Dictionary<string, ControlPermissionApiDto> Controls)> LoadAppPermissionsAsync(
        string token, string empNo, CancellationToken ct)
    {
        var controls = new Dictionary<string, ControlPermissionApiDto>(StringComparer.OrdinalIgnoreCase);
        var path = _api.Relative($"users/{Uri.EscapeDataString(empNo)}/permissions/{CentralApiOptions.AppSystemKey}");
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<JcCommonApiEnvelope<SystemPermissionsForSystemApiDto>>(body, JsonOptions);
        if (parsed is null || parsed.Code != 200 || parsed.Data is null)
            return (null, controls);

        var perms = parsed.Data.Permissions;
        if (perms.ValueKind != JsonValueKind.Object)
            return ("operator", controls);

        string role = "operator";
        foreach (var p in perms.EnumerateObject())
        {
            if (p.Name.Equals("Role", StringComparison.OrdinalIgnoreCase)
                && p.Value.ValueKind == JsonValueKind.String)
            {
                var r = (p.Value.GetString() ?? "").Trim();
                role = string.IsNullOrEmpty(r) ? "operator" : r;
            }
            else if (p.Name.Equals("Controls", StringComparison.OrdinalIgnoreCase)
                     && p.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var c in p.Value.EnumerateObject())
                {
                    var key = CentralApiOptions.NormalizeControlKey(c.Name);
                    if (key.Length == 0) continue;
                    var visible = false;
                    var disabled = false;
                    if (c.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var f in c.Value.EnumerateObject())
                        {
                            if (f.Name.Equals("visible", StringComparison.OrdinalIgnoreCase)
                                && (f.Value.ValueKind is JsonValueKind.True or JsonValueKind.False))
                                visible = f.Value.GetBoolean();
                            else if (f.Name.Equals("disabled", StringComparison.OrdinalIgnoreCase)
                                     && (f.Value.ValueKind is JsonValueKind.True or JsonValueKind.False))
                                disabled = f.Value.GetBoolean();
                        }
                    }

                    controls[key] = new ControlPermissionApiDto { Visible = visible, Disabled = disabled };
                }
            }
        }

        return (role, controls);
    }
}
