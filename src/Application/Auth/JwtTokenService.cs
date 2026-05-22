using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AfriPay.Application.Auth.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace AfriPay.Application.Auth;

public sealed class JwtTokenService(IConfiguration config) : ITokenService
{
    private readonly string _secret   = config["Jwt:Secret"]
                                        ?? throw new InvalidOperationException("Jwt:Secret not configured.");
    private readonly string _issuer   = config["Jwt:Issuer"]   ?? "AfriPay";
    private readonly string _audience = config["Jwt:Audience"] ?? "AfriPay";
    private readonly int    _expMinutes = int.Parse(config["Jwt:ExpiresInMinutes"] ?? "15");
 
    public string GenerateAccessToken(
        Guid merchantId, string email, string plan, string role = "merchant")
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
 
        var claims = new[]
        {
            new Claim("merchantId",   merchantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim("plan", plan),
            new Claim(ClaimTypes.Role, role),
        };
 
        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_expMinutes),
            signingCredentials: creds);
 
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
 
    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}