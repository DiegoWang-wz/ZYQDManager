using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

public interface IAppService
{
    Task<ApiResponse<DbStatusDto>> GetDbStatusAsync(CancellationToken ct = default);
}

public class AppService : IAppService
{
    private readonly SqlServerDbContext _db;
    private readonly ILogger<AppService> _logger;

    public AppService(SqlServerDbContext db, ILogger<AppService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<DbStatusDto>> GetDbStatusAsync(CancellationToken ct = default)
    {
        var dto = new DbStatusDto
        {
            Key = "Sunlight_Management_BU",
            Database = _db.Database.GetDbConnection().Database
        };

        var sw = Stopwatch.StartNew();
        try
        {
            dto.Connected = await _db.Database.CanConnectAsync(ct);
            sw.Stop();
            dto.ElapsedMs = sw.ElapsedMilliseconds;
            return dto.Connected
                ? ApiResponse<DbStatusDto>.Ok(dto, "数据库连接正常")
                : ApiResponse<DbStatusDto>.Fail("无法连接到数据库", data: dto);
        }
        catch (Exception ex)
        {
            sw.Stop();
            dto.ElapsedMs = sw.ElapsedMilliseconds;
            dto.Connected = false;
            dto.Error = ex.Root().Message;
            _logger.LogError(ex, "GetDbStatusAsync failed: {Msg}", dto.Error);
            return ApiResponse<DbStatusDto>.Fail(dto.Error, data: dto);
        }
    }
}
