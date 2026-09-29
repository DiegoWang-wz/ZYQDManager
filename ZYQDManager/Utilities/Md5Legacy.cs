using System.Security.Cryptography;
using System.Text;

namespace ZYQDManager.Utilities;

/// <summary>与老系统 EM.Busness.MD5T.MD5Encrypt 一致（UTF-8、小写 hex）。</summary>
public static class Md5Legacy
{
    public static string Encrypt(string? input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input ?? ""));
        var sb = new StringBuilder(32);
        foreach (var b in bytes)
            sb.Append(b.ToString("x").PadLeft(2, '0'));
        return sb.ToString();
    }

    /// <summary>
    /// saltCode = MD5(filePath) + MD5(MD5(yyyy/MM) + MD5("330624"))
    /// </summary>
    public static string FileOperationSalt(string filePath, string saltKey)
    {
        var month = DateTime.Now.ToString("yyyy/MM");
        return Encrypt(filePath) + Encrypt(Encrypt(month) + Encrypt(saltKey));
    }
}
