using Mapster;
using MediatR;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Students.Queries.GetStudent360Profile;

public class GetStudent360ProfileQueryHandler : IRequestHandler<GetStudent360ProfileQuery, Result<Student360ProfileDto>>
{
    private readonly IWorkspaceStudentRepository _workspaceStudentRepository;
    private readonly IStudentCredentialRepository _credentialRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public GetStudent360ProfileQueryHandler(
        IWorkspaceStudentRepository workspaceStudentRepository,
        IStudentCredentialRepository credentialRepository,
        IAuditLogRepository auditLogRepository)
    {
        _workspaceStudentRepository = workspaceStudentRepository;
        _credentialRepository = credentialRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<Student360ProfileDto>> Handle(GetStudent360ProfileQuery request, CancellationToken cancellationToken)
    {
        var wsStudent = await _workspaceStudentRepository.GetWithStudentAndAcademicHistoryAsync(
            request.WorkspaceId,
            request.StudentId,
            cancellationToken);

        if (wsStudent is null)
        {
            return Error.NotFound("STUDENT_NOT_FOUND_IN_WORKSPACE", "الطالب غير مسجل في مركز الدروس الحالي.");
        }

        // 1. Workspace-scoped enrollment details
        var enrollmentDetails = new WorkspaceEnrollmentDetailsDto(
            Status: wsStudent.Status.ToString(),
            GuardianName: wsStudent.GuardianName,
            GuardianPhone: wsStudent.GuardianPhone,
            StudentPhone: wsStudent.StudentPhone,
            WhatsappOptIn: wsStudent.WhatsappOptIn,
            SeatNumber: wsStudent.SeatNumber,
            LocalNotes: wsStudent.LocalNotes,
            EnrolledAt: wsStudent.EnrolledAt
        );

        // 2. Academic progression history across all years
        var academicHistory = wsStudent.Student.AcademicHistories
            .OrderByDescending(ah => ah.RecordedAt)
            .Select(ah => new AcademicHistoryRecordDto(
                AcademicYearId: ah.AcademicYearId,
                AcademicYearName: ah.AcademicYear?.Name ?? string.Empty,
                GradeId: ah.GradeId,
                GradeNameAr: ah.Grade?.NameAr ?? string.Empty,
                GradeNameEn: ah.Grade?.NameEn ?? string.Empty,
                IsCurrentYear: ah.AcademicYear?.IsCurrent ?? false,
                RecordedAt: ah.RecordedAt
            ))
            .ToList();

        // 3. Workspace-scoped credentials
        var credentialsList = await _credentialRepository.GetCredentialsByStudentAsync(
            request.WorkspaceId,
            request.StudentId,
            includeUserDetails: false,
            cancellationToken: cancellationToken);

        var credentials = credentialsList.Adapt<List<CredentialSummaryDto>>();

        // 4. Workspace-scoped audit logs
        var studentIdStr = request.StudentId.ToString();
        var wsStudentIdStr = wsStudent.Id.ToString();
        var auditLogsList = await _auditLogRepository.GetRecentLogsForEntitiesAsync(
            request.WorkspaceId,
            new[] { studentIdStr, wsStudentIdStr },
            10,
            cancellationToken);

        var auditLogs = auditLogsList.Adapt<List<StudentAuditLogItemDto>>();

        // 5. Future modules placeholders (zero defaults, resilient)
        var groups = Array.Empty<StudentGroupMembershipDto>();
        var attendanceSummary = new AttendanceSummaryDto(0, 0, 0, 0, 0.0);
        var recentAttendance = Array.Empty<RecentAttendanceSessionDto>();
        var financialSummary = new FinancialSummaryDto(0m, 0m, 0m);

        var profileDto = new Student360ProfileDto(
            StudentId: wsStudent.StudentId,
            StudentCode: wsStudent.Student.StudentCode,
            FullName: wsStudent.Student.FullName,
            IsActive: wsStudent.Student.IsActive,
            WorkspaceEnrollment: enrollmentDetails,
            AcademicHistory: academicHistory,
            Credentials: credentials,
            Groups: groups,
            AttendanceSummary: attendanceSummary,
            RecentAttendance: recentAttendance,
            FinancialSummary: financialSummary,
            RecentAuditLogs: auditLogs
        );

        return Result.Success(profileDto);
    }
}
