using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IGradeRepository : IGenericRepository<Grade>
{
    Task<IReadOnlyList<Grade>> GetActiveGradesAsync(CancellationToken cancellationToken = default);
}
