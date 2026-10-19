using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IAcademicYearRepository : IGenericRepository<AcademicYear>
{
    Task<AcademicYear?> GetCurrentYearAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcademicYear>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
}
