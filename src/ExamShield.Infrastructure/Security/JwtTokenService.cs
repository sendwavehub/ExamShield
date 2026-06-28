using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExamShield.Domain.Entities;
using ExamShield.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ExamShield.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        _issuer = configuration["Jwt:Issuer"] ?? "ExamShield";
        _audience = configuration["Jwt:Audience"] ?? "ExamShield";
        _expirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var m) ? m : 60;
    }

    public string Generate(User user) => GenerateCore(user, mfaVerified: false);

    public string GenerateWithMfa(User user) => GenerateCore(user, mfaVerified: true);

    private string GenerateCore(User user, bool mfaVerified)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (mfaVerified)
            claims.Add(new Claim("amr", "mfa"));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
