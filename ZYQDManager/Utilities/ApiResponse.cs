namespace ZYQDManager.Utilities;

public class ApiResponse
{
    /// <summary>
    /// 结果编码
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 结果信息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 数据
    /// </summary>
    public object? Data { get; set; }
}

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T? data, string message = "OK")
        => new ApiResponse<T> { Code = 1, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, int code = 0, T? data = default)
        => new ApiResponse<T> { Code = code, Message = message, Data = data };

    public static ApiResponse<T> Canceled(string message = "Request canceled")
        => new ApiResponse<T> { Code = -2, Message = message, Data = default };

    public static ApiResponse<T> NotFound(string message = "Not Found")
        => new ApiResponse<T> { Code = 404, Message = message, Data = default };

    /// <summary>参数错误专用</summary>
    public static ApiResponse<T> BadRequest(string message = "Bad Request")
        => new ApiResponse<T> { Code = 0, Message = message, Data = default };
}