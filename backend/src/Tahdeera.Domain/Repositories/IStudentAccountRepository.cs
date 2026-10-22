using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IStudentAccountRepository
{
    Task<StudentAccount?> GetByWorkspaceStudentIdAsync(Guid workspaceStudentId, CancellationToken ct = default);
    Task<StudentAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(StudentAccount account, CancellationToken ct = default);
}
