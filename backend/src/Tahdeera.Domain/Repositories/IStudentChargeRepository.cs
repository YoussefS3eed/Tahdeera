using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IStudentChargeRepository
{
    Task<StudentCharge?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<StudentCharge>> GetOpenChargesByAccountIdAsync(Guid studentAccountId, CancellationToken ct = default);
    Task AddAsync(StudentCharge charge, CancellationToken ct = default);
}
