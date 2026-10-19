using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class WorkspaceStudentRepository : GenericRepository<WorkspaceStudent>, IWorkspaceStudentRepository
{
    public WorkspaceStudentRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<WorkspaceStudent?> GetByWorkspaceAndStudentIdAsync(
        Guid workspaceId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(ws =>
            ws.WorkspaceId == workspaceId && ws.StudentId == studentId, cancellationToken);
    }

    public async Task<WorkspaceStudent?> GetWithStudentAndAcademicHistoryAsync(
        Guid workspaceId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(ws => ws.Student)
                .ThenInclude(s => s.AcademicHistories)
                    .ThenInclude(ah => ah.Grade)
            .Include(ws => ws.Student)
                .ThenInclude(s => s.AcademicHistories)
                    .ThenInclude(ah => ah.AcademicYear)
            .FirstOrDefaultAsync(ws => ws.WorkspaceId == workspaceId && ws.StudentId == studentId, cancellationToken);
    }

    public async Task<bool> IsStudentEnrolledAsync(
        Guid workspaceId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(ws =>
            ws.WorkspaceId == workspaceId && ws.StudentId == studentId, cancellationToken);
    }

    public async Task<bool> GuardianPhoneExistsInWorkspaceAsync(
        Guid workspaceId,
        string? guardianPhone,
        Guid? excludeStudentId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(guardianPhone))
        {
            return false;
        }

        var query = _dbSet.Where(ws => ws.WorkspaceId == workspaceId && ws.GuardianPhone == guardianPhone);
        if (excludeStudentId.HasValue)
        {
            query = query.Where(ws => ws.StudentId != excludeStudentId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> GetActiveStudentCountAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(ws =>
            ws.WorkspaceId == workspaceId && ws.Status == WorkspaceStudentStatus.Active, cancellationToken);
    }

    public async Task<(IReadOnlyList<WorkspaceStudent> Items, int TotalCount)> GetWorkspaceRosterAsync(
        Guid workspaceId,
        string? searchTerm,
        short? gradeId,
        Guid? academicYearId,
        WorkspaceStudentStatus? status,
        int pageNumber,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(ws => ws.Student)
                .ThenInclude(s => s.AcademicHistories)
                    .ThenInclude(ah => ah.Grade)
            .Where(ws => ws.WorkspaceId == workspaceId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(ws =>
                ws.Student.FullName.Contains(term) ||
                ws.Student.StudentCode.Contains(term) ||
                (ws.GuardianPhone != null && ws.GuardianPhone.Contains(term)) ||
                (ws.StudentPhone != null && ws.StudentPhone.Contains(term)) ||
                (ws.SeatNumber != null && ws.SeatNumber.Contains(term)));
        }

        if (gradeId.HasValue)
        {
            var targetGrade = gradeId.Value;
            query = query.Where(ws => ws.Student.AcademicHistories.Any(ah =>
                (!academicYearId.HasValue || ah.AcademicYearId == academicYearId.Value) &&
                ah.GradeId == targetGrade));
        }

        if (status.HasValue)
        {
            query = query.Where(ws => ws.Status == status.Value);
        }

        var isAsc = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => isAsc ? query.OrderBy(ws => ws.Student.FullName) : query.OrderByDescending(ws => ws.Student.FullName),
            "code" => isAsc ? query.OrderBy(ws => ws.Student.StudentCode) : query.OrderByDescending(ws => ws.Student.StudentCode),
            _ => isAsc ? query.OrderBy(ws => ws.EnrolledAt) : query.OrderByDescending(ws => ws.EnrolledAt)
        };

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
