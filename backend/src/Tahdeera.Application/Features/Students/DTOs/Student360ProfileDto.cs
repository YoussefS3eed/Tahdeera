namespace Tahdeera.Application.Features.Students.DTOs;

public record Student360ProfileDto(
    Guid StudentId,
    string StudentCode,
    string FullName,
    bool IsActive,
    WorkspaceEnrollmentDetailsDto WorkspaceEnrollment,
    IReadOnlyList<AcademicHistoryRecordDto> AcademicHistory,
    IReadOnlyList<CredentialSummaryDto> Credentials,
    IReadOnlyList<StudentGroupMembershipDto> Groups,
    AttendanceSummaryDto AttendanceSummary,
    IReadOnlyList<RecentAttendanceSessionDto> RecentAttendance,
    FinancialSummaryDto FinancialSummary,
    IReadOnlyList<StudentAuditLogItemDto> RecentAuditLogs
);
