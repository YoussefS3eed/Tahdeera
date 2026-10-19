using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Common.Interfaces;

public record AbsenceCountInfo(
    Guid StudentId,
    int ConsecutiveAbsenceCount,
    DateTimeOffset? LastAttendedAt,
    bool ThresholdExceeded,
    AbsenceAction RecommendedAction);

public record ReconciliationResult(
    Guid GroupId,
    IReadOnlyList<AbsenceCountInfo> StudentAbsences,
    int SanctionsAppliedCount);

public interface IAttendanceReconciliationService
{
    Task<ReconciliationResult> ReconcileStudentAbsencesAsync(
        Guid workspaceId,
        Guid groupId,
        Guid studentId,
        CancellationToken cancellationToken = default);
}
