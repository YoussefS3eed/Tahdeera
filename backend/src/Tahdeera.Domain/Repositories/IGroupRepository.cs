using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.Repositories;

public interface IGroupRepository : IGenericRepository<Group>
{
    Task<Group?> GetByIdWithDetailsAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<Group?> GetByIdForWorkspaceAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(Guid workspaceId, Guid academicYearId, string name, Guid? excludeGroupId = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Group> Items, int TotalCount)> GetPagedGroupsAsync(
        Guid workspaceId,
        Tahdeera.Domain.Enums.GroupStatus? status,
        short? gradeId,
        Guid? academicYearId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
