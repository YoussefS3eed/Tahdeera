using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Repositories;

public interface IStudentCredentialRepository : IGenericRepository<StudentCredential>
{
    Task<StudentCredential?> GetActiveByTokenHashAsync(byte[] tokenHash, CancellationToken cancellationToken = default);
    Task<bool> TokenHashExistsAsync(byte[] tokenHash, CancellationToken cancellationToken = default);
    Task<StudentCredential?> GetActiveCardForStudentAsync(Guid workspaceId, Guid studentId, CredentialType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentCredential>> GetCredentialsByStudentAsync(Guid workspaceId, Guid studentId, bool includeUserDetails = false, CancellationToken cancellationToken = default);
    Task<bool> HasActiveCredentialAsync(Guid workspaceId, Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetStudentIdsWithActiveCardsAsync(Guid workspaceId, IEnumerable<Guid> studentIds, CancellationToken cancellationToken = default);
}
