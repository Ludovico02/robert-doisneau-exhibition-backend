using Microsoft.IdentityModel.Tokens;
using RobertDoisneau.Login.WebApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RobertDoisneau.Login.WebApi.Services;

public class JWTService
{
    private readonly IConfiguration _config;

    public JWTService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(User user)
    {
        var key = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Configuration value 'Jwt:Key' is missing or empty. Set it in appsettings or environment variables.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
           new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    
           new Claim(JwtRegisteredClaimNames.Email, user.Email),
           new Claim(ClaimTypes.Name, user.Username),

           new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),

           // Per ora non ci sono ruoli, lo imposto fisso a ruolo User
           new Claim(ClaimTypes.Role, "User")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}