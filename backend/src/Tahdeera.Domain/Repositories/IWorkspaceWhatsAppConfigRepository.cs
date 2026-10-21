using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IWorkspaceWhatsAppConfigRepository
{
    Task<WorkspaceWhatsAppConfig?> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
    Task AddAsync(WorkspaceWhatsAppConfig config, CancellationToken ct = default);
}
