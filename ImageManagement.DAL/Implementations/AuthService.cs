using ImageManagement.Infrastructure.Interfaces;
using ImageManagement.Infrastructure.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ImageManagement.Infrastructure.Implementations;
public class AuthService : IAuthService
{

    public async Task<string> GenerateJwtToken(string username, IConfiguration config)
    {
        try
        {
            var _jwt = config.GetSection("JWT")?.Get<JWT>();
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwt!.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                     {
                      }),
                Expires = DateTime.UtcNow.AddMinutes(_jwt.ExpirationTime),
                Issuer = config[_jwt.Issuer],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);

        }
        catch (Exception ex)
        {
            throw new Exception("Error generating JWT token", ex);
        }
    }
}
