using Microsoft.Extensions.Configuration;

namespace ImageManagement.Infrastructure.Interfaces;
public interface IAuthService
{
    Task<string> GenerateJwtToken(string username, IConfiguration config);
}
