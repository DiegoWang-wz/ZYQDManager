using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZYQDManager.DataDtos;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

/// <summary>
/// 读写 JcCommonApi CentralUsers.UserInformation 中 ZYQDManager 这一块
///（部门 / 邮箱 / 电话 / 报价签名等）。
/// </summary>
public class CentralUserInformationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly LocalStorageHelper _localStorage;
    private readonly CentralApiOptions _api;
    private readonly ILogger<CentralUserInformationService> _logger;

    public CentralUserInformationService(
        IHttpClientFactory httpFactory,
        LocalStorageHelper localStorage,
        IOptions<CentralApiOptions> options,
        ILogger<CentralUserInformationService> logger)
    {
        _httpFactory = httpFactory;
        _localStorage = localStorage;
        _api = options.Value;
        _logger = logger;
    }

    public static ZyqdUserInformationDto? FromDetail(UserDetailApiDto? detail)
    {
        if (detail?.UserInformation is null)
            return null;

        foreach (var kv in detail.UserInformation)
        {
            if (!kv.Key.Equals(CentralApiOptions.AppSystemKey, StringComparison.OrdinalIgnoreCase))
                continue;
            if (kv.Value.ValueKind != JsonValueKind.Object)
                return new ZyqdUserInformationDto();
            return JsonSerializer.Deserialize<ZyqdUserInformationDto>(kv.Value.GetRawText(), JsonOptions)
                   ?? new ZyqdUserInformationDto();
        }

        return null;
    }

    public async Task<(bool Ok, string DisplayName, ZyqdUserInformationDto? Info, string? Message)> GetAsync(
        string empNo, CancellationToken ct = default)
    {
        var (client, token, err) = await CreateAuthedClientAsync(ct);
        if (err is not null) return (false, "", null, err);

        var path = _api.Relative(
            $"users/{Uri.EscapeDataString(empNo.Trim())}/user-information/{CentralApiOptions.AppSystemKey}");
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await client.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        try
        {
            var env = JsonSerializer.Deserialize<JcCommonApiEnvelope<UserInformationForSystemApiDto>>(body, JsonOptions);
            if (env is null) return (false, "", null, "无法解析档案响应");
            if (env.Code != 200 || env.Data is null)
                return (false, "", null, string.IsNullOrWhiteSpace(env.Message) ? "读取档案失败" : env.Message);

            var info = env.Data.Information.ValueKind == JsonValueKind.Object
                ? JsonSerializer.Deserialize<ZyqdUserInformationDto>(env.Data.Information.GetRawText(), JsonOptions)
                : new ZyqdUserInformationDto();
            return (true, env.Data.DisplayName ?? "", info ?? new ZyqdUserInformationDto(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAsync failed, empNo={EmpNo}", empNo);
            return (false, "", null, "无法解析档案响应");
        }
    }

    public async Task<(bool Ok, string? Message)> SaveAsync(
        string empNo, ZyqdUserInformationDto info, string? displayName = null, CancellationToken ct = default)
    {
        var (client, token, err) = await CreateAuthedClientAsync(ct);
        if (err is not null) return (false, err);

        var path = _api.Relative(
            $"users/{Uri.EscapeDataString(empNo.Trim())}/user-information/{CentralApiOptions.AppSystemKey}/save");
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new SaveUserInformationRequestDto
        {
            Information = info,
            DisplayName = displayName
        }, options: JsonOptions);
        using var resp = await client.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        try
        {
            var env = JsonSerializer.Deserialize<JcCommonApiEnvelope<object?>>(text, JsonOptions);
            if (env is null) return (false, "无法解析档案响应");
            if (env.Code != 200)
                return (false, string.IsNullOrWhiteSpace(env.Message) ? "保存档案失败" : env.Message);
            return (true, env.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveAsync failed, empNo={EmpNo}", empNo);
            return (false, "无法解析档案响应");
        }
    }

    /// <summary>
    /// 汇总已配置报价签名的人员，供报价录入下拉使用（替代旧 T_ZYQD_Quotation_signature 表）。
    /// 全员列表需 CentralUsersAdmin；业务员无权限时回退为仅本人中/英签名。
    /// </summary>
    public async Task<(bool Ok, List<QuotationSignatureOptionDto> List, string? Message)> ListSignatureOptionsAsync(
        CancellationToken ct = default)
    {
        var (client, token, err) = await CreateAuthedClientAsync(ct);
        if (err is not null) return (false, [], err);

        using var listReq = new HttpRequestMessage(HttpMethod.Get, _api.Relative("users"));
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var listResp = await client.SendAsync(listReq, ct);
        var listBody = await listResp.Content.ReadAsStringAsync(ct);
        JcCommonApiEnvelope<List<UserSummaryApiDto>>? listEnv = null;
        var listOk = false;
        try
        {
            listEnv = JsonSerializer.Deserialize<JcCommonApiEnvelope<List<UserSummaryApiDto>>>(listBody, JsonOptions);
            listOk = listEnv is not null && listEnv.Code == 200 && listEnv.Data is not null;
        }
        catch
        {
            listOk = false;
        }

        // 403 / 非 JSON / 无权限：业务员走本人档案，不再弹「无法解析人员列表」
        if (!listOk)
            return await ListCurrentUserSignatureOptionsAsync(ct);

        var result = new List<QuotationSignatureOptionDto>();
        foreach (var u in listEnv!.Data!.Where(x => x.IsActive))
        {
            var (ok, displayName, info, _) = await GetAsync(u.EmpNo, ct);
            if (!ok || info is null) continue;

            var fallbackName = string.IsNullOrWhiteSpace(displayName) ? u.DisplayName : displayName;
            if (string.IsNullOrWhiteSpace(fallbackName))
                fallbackName = u.EmpNo;

            TryAddSignatureOption(result, u.EmpNo, "cn", info.Signature, info.SignatureUrl, fallbackName);
            TryAddSignatureOption(
                result,
                u.EmpNo,
                "en",
                info.SignatureEn,
                info.SignatureUrlEn,
                string.IsNullOrWhiteSpace(info.EnglishName) ? fallbackName : info.EnglishName!);
        }

        return (true, result
            .OrderBy(x => x.Signature, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Kind, StringComparer.OrdinalIgnoreCase)
            .ToList(), null);
    }

    /// <summary>仅当前登录人已配置的中/英报价签名。</summary>
    private async Task<(bool Ok, List<QuotationSignatureOptionDto> List, string? Message)> ListCurrentUserSignatureOptionsAsync(
        CancellationToken ct)
    {
        var auth = await _localStorage.GetAsync<StoredCentralAuth?>(StorageKeys.CentralAuth, ct);
        var empNo = (auth?.EmpNo ?? "").Trim();
        if (empNo.Length == 0)
            return (false, [], "未登录");

        var (ok, displayName, info, msg) = await GetAsync(empNo, ct);
        if (!ok || info is null)
            return (false, [], msg ?? "读取本人档案失败");

        var fallbackName = string.IsNullOrWhiteSpace(displayName) ? empNo : displayName;
        var result = new List<QuotationSignatureOptionDto>();
        TryAddSignatureOption(result, empNo, "cn", info.Signature, info.SignatureUrl, fallbackName);
        TryAddSignatureOption(
            result,
            empNo,
            "en",
            info.SignatureEn,
            info.SignatureUrlEn,
            string.IsNullOrWhiteSpace(info.EnglishName) ? fallbackName : info.EnglishName!);

        return (true, result
            .OrderBy(x => x.Kind == "cn" ? 0 : 1)
            .ThenBy(x => x.Signature, StringComparer.OrdinalIgnoreCase)
            .ToList(), null);
    }

    private static void TryAddSignatureOption(
        List<QuotationSignatureOptionDto> list,
        string empNo,
        string kind,
        string? signature,
        string? signatureUrl,
        string fallbackName)
    {
        var url = (signatureUrl ?? "").Trim();
        if (url.Length == 0) return;

        var name = (signature ?? "").Trim();
        if (name.Length == 0)
            name = (fallbackName ?? "").Trim();
        if (name.Length == 0)
            name = empNo;

        list.Add(new QuotationSignatureOptionDto
        {
            EmpNo = empNo,
            Kind = kind,
            Signature = name,
            SignatureUrl = url
        });
    }

    private async Task<(HttpClient Client, string? Token, string? Error)> CreateAuthedClientAsync(CancellationToken ct)
    {
        var auth = await _localStorage.GetAsync<StoredCentralAuth?>(StorageKeys.CentralAuth, ct);
        if (auth is null || string.IsNullOrWhiteSpace(auth.Token))
            return (null!, null, "未登录");
        return (_httpFactory.CreateClient(CentralApiOptions.HttpClientName), auth.Token, null);
    }
}
