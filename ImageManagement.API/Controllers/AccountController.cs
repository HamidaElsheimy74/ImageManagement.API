using ImageManagement.BLL.Interfaces;
using ImageManagement.BLL.Models;
using ImageManagement.Common.DTOs;
using ImageManagement.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ImageManagement.API.Controllers;

public class AccountController : BaseAPIController
{
    private readonly ILoginService _loginService;
    private readonly ILogger<AccountController> _logger;
    public AccountController(
        ILoginService loginService,
        ILogger<AccountController> logger)
    {
        _loginService = loginService;
        _logger = logger;
    }

    /// <summary>
    /// Login to the system
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("Login")]
    [AllowAnonymous]
    [DisableRateLimiting]
    public async Task<IActionResult> Login([FromBody] LoginData model)
    {
        try
        {
            if (!ModelState.IsValid || model is null)
            {
                return BadRequest(new ResponseResult(ErrorsHandler.Invalid_LoginModel, null!, 400));
            }

            var result = await _loginService.LoginAsync(model.UserName, model.Password);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while login");
            return StatusCode(500, new { Message = ErrorsHandler.Internal_Server_Error });
        }
    }

}
