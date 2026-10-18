namespace Tahdeera.Application.Features.Workspaces.DTOs;

public record WorkspaceSubscriptionStatusDto(
    Guid WorkspaceId,
    string? PlanNameAr,
    string? PlanNameEn,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsLockedOut,
    bool IsSuspended,
    string? SuspensionReason,
    string SupportPhone1,
    string SupportPhone2,
    int MaxStudents,
    int MaxStaff,
    object Features);
