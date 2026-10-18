namespace Tahdeera.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        Guid? actorUserId,
        Guid? workspaceId,
        string action,
        string entityType,
        string? entityId,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}
