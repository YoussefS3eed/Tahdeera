namespace Tahdeera.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(bool Success, Guid UserId, string[] Errors)> CreateUserAsync(
        string username,
        string email,
        string password,
        bool isPlatformAdmin = false,
        CancellationToken cancellationToken = default);

    Task<(bool Success, Guid UserId)> CheckPasswordAsync(
        string usernameOrEmail,
        string password,
        CancellationToken cancellationToken = default);
}
