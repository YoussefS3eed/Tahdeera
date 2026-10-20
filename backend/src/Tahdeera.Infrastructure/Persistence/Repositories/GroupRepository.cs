using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Infrastructure.Persistence.Repositories;

public class GroupRepository : GenericRepository<Group>, IGroupRepository
{
    public GroupRepository(TahdeeraDbContext context) : base(context)
    {
    }

    public async Task<Group?> GetByIdWithDetailsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(g => g.AcademicYear)
            .Include(g => g.Grade)
            .Include(g => g.Subject)
            .Include(g => g.EnrolledStudents)
            .Include(g => g.Delegations)
                .ThenInclude(d => d.AssistantUser)
            .Include(g => g.Sessions)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
    }

    public async Task<Group?> GetByIdForWorkspaceAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(g => g.EnrolledStudents)
            .FirstOrDefaultAsync(g => g.OwnerWorkspaceId == workspaceId && g.Id == groupId, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        Guid workspaceId,
        Guid academicYearId,
        string name,
        Guid? excludeGroupId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();
        var query = _dbSet.Where(g => g.OwnerWorkspaceId == workspaceId && g.AcademicYearId == academicYearId && g.Name.Trim().ToLower() == normalizedName);
        if (excludeGroupId.HasValue)
        {
            query = query.Where(g => g.Id != excludeGroupId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Group> Items, int TotalCount)> GetPagedGroupsAsync(
        Guid workspaceId,
        Tahdeera.Domain.Enums.GroupStatus? status,
        short? gradeId,
        Guid? academicYearId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(g => g.EnrolledStudents)
            .Where(g => g.OwnerWorkspaceId == workspaceId);

        if (status.HasValue)
        {
            query = query.Where(g => g.Status == status.Value);
        }

        if (gradeId.HasValue)
        {
            query = query.Where(g => g.GradeId == gradeId.Value);
        }

        if (academicYearId.HasValue)
        {
            query = query.Where(g => g.AcademicYearId == academicYearId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(g => g.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
