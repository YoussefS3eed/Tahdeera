using Tahdeera.Domain.Entities;

namespace Tahdeera.Application.Common.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, DateTimeOffset ExpiresAt) GenerateAccessToken(AppUser user, IEnumerable<string>? roles = null, Guid? workspaceId = null);
    string GenerateRefreshToken();
    byte[] HashToken(string token);
}
