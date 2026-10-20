using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Attendance.Commands.RegisterGuestMakeupAttendance;

public sealed class RegisterGuestMakeupAttendanceCommandHandler : IRequestHandler<RegisterGuestMakeupAttendanceCommand, Result<GuestMakeupResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAttendanceReconciliationService _reconciliationService;

    public RegisterGuestMakeupAttendanceCommandHandler(
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

    public async Task<Result<GuestMakeupResultDto>> Handle(
        RegisterGuestMakeupAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        // 1. Load visited session with group
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

        // 3. Verify student actively enrolled in home group
        var homeGroup = await _unitOfWork.Groups.GetByIdAsync(request.HomeGroupId, cancellationToken);
        if (homeGroup is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        var homeGroupStudent = await _unitOfWork.GroupStudents
            .FirstOrDefaultAsync(gs => gs.GroupId == request.HomeGroupId && gs.StudentId == request.StudentId, cancellationToken);

        if (homeGroupStudent is null || homeGroupStudent.Status != GroupStudentStatus.Active)
        {
            return DomainErrors.Attendance.StudentNotEnrolledInHomeGroup;
        }

        // 4. Verify academic grade compatibility
        var academicHistory = await _unitOfWork.StudentAcademicHistories
            .FirstOrDefaultAsync(h => h.StudentId == request.StudentId && h.AcademicYearId == session.Group.AcademicYearId, cancellationToken);

        if (academicHistory != null && academicHistory.GradeId != session.Group.GradeId)
        {
            return DomainErrors.Attendance.GradeMismatch;
        }

        // 5. Verify student in workspace
        var workspaceStudent = await _unitOfWork.WorkspaceStudents
            .FirstOrDefaultAsync(ws => ws.WorkspaceId == workspaceId && ws.StudentId == request.StudentId, cancellationToken);

        if (workspaceStudent is null || workspaceStudent.Status == WorkspaceStudentStatus.Blocked)
        {
            return DomainErrors.Attendance.StudentBlockedInWorkspace;
        }

        // 6. Cross-Group Excusal Processing
        Guid? excusedHomeSessionId = null;
        var homeSessionExcused = false;

        if (request.HomeSessionId.HasValue)
        {
            var targetHomeRecord = await _unitOfWork.AttendanceRecords
                .FirstOrDefaultAsync(r => r.SessionId == request.HomeSessionId.Value && r.StudentId == request.StudentId && r.VoidedAt == null, cancellationToken);

            if (targetHomeRecord is null || targetHomeRecord.Status != AttendanceStatus.Absent)
            {
                return DomainErrors.Attendance.HomeSessionNotEligibleForExcusal;
            }

            var oldStatus = targetHomeRecord.Status;
            targetHomeRecord.Status = AttendanceStatus.Excused;
            targetHomeRecord.RetroactiveReason = $"Covered by make-up attendance in {session.Group.Name}";
            targetHomeRecord.LastModifiedAt = now;
            targetHomeRecord.LastModifiedByUserId = _currentUserService.UserId;

            await _unitOfWork.AttendanceHistories.AddAsync(AttendanceHistory.Create(
                attendanceRecordId: targetHomeRecord.Id,
                oldStatus: oldStatus,
                newStatus: AttendanceStatus.Excused,
                oldSource: targetHomeRecord.Source,
                newSource: AttendanceSource.User,
                changedByUserId: _currentUserService.UserId,
                changedAt: now,
                reason: $"Covered by make-up attendance in {session.Group.Name}",
                changeKind: AttendanceChangeKind.ManualCorrection), cancellationToken);

            await _reconciliationService.ReconcileStudentStreakAsync(request.HomeGroupId, request.StudentId, cancellationToken);
            excusedHomeSessionId = request.HomeSessionId.Value;
            homeSessionExcused = true;
        }
        else
        {
            // Auto-selection of most recent unexcused Absent session in home group
            var homeSessions = await _unitOfWork.Sessions
                .FindAsync(s => s.GroupId == request.HomeGroupId && s.Status == SessionStatus.Completed, cancellationToken);

            var homeSessionIds = homeSessions.OrderByDescending(s => s.StartsAt).Select(s => s.Id).ToList();

            if (homeSessionIds.Count > 0)
            {
                var candidateRecords = await _unitOfWork.AttendanceRecords
                    .FindAsync(r => homeSessionIds.Contains(r.SessionId) && r.StudentId == request.StudentId && r.Status == AttendanceStatus.Absent && r.VoidedAt == null, cancellationToken);

                // Pick the candidate belonging to the latest session
                var latestAbsentRecord = homeSessionIds
                    .Select(sId => candidateRecords.FirstOrDefault(r => r.SessionId == sId))
                    .FirstOrDefault(r => r != null);

                if (latestAbsentRecord != null)
                {
                    var oldStatus = latestAbsentRecord.Status;
                    latestAbsentRecord.Status = AttendanceStatus.Excused;
                    latestAbsentRecord.RetroactiveReason = $"Covered by make-up attendance in {session.Group.Name}";
                    latestAbsentRecord.LastModifiedAt = now;
                    latestAbsentRecord.LastModifiedByUserId = _currentUserService.UserId;

                    await _unitOfWork.AttendanceHistories.AddAsync(AttendanceHistory.Create(
                        attendanceRecordId: latestAbsentRecord.Id,
                        oldStatus: oldStatus,
                        newStatus: AttendanceStatus.Excused,
                        oldSource: latestAbsentRecord.Source,
                        newSource: AttendanceSource.User,
                        changedByUserId: _currentUserService.UserId,
                        changedAt: now,
                        reason: $"Covered by make-up attendance in {session.Group.Name}",
                        changeKind: AttendanceChangeKind.ManualCorrection), cancellationToken);

                    await _reconciliationService.ReconcileStudentStreakAsync(request.HomeGroupId, request.StudentId, cancellationToken);
                    excusedHomeSessionId = latestAbsentRecord.SessionId;
                    homeSessionExcused = true;
                }
            }
        }

        // 7. Register guest make-up record
        var guestRecord = AttendanceRecord.RegisterMakeup(
            session,
            request.StudentId,
            workspaceId,
            request.HomeGroupId,
            request.Reason,
            now,
            _currentUserService.UserId);

        await _unitOfWork.AttendanceRecords.AddAsync(guestRecord, cancellationToken);

        // 8. Append history audit entry
        await _unitOfWork.AttendanceHistories.AddAsync(AttendanceHistory.Create(
            attendanceRecordId: guestRecord.Id,
            oldStatus: null,
            newStatus: guestRecord.Status,
            oldSource: null,
            newSource: guestRecord.Source,
            changedByUserId: _currentUserService.UserId,
            changedAt: now,
            reason: request.Reason ?? $"Make-up guest attendance from {homeGroup.Name}",
            changeKind: AttendanceChangeKind.InitialUserEntry), cancellationToken);

        // 9. Transactional WhatsApp Outbox Staging
        var notificationStaged = false;
        if (workspaceStudent.WhatsappOptIn && !string.IsNullOrWhiteSpace(workspaceStudent.GuardianPhone))
        {
            var idempotencyKey = $"att_{guestRecord.Id}_{guestRecord.Status}";
            var payload = $"{{\"studentId\":\"{request.StudentId}\",\"sessionId\":\"{session.Id}\",\"homeGroupId\":\"{request.HomeGroupId}\",\"status\":\"{guestRecord.Status}\",\"isGuest\":true}}";

            var outboxMessage = MessageOutbox.CreateForAttendance(
                workspaceId: workspaceId,
                studentId: request.StudentId,
                attendanceRecordId: guestRecord.Id,
                recipientPhone: workspaceStudent.GuardianPhone,
                messageType: "AttendanceCheckIn",
                payload: payload,
                idempotencyKey: idempotencyKey,
                languageCode: "ar");

            await _unitOfWork.MessageOutboxes.AddAsync(outboxMessage, cancellationToken);
            notificationStaged = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId, cancellationToken);
        var studentName = student?.FullName ?? string.Empty;

        return Result.Success(new GuestMakeupResultDto(
            guestRecord.Id,
            session.Id,
            request.StudentId,
            studentName,
            guestRecord.Status.ToString(),
            now,
            IsGuestMakeup: true,
            request.HomeGroupId,
            homeGroup.Name,
            homeSessionExcused,
            excusedHomeSessionId,
            BillingStatus: "CoveredByHomeSubscription",
            GuestInvoicesCreated: 0,
            notificationStaged));
    }
}
