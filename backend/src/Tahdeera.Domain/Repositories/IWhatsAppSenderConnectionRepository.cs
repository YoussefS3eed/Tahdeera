using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IWhatsAppSenderConnectionRepository
{
    Task<WhatsAppSenderConnection?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WhatsAppSenderConnection?> GetActiveForWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppSenderConnection>> GetHistoryForWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task AddAsync(WhatsAppSenderConnection connection, CancellationToken ct = default);
}
