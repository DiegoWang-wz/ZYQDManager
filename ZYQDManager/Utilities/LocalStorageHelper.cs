using Blazored.LocalStorage;

namespace ZYQDManager.Utilities;

/// <summary>
/// LocalStorage 便捷封装，便于在页面中存取信息。
/// </summary>
public class LocalStorageHelper
{
    private readonly ILocalStorageService _storage;

    public LocalStorageHelper(ILocalStorageService storage)
    {
        _storage = storage;
    }

    /// <summary>获取指定键的字符串，不存在或为空时返回 null。</summary>
    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        return await _storage.GetItemAsync<string?>(key, ct).ConfigureAwait(false);
    }

    /// <summary>获取指定键的值，反序列化为 T；不存在时返回 default。</summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return await _storage.GetItemAsync<T?>(key, ct).ConfigureAwait(false);
    }

    /// <summary>获取指定键的值，不存在或为空时返回 <paramref name="defaultValue"/>。</summary>
    public async Task<string> GetStringOrDefaultAsync(string key, string defaultValue = "", CancellationToken ct = default)
    {
        var v = await GetStringAsync(key, ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(v) ? defaultValue : v.Trim();
    }

    /// <summary>设置字符串。</summary>
    public async Task SetAsync(string key, string? value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value))
            await _storage.RemoveItemAsync(key, ct).ConfigureAwait(false);
        else
            await _storage.SetItemAsync(key, value.Trim(), ct).ConfigureAwait(false);
    }

    /// <summary>设置任意可序列化类型。</summary>
    public async Task SetAsync<T>(string key, T value, CancellationToken ct = default)
    {
        await _storage.SetItemAsync(key, value, ct).ConfigureAwait(false);
    }

    /// <summary>移除指定键。</summary>
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _storage.RemoveItemAsync(key, ct).ConfigureAwait(false);
    }

    /// <summary>是否包含指定键。</summary>
    public async Task<bool> ContainsKeyAsync(string key, CancellationToken ct = default)
    {
        return await _storage.ContainKeyAsync(key, ct).ConfigureAwait(false);
    }

    /// <summary>清空所有项。</summary>
    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _storage.ClearAsync(ct).ConfigureAwait(false);
    }

    public async Task<string?> GetOperatorIdAsync(CancellationToken ct = default)
    {
        var v = await GetStringAsync(StorageKeys.OperatorId, ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    public async Task SetOperatorIdAsync(string? operatorId, CancellationToken ct = default)
    {
        await SetAsync(StorageKeys.OperatorId, operatorId, ct).ConfigureAwait(false);
    }
}
