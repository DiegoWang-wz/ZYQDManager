using Microsoft.AspNetCore.Mvc;
using ZYQDManager.DataDtos;
using ZYQDManager.Services;
using ZYQDManager.Utilities;

namespace ZYQDManager.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class AppController : ControllerBase
{
    private readonly IAppService _appService;

    public AppController(IAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public Task<ApiResponse<DbStatusDto>> GetDbStatus(CancellationToken ct = default)
        => _appService.GetDbStatusAsync(ct);
}
