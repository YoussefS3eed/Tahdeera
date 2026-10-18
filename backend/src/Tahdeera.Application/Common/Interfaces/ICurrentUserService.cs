namespace Tahdeera.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsPlatformAdmin { get; }
    Guid? ActiveWorkspaceId { get; }
}
