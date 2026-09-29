using Microsoft.EntityFrameworkCore;
using ZYQDManager.DataDtos;
using ZYQDManager.DataModels;
using ZYQDManager.Utilities;

namespace ZYQDManager.Services;

public interface IUserProfileService
{
    Task<List<UserProfileDto>> GetAllAsync(CancellationToken ct = default);
    Task<UserProfileDto?> GetByEmpNoAsync(string empNo, CancellationToken ct = default);
    Task<ApiResponse<bool>> UpsertAsync(string empNo, string? department, string? email, string? phone, CancellationToken ct = default);
}

public class UserProfileService : IUserProfileService
{
    private readonly SqlServerDbContext _db;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(SqlServerDbContext db, ILogger<UserProfileService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<UserProfileDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.UserProfiles.AsNoTracking()
            .OrderBy(x => x.EmpNo)
            .Select(x => new UserProfileDto
            {
                Id = x.Id,
                EmpNo = x.EmpNo,
                Department = x.Department,
                Email = x.Email,
                Phone = x.Phone
            })
            .ToListAsync(ct);
    }

    public async Task<UserProfileDto?> GetByEmpNoAsync(string empNo, CancellationToken ct = default)
    {
        empNo = (empNo ?? "").Trim();
        if (string.IsNullOrEmpty(empNo)) return null;
        return await _db.UserProfiles.AsNoTracking()
            .Where(x => x.EmpNo == empNo)
            .Select(x => new UserProfileDto
            {
                Id = x.Id,
                EmpNo = x.EmpNo,
                Department = x.Department,
                Email = x.Email,
                Phone = x.Phone
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ApiResponse<bool>> UpsertAsync(
        string empNo, string? department, string? email, string? phone, CancellationToken ct = default)
    {
        empNo = (empNo ?? "").Trim();
        if (string.IsNullOrEmpty(empNo) || empNo.Length > 20)
            return ApiResponse<bool>.BadRequest("工号无效");

        try
        {
            var row = await _db.UserProfiles.FirstOrDefaultAsync(x => x.EmpNo == empNo, ct);
            if (row is null)
            {
                _db.UserProfiles.Add(new ZyqdUserProfileModel
                {
                    EmpNo = empNo,
                    Department = TrimOrNull(department, 100),
                    Email = TrimOrNull(email, 200),
                    Phone = TrimOrNull(phone, 50)
                });
            }
            else
            {
                row.Department = TrimOrNull(department, 100);
                row.Email = TrimOrNull(email, 200);
                row.Phone = TrimOrNull(phone, 50);
            }

            await _db.SaveChangesAsync(ct);
            return ApiResponse<bool>.Ok(true, "档案已保存");
        }
        catch (Exception ex)
        {
            var root = ex.Root();
            _logger.LogError(ex, "UpsertAsync failed, empNo={EmpNo}", empNo);
            return ApiResponse<bool>.Fail("保存档案失败：" + root.Message);
        }
    }

    private static string? TrimOrNull(string? value, int maxLen)
    {
        var v = (value ?? "").Trim();
        if (string.IsNullOrEmpty(v)) return null;
        return v.Length <= maxLen ? v : v[..maxLen];
    }
}
