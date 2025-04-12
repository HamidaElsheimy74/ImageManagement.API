using ImageManagement.BLL.Interfaces;
using ImageManagement.BLL.Models;
using ImageManagement.Common.DTOs;
using ImageManagement.Common.Errors;
using ImageManagement.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ImageManagement.BLL.Services;
public class LoginService : ILoginService
{
    private readonly ILogger<LoginService> _logger;
    private readonly IConfiguration _config;
    private IAuthService _authService;
    public LoginService(ILogger<LoginService> logger, IConfiguration config, IAuthService authService)
    {
        _logger = logger;
        _config = config;
        _authService = authService;
    }

    public async Task<ResponseResult> LoginAsync(string username, string password)
    {

        try
        {
            var loginData = _config.GetSection("LoginData").Get<LoginData>();

            if (username == loginData!.UserName && password == loginData.Password)
            {
                var token = await _authService.GenerateJwtToken(username, _config);
                return new ResponseResult()
                {
                    StatusCode = StatusCodes.Status200OK,
                    Data = token
                };
            }
            else
            {
                return new ResponseResult(ErrorsHandler.Invalid_Credentials, null!, 401);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return new ResponseResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = ErrorsHandler.Internal_Server_Error
            };
        }
    }
}
