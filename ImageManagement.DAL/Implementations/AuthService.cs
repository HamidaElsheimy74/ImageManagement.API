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

            var claims = new[]
         {
            new Claim("UserName", username),
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt!.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwt.ExpirationTime),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);

        }
        catch (Exception ex)
        {
            throw new Exception("Error generating JWT token", ex);
        }
    }
}
