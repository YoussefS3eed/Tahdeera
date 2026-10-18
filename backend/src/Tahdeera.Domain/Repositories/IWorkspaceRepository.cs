using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IWorkspaceRepository : IGenericRepository<Workspace>
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Workspace?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
