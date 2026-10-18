using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenService(IConfiguration configuration, IDateTimeProvider dateTimeProvider)
    {
        _configuration = configuration;
        _dateTimeProvider = dateTimeProvider;
    }

    public (string AccessToken, DateTimeOffset ExpiresAt) GenerateAccessToken(AppUser user, IEnumerable<string>? roles = null, Guid? workspaceId = null)
    {
        var signingKey = _configuration["Jwt:SigningKey"]
            ?? "SuperSecretJwtSigningKeyWith256BitsMinimumLengthForDevelopmentOnly!";
        var issuer = _configuration["Jwt:Issuer"] ?? "https://api.tahdeera.app";
        var audience = _configuration["Jwt:Audience"] ?? "https://tahdeera.app";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var minutes) ? minutes : 15;
        var expiresAt = _dateTimeProvider.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.NormalizedEmail ?? string.Empty),
            new("display_name", user.DisplayName),
            new("preferred_language", user.PreferredLanguage),
            new("preferred_theme", user.PreferredTheme),
            new("is_platform_admin", user.IsPlatformAdmin.ToString().ToLowerInvariant())
        };

        if (workspaceId.HasValue)
        {
            claims.Add(new Claim("workspace_id", workspaceId.Value.ToString()));
        }

        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt.UtcDateTime,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public byte[] HashToken(string token)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }
}
