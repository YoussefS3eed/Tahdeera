namespace Tahdeera.Application.Features.Auth.DTOs;

public record AuthTokenDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken);
