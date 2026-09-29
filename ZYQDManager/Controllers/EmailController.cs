using Microsoft.AspNetCore.Mvc;
using ZYQDManager.DataDtos;
using ZYQDManager.Services;
using ZYQDManager.Utilities;

namespace ZYQDManager.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    /// <summary>
    /// 发送邮件。发件人固定为遮阳表单系统管理员，只需传收件人与内容。
    /// POST api/Email/Send
    /// </summary>
    [HttpPost]
    public Task<ApiResponse<bool>> Send([FromBody] SendEmailDto dto, CancellationToken ct = default)
        => _emailService.SendAsync(dto, ct);
}
