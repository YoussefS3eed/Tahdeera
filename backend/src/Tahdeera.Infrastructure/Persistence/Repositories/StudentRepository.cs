using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class StudentRepository : GenericRepository<Student>, IStudentRepository
{
    public StudentRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<Student?> GetByCodeAsync(string studentCode, CancellationToken cancellationToken = default)
    {
        var code = studentCode.Trim().ToUpperInvariant();
        return await _dbSet
            .Include(s => s.AcademicHistories)
                .ThenInclude(ah => ah.Grade)
            .Include(s => s.AcademicHistories)
                .ThenInclude(ah => ah.AcademicYear)
            .Include(s => s.WorkspaceEnrollments)
            .FirstOrDefaultAsync(s => s.StudentCode == code, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string studentCode, CancellationToken cancellationToken = default)
    {
        var code = studentCode.Trim().ToUpperInvariant();
        return await _dbSet.AnyAsync(s => s.StudentCode == code, cancellationToken);
    }

    public async Task<Student?> GetWithAcademicHistoryAndEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.AcademicHistories)
                .ThenInclude(ah => ah.Grade)
            .Include(s => s.AcademicHistories)
                .ThenInclude(ah => ah.AcademicYear)
            .Include(s => s.WorkspaceEnrollments)
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);
    }
}
