using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Repositories;

public interface IWorkspaceStudentRepository : IGenericRepository<WorkspaceStudent>
{
    Task<WorkspaceStudent?> GetByWorkspaceAndStudentIdAsync(Guid workspaceId, Guid studentId, CancellationToken cancellationToken = default);
    Task<WorkspaceStudent?> GetWithStudentAndAcademicHistoryAsync(Guid workspaceId, Guid studentId, CancellationToken cancellationToken = default);
    Task<bool> IsStudentEnrolledAsync(Guid workspaceId, Guid studentId, CancellationToken cancellationToken = default);
    Task<bool> GuardianPhoneExistsInWorkspaceAsync(Guid workspaceId, string? guardianPhone, Guid? excludeStudentId = null, CancellationToken cancellationToken = default);
    Task<int> GetActiveStudentCountAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<WorkspaceStudent> Items, int TotalCount)> GetWorkspaceRosterAsync(
        Guid workspaceId,
        string? searchTerm,
        short? gradeId,
        Guid? academicYearId,
        WorkspaceStudentStatus? status,
        int pageNumber,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default);
}
