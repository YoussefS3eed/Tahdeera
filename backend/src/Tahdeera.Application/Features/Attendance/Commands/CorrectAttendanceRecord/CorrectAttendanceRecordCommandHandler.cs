using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Attendance.Commands.CorrectAttendanceRecord;

public sealed class CorrectAttendanceRecordCommandHandler : IRequestHandler<CorrectAttendanceRecordCommand, Result<AttendanceCorrectedDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAttendanceReconciliationService _reconciliationService;

    public CorrectAttendanceRecordCommandHandler(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IAttendanceReconciliationService reconciliationService)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _reconciliationService = reconciliationService;
    }

    public async Task<Result<AttendanceCorrectedDto>> Handle(
        CorrectAttendanceRecordCommand request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return DomainErrors.Attendance.CorrectionReasonRequired;
        }

        // 1. Locate attendance record
        var record = await _unitOfWork.AttendanceRecords
            .FirstOrDefaultAsync(r => r.Id == request.AttendanceRecordId, cancellationToken);

        if (record is null)
        {
            return DomainErrors.Attendance.NotFound;
        }

        if (record.VoidedAt.HasValue)
        {
            return DomainErrors.Attendance.AlreadyVoided;
        }

        // 2. Load session with group details
        var session = await _unitOfWork.Sessions
            .GetByIdWithDetailsAsync(record.SessionId, cancellationToken);

        if (session is null)
        {
            return DomainErrors.Sessions.NotFound;
        }

        // 3. Verify workspace isolation
        var workspaceId = session.Group.OwnerWorkspaceId;
        if (_currentUserService.ActiveWorkspaceId.HasValue &&
            _currentUserService.ActiveWorkspaceId.Value != workspaceId)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        // 4. 7-Day Window Governance (FR-14, FR-25, SC-05)
        var sessionCompletionPoint = session.AutoAbsenceProcessedAt ?? session.EndsAt;
        if (now > sessionCompletionPoint.AddDays(7) && !request.IsOwner)
        {
            return DomainErrors.Attendance.CorrectionWindowExpired;
        }

        // 5. Withdrawn student retroactive reason check (FR-16)
        var groupStudent = await _unitOfWork.GroupStudents
            .FirstOrDefaultAsync(gs => gs.GroupId == session.GroupId && gs.StudentId == record.StudentId, cancellationToken);

        if (groupStudent != null && groupStudent.Status == GroupStudentStatus.Withdrawn &&
            string.IsNullOrWhiteSpace(request.Reason))
        {
            return DomainErrors.Attendance.RetroactiveReasonRequired;
        }

        var oldStatus = record.Status;
        var actorUserId = _currentUserService.UserId ?? Guid.Empty;

        // 6. Mutate domain entity
        record.ManualOverride(request.NewStatus, actorUserId, now, request.Reason);

        // 7. Append history audit entry
        await _unitOfWork.AttendanceHistories.AddAsync(AttendanceHistory.Create(
            attendanceRecordId: record.Id,
            oldStatus: oldStatus,
            newStatus: record.Status,
            oldSource: record.Source,
            newSource: AttendanceSource.User,
            changedByUserId: actorUserId,
            changedAt: now,
            reason: request.Reason,
            changeKind: AttendanceChangeKind.ManualCorrection), cancellationToken);

        // 8. Trigger streak reconciliation if status involves Absence
        var suspensionReconciled = false;
        var consecutiveAbsences = 0;

        if (oldStatus == AttendanceStatus.Absent || request.NewStatus == AttendanceStatus.Absent || request.NewStatus == AttendanceStatus.Excused)
        {
            await _reconciliationService.ReconcileStudentStreakAsync(session.GroupId, record.StudentId, cancellationToken);
            consecutiveAbsences = await _reconciliationService.CalculateConsecutiveAbsenceStreakAsync(session.GroupId, record.StudentId, cancellationToken);
            suspensionReconciled = true;
        }

        // 9. Transactional WhatsApp Outbox Staging
        var notificationStaged = false;
        var workspaceStudent = await _unitOfWork.WorkspaceStudents
            .FirstOrDefaultAsync(ws => ws.WorkspaceId == workspaceId && ws.StudentId == record.StudentId, cancellationToken);

        if (workspaceStudent != null && workspaceStudent.WhatsappOptIn && !string.IsNullOrWhiteSpace(workspaceStudent.GuardianPhone))
        {
            var idempotencyKey = $"att_{record.Id}_{request.NewStatus}_{now.ToUnixTimeSeconds()}";
            var payload = $"{{\"studentId\":\"{record.StudentId}\",\"sessionId\":\"{session.Id}\",\"oldStatus\":\"{oldStatus}\",\"newStatus\":\"{request.NewStatus}\",\"reason\":\"{request.Reason}\"}}";

            var outboxMessage = MessageOutbox.CreateForAttendance(
                workspaceId: workspaceId,
                studentId: record.StudentId,
                attendanceRecordId: record.Id,
                recipientPhone: workspaceStudent.GuardianPhone,
                messageType: "AttendanceCorrection",
                payload: payload,
                idempotencyKey: idempotencyKey,
                languageCode: "ar");

            await _unitOfWork.MessageOutboxes.AddAsync(outboxMessage, cancellationToken);
            notificationStaged = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AttendanceCorrectedDto(
            record.Id,
            record.StudentId,
            oldStatus.ToString(),
            record.Status.ToString(),
            record.AutoEvaluationLocked,
            now,
            actorUserId,
            consecutiveAbsences,
            suspensionReconciled,
            notificationStaged));
    }
}
