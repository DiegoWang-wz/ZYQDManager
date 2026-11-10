namespace ZYQDManager.Utilities;

/// <summary>
/// 本地存储 / 会话存储的键名常量。
/// </summary>
public static class StorageKeys
{
    public const string OperatorId = "operatorId";
    public const string CentralAuth = "jcCentralAuth";
    /// <summary>上次成功登录的工号，退出/过期后仍用于回填登录框。</summary>
    public const string LastLoginEmpNo = "zyqdLastLoginEmpNo";
    /// <summary>登录密码仍是初始密码 123 的工号。改密成功或改用其他密码登录后清除。</summary>
    public const string MustChangePassword = "zyqdMustChangePassword";
}
