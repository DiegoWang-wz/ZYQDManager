using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

public class CentralUserManagementService
{
    private static readonly JsonSerializerOptions JsonApi = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions JsonEnvelope = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly LocalStorageHelper _localStorage;
    private readonly CentralApiOptions _api;

    public CentralUserManagementService(
        IHttpClientFactory httpFactory,
        LocalStorageHelper localStorage,
        IOptions<CentralApiOptions> options)
    {
        _httpFactory = httpFactory;
        _localStorage = localStorage;
        _api = options.Value;
    }

    public async Task<(bool Ok, IReadOnlyList<UserSummaryApiDto>? List, string? Message)> ListUsersAsync(
        bool? activeOnly, CancellationToken ct = default)
    {
        var (client, token, err) = await CreateAuthedClientAsync(ct);
        if (err is not null) return (false, null, err);
        var q = activeOnly is null ? "" : $"?activeOnly={(activeOnly.Value ? "true" : "false")}";
        using var req = new HttpRequestMessage(HttpMethod.Get, _api.Relative("users" + q));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await client.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (resp.StatusCode == HttpStatusCode.Forbidden)
            return (false, null, "没有用户管理权限");
        try
        {
            var env = JsonSerializer.Deserialize<JcCommonApiEnvelope<List<UserSummaryApiDto>>>(body, JsonEnvelope);
            if (env is null) return (false, null, "无法解析响应");
            if (env.Code != 200 || env.Data is null)
                return (false, null, string.IsNullOrWhiteSpace(env.Message) ? "请求失败" : env.Message);
            return (true, env.Data, null);
        }
        catch
        {
            return (false, null, "无法解析响应");
        }
    }

    public async Task<(bool Ok, UserDetailApiDto? Detail, string? Message)> GetUserAsync(
        string empNo, CancellationToken ct = default)
    {
        var (client, token, err) = await CreateAuthedClientAsync(ct);
        if (err is not null) return (false, null, err);
        using var req = new HttpRequestMessage(HttpMethod.Get, _api.Relative("users/" + Uri.EscapeDataString(empNo.Trim())));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await client.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        try
        {
            var env = JsonSerializer.Deserialize<JcCommonApiEnvelope<UserDetailApiDto>>(body, JsonEnvelope);
            if (env is null) return (false, null, "无法解析响应");
            if (env.Code != 200 || env.Data is null)
                return (false, null, string.IsNullOrWhiteSpace(env.Message) ? "请求失败" : env.Message);
            return (true, env.Data, null);
        }
        catch
        {
            return (false, null, "无法解析响应");
        }
    }

    public async Task<(bool Ok, string? Message)> CreateUserAsync(
        CreateCentralUserRequestDto request, CancellationToken ct = default)
        => await SendAsync(HttpMethod.Post, _api.Relative("users"), request, ct);

    public async Task<(bool Ok, string? Message)> UpdateUserAsync(
        string empNo, UpdateCentralUserRequestDto request, CancellationToken ct = default)
        => await SendAsync(HttpMethod.Post, _api.Relative("users/" + Uri.EscapeDataString(empNo.Trim()) + "/update"), request, ct);

    public async Task<(bool Ok, string? Message)> SaveAppPermissionsAsync(
        string empNo, string role, Dictionary<string, ControlPermissionApiDto>? controls, CancellationToken ct = default)
    {
        var entry = new SystemPermissionEntryApiDto
        {
            Role = string.IsNullOrWhiteSpace(role) ? "operator" : role.Trim(),
            Controls = controls
        };
        return await SendAsync(
            HttpMethod.Post,
            _api.Relative("users/" + Uri.EscapeDataString(empNo.Trim())
                                   + "/permissions/" + CentralApiOptions.AppSystemKey + "/save"),
            entry,
            ct);
    }

    [Obsolete("Use SaveAppPermissionsAsync to preserve Controls")]
    public async Task<(bool Ok, string? Message)> SaveAppRoleAsync(
        string empNo, string role, CancellationToken ct = default)
        => await SaveAppPermissionsAsync(empNo, role, null, ct);

    public async Task<(bool Ok, string? Message)> DeactivateUserAsync(string empNo, CancellationToken ct = default)
        => await SendAsync(HttpMethod.Post, _api.Relative("users/" + Uri.EscapeDataString(empNo.Trim()) + "/deactivate"), null, ct);

    private async Task<(bool Ok, string? Message)> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var (client, token, err) = await CreateAuthedClientAsync(ct);
        if (err is not null) return (false, err);
        using var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            req.Content = JsonContent.Create(body, options: JsonApi);
        using var resp = await client.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (resp.StatusCode == HttpStatusCode.Forbidden)
            return (false, "没有权限执行该操作");
        try
        {
            var env = JsonSerializer.Deserialize<JcCommonApiEnvelope<object?>>(text, JsonEnvelope);
            if (env is null) return (false, "无法解析响应");
            if (env.Code != 200)
                return (false, string.IsNullOrWhiteSpace(env.Message) ? "请求失败" : env.Message);
            return (true, env.Message);
        }
        catch
        {
            return (false, "无法解析响应");
        }
    }

    private async Task<(HttpClient Client, string? Token, string? Error)> CreateAuthedClientAsync(CancellationToken ct)
    {
        var auth = await _localStorage.GetAsync<StoredCentralAuth?>(StorageKeys.CentralAuth, ct);
        if (auth is null || string.IsNullOrWhiteSpace(auth.Token))
            return (null!, null, "未登录");
        return (_httpFactory.CreateClient(CentralApiOptions.HttpClientName), auth.Token, null);
    }
}
