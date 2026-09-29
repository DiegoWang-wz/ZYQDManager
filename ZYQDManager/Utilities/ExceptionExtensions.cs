namespace ZYQDManager.Utilities;

public static class ExceptionExtensions
{
    /// <summary>获取最内层异常</summary>
    public static Exception Root(this Exception ex)
    {
        while (ex.InnerException != null)
            ex = ex.InnerException;
        return ex;
    }
}
