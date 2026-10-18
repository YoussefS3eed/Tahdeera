using Microsoft.AspNetCore.Identity;
using Tahdeera.Application.Common.Interfaces;

namespace Tahdeera.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public IdentityService(UserManager<IdentityUser<Guid>> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(bool Success, Guid UserId, string[] Errors)> CreateUserAsync(
        string username,
        string email,
        string password,
        bool isPlatformAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var identityUser = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid(),
            UserName = username,
            Email = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(identityUser, password);
        if (!result.Succeeded)
        {
            return (false, Guid.Empty, result.Errors.Select(e => e.Description).ToArray());
        }

        if (isPlatformAdmin)
        {
            await _userManager.AddToRoleAsync(identityUser, "PlatformAdmin");
        }

        return (true, identityUser.Id, Array.Empty<string>());
    }

    public async Task<(bool Success, Guid UserId)> CheckPasswordAsync(
        string usernameOrEmail,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(usernameOrEmail)
                ?? await _userManager.FindByNameAsync(usernameOrEmail);

        if (user == null)
        {
            return (false, Guid.Empty);
        }

        var isValid = await _userManager.CheckPasswordAsync(user, password);
        return (isValid, isValid ? user.Id : Guid.Empty);
    }
}
