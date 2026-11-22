using System.Text.Json;

namespace ZYQDManager.Utilities;

/// <summary>
/// 表单「有没有未保存的修改」的判定工具。
///
/// 单据页字段太多（几百个 key），不可能逐个挂 @onchange 记脏标记，
/// 所以改成整体快照：加载/保存完把当前模型序列化存成字符串，
/// 提交前再序列化一次比较，字符串不同就说明用户改过东西。
/// </summary>
public static class FormSnapshot
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };

    /// <summary>把若干模型序列化成一个稳定的字符串（属性顺序固定，字典按插入顺序）。</summary>
    public static string Of(params object?[] parts) => JsonSerializer.Serialize(parts, Options);

    /// <summary>两份快照是否一致。</summary>
    public static bool Same(string? a, string? b) => string.Equals(a, b, StringComparison.Ordinal);
}
