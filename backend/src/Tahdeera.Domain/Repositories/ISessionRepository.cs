using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Repositories;

public interface ISessionRepository : IGenericRepository<Session>
{
    Task<Session?> GetByIdWithDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<Session?> GetByIdForWorkspaceAsync(Guid workspaceId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Session>> GetSessionsForGroupAsync(
        Guid groupId,
        SessionStatus? status,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Session>> GetCompletedSessionsInPeriodAsync(
        Guid groupId,
        DateTimeOffset? since,
        CancellationToken cancellationToken = default);
}
