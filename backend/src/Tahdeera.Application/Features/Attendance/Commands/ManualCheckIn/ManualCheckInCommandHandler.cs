using System.Diagnostics;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Attendance.Commands.ManualCheckIn;

public sealed class ManualCheckInCommandHandler : IRequestHandler<ManualCheckInCommand, Result<AttendanceScanResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public ManualCheckInCommandHandler(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AttendanceScanResultDto>> Handle(
        ManualCheckInCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var now = _dateTimeProvider.UtcNow;

        // 1. Load session with group details
        var session = await _unitOfWork.Sessions
            .GetByIdWithDetailsAsync(request.SessionId, cancellationToken);

        if (session is null)
        {
            return DomainErrors.Sessions.NotFound;
        }

        if (session.Status == SessionStatus.Cancelled)
        {
            return DomainErrors.Attendance.SessionNotActive;
        }

        // 2. Verify workspace isolation
        var workspaceId = session.Group.OwnerWorkspaceId;
        if (_currentUserService.ActiveWorkspaceId.HasValue &&
            _currentUserService.ActiveWorkspaceId.Value != workspaceId)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        // 3. Verify student enrollment in group
        var groupStudent = await _unitOfWork.GroupStudents
            .FirstOrDefaultAsync(gs => gs.GroupId == session.GroupId && gs.StudentId == request.StudentId, cancellationToken);

        if (groupStudent is null)
        {
            return DomainErrors.Groups.StudentNotEnrolled;
        }

        if (groupStudent.Status == GroupStudentStatus.Suspended)
        {
            return DomainErrors.Groups.StudentSuspendedRequiresReinstatement;
        }

        if (groupStudent.Status == GroupStudentStatus.Withdrawn &&
            string.IsNullOrWhiteSpace(request.RetroactiveReason))
        {
            return DomainErrors.Attendance.RetroactiveReasonRequired;
        }

        // 4. Verify student active in workspace
        var workspaceStudent = await _unitOfWork.WorkspaceStudents
            .FirstOrDefaultAsync(ws => ws.WorkspaceId == workspaceId && ws.StudentId == request.StudentId, cancellationToken);

        if (workspaceStudent is null || workspaceStudent.Status == WorkspaceStudentStatus.Blocked)
        {
            return DomainErrors.Attendance.StudentBlockedInWorkspace;
        }

        // 5. Verify academic grade compatibility
        var academicHistory = await _unitOfWork.StudentAcademicHistories
            .FirstOrDefaultAsync(h => h.StudentId == request.StudentId && h.AcademicYearId == session.Group.AcademicYearId, cancellationToken);

        if (academicHistory != null && academicHistory.GradeId != session.Group.GradeId)
        {
            return DomainErrors.Attendance.GradeMismatch;
        }

        // 6. Check existing attendance record (idempotency)
        var existingRecord = await _unitOfWork.AttendanceRecords
            .FirstOrDefaultAsync(r => r.SessionId == request.SessionId && r.StudentId == request.StudentId && r.VoidedAt == null, cancellationToken);

        var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId, cancellationToken);
        var studentName = student?.FullName ?? string.Empty;
        var studentCode = student?.StudentCode ?? string.Empty;

        if (existingRecord != null)
        {
            stopwatch.Stop();
            return Result.Success(new AttendanceScanResultDto(
                existingRecord.Id,
                session.Id,
                request.StudentId,
                studentName,
                studentCode,
                existingRecord.Status.ToString(),
                existingRecord.CheckInAt ?? existingRecord.RecordedAt,
                existingRecord.IsGuestMakeup,
                existingRecord.HomeGroupId,
                NotificationStaged: false,
                stopwatch.ElapsedMilliseconds));
        }

        // 7. Create attendance record
        var record = AttendanceRecord.CreateCheckIn(
            session,
            request.StudentId,
            workspaceId,
            now,
            _currentUserService.UserId);

        if (!string.IsNullOrWhiteSpace(request.RetroactiveReason))
        {
            record.RetroactiveReason = request.RetroactiveReason.Trim();
        }

        await _unitOfWork.AttendanceRecords.AddAsync(record, cancellationToken);

        // 8. Append history audit entry
        var reason = !string.IsNullOrWhiteSpace(request.RetroactiveReason)
            ? $"Manual roster check-in (Retroactive: {request.RetroactiveReason.Trim()})"
            : "Manual roster check-in";

        await _unitOfWork.AttendanceHistories.AddAsync(AttendanceHistory.Create(
            attendanceRecordId: record.Id,
            oldStatus: null,
            newStatus: record.Status,
            oldSource: null,
            newSource: record.Source,
            changedByUserId: _currentUserService.UserId,
            changedAt: now,
            reason: reason,
            changeKind: AttendanceChangeKind.InitialUserEntry), cancellationToken);

        // 9. Transactional WhatsApp Outbox Staging
        var notificationStaged = false;
        if (workspaceStudent.WhatsappOptIn && !string.IsNullOrWhiteSpace(workspaceStudent.GuardianPhone))
        {
            var idempotencyKey = $"att_{record.Id}_{record.Status}";
            var payload = $"{{\"studentId\":\"{request.StudentId}\",\"sessionId\":\"{session.Id}\",\"status\":\"{record.Status}\",\"checkInAt\":\"{now:O}\"}}";

            var outboxMessage = MessageOutbox.CreateForAttendance(
                workspaceId: workspaceId,
                studentId: request.StudentId,
                attendanceRecordId: record.Id,
                recipientPhone: workspaceStudent.GuardianPhone,
                messageType: "AttendanceCheckIn",
                payload: payload,
                idempotencyKey: idempotencyKey,
                languageCode: "ar");

            await _unitOfWork.MessageOutboxes.AddAsync(outboxMessage, cancellationToken);
            notificationStaged = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        stopwatch.Stop();

        return Result.Success(new AttendanceScanResultDto(
            record.Id,
            session.Id,
            request.StudentId,
            studentName,
            studentCode,
            record.Status.ToString(),
            now,
            record.IsGuestMakeup,
            record.HomeGroupId,
            notificationStaged,
            stopwatch.ElapsedMilliseconds));
    }
}
