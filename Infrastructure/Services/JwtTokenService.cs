using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinGround.Application.Common.Interfaces;
using FinGround.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinGround.Infrastructure.Services;

public class JwtTokenService : ITokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        _secretKey = jwt["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured.");
        _issuer = jwt["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured.");
        _audience = jwt["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured.");
        _expiryMinutes = int.TryParse(jwt["ExpiryMinutes"], out var mins) ? mins : 60;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name,  user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                      DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                      ClaimValueTypes.Integer64),
            // Role claim — ASP.NET Core's [Authorize(Roles = "...")] reads
            // ClaimTypes.Role, which the JWT bearer middleware maps from the
            // standard "role" claim automatically.
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: ExpiresAt(),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime ExpiresAt() =>
        DateTime.UtcNow.AddMinutes(_expiryMinutes);
}
