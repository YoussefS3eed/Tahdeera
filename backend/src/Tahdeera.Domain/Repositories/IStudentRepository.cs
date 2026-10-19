using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IStudentRepository : IGenericRepository<Student>
{
    Task<Student?> GetByCodeAsync(string studentCode, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string studentCode, CancellationToken cancellationToken = default);
    Task<Student?> GetWithAcademicHistoryAndEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default);
}
