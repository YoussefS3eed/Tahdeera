using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Sessions.Commands.CompleteSession;

public sealed class CompleteSessionCommandHandler : IRequestHandler<CompleteSessionCommand, Result<SessionCompletedSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CompleteSessionCommandHandler(IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SessionCompletedSummaryDto>> Handle(CompleteSessionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var session = await _unitOfWork.Sessions.GetByIdWithDetailsAsync(request.SessionId, cancellationToken);

        if (session is null)
        {
            return DomainErrors.Sessions.NotFound;
        }

        if (session.Group.OwnerWorkspaceId != request.WorkspaceId)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        if (session.Status != SessionStatus.Scheduled)
        {
            return DomainErrors.Sessions.AlreadyCompletedScheduleFrozen;
        }

        // Option A: Session cannot be completed before its scheduled start time
        if (now < session.StartsAt)
        {
            return DomainErrors.Sessions.CannotCompleteBeforeStart;
        }

        // 1. Mark session completed
        session.MarkCompleted(now);

        // 2. Fetch active students in the group
        var activeGroupStudents = await _unitOfWork.GroupStudents
            .FindAsync(gs => gs.GroupId == session.GroupId && gs.Status == GroupStudentStatus.Active, cancellationToken);

        var totalActiveRoster = activeGroupStudents.Count;

        // 3. Fetch existing attendance records for this session
        var existingRecords = await _unitOfWork.AttendanceRecords
            .FindAsync(ar => ar.SessionId == session.Id && ar.VoidedAt == null, cancellationToken);

        var existingStudentIds = existingRecords.Select(ar => ar.StudentId).ToHashSet();
        var existingCheckInsCount = existingRecords.Count(ar =>
            ar.Status == AttendanceStatus.Present || ar.Status == AttendanceStatus.Late);

        // 4. Generate system absences for unattended active students
        var unattendedStudents = activeGroupStudents
            .Where(gs => !existingStudentIds.Contains(gs.StudentId))
            .ToList();

        var unattendedStudentIds = unattendedStudents.Select(s => s.StudentId).ToList();

        var wsStudents = await _unitOfWork.WorkspaceStudents
            .FindAsync(ws => ws.WorkspaceId == session.Group.OwnerWorkspaceId && unattendedStudentIds.Contains(ws.StudentId), cancellationToken);
        var contactDict = wsStudents.ToDictionary(ws => ws.StudentId, ws => new { ws.WhatsappOptIn, ws.GuardianPhone });

        var generatedAbsencesCount = 0;
        var stagedOutboxMessagesCount = 0;

        foreach (var unattended in unattendedStudents)
        {
            var absenceRecord = AttendanceRecord.CreateSystemAbsence(
                session.Id,
                unattended.StudentId,
                session.Group.OwnerWorkspaceId,
                now);

            await _unitOfWork.AttendanceRecords.AddAsync(absenceRecord, cancellationToken);
            generatedAbsencesCount++;

            await _unitOfWork.AttendanceHistories.AddAsync(AttendanceHistory.Create(
                attendanceRecordId: absenceRecord.Id,
                oldStatus: null,
                newStatus: AttendanceStatus.Absent,
                oldSource: null,
                newSource: AttendanceSource.System,
                changedByUserId: null,
                changedAt: now,
                reason: "System automated absence on session completion",
                changeKind: AttendanceChangeKind.InitialSystemAbsence), cancellationToken);

            // WhatsApp outbox staging (DEC-07)
            if (contactDict.TryGetValue(unattended.StudentId, out var contact)
                && contact.WhatsappOptIn
                && !string.IsNullOrWhiteSpace(contact.GuardianPhone))
            {
                var idempotencyKey = $"att_{absenceRecord.Id}_Absent";
                var payload = $"{{\"studentId\":\"{unattended.StudentId}\",\"sessionId\":\"{session.Id}\",\"status\":\"Absent\",\"recordedAt\":\"{now:O}\"}}";

                var outboxMessage = MessageOutbox.CreateForAttendance(
                    workspaceId: session.Group.OwnerWorkspaceId,
                    studentId: unattended.StudentId,
                    attendanceRecordId: absenceRecord.Id,
                    recipientPhone: contact.GuardianPhone,
                    messageType: "AbsenceNotification",
                    payload: payload,
                    idempotencyKey: idempotencyKey,
                    languageCode: "ar");

                await _unitOfWork.MessageOutboxes.AddAsync(outboxMessage, cancellationToken);
                stagedOutboxMessagesCount++;
            }
        }

        // 5. Evaluate consecutive absence streak (Option A)
        var studentsSuspendedCount = 0;
        var group = session.Group;

        if (group.ConsecutiveAbsenceThreshold.HasValue && group.AbsenceAction != AbsenceAction.NotifyOnly)
        {
            var completedSessions = await _unitOfWork.Sessions
                .FindAsync(s => s.GroupId == group.Id && (s.Status == SessionStatus.Completed || s.Id == session.Id), cancellationToken);

            completedSessions = completedSessions.OrderByDescending(s => s.StartsAt).ToList();
            var completedSessionIds = completedSessions.Select(s => s.Id).ToHashSet();

            var allAttendanceRecords = await _unitOfWork.AttendanceRecords
                .FindAsync(ar => completedSessionIds.Contains(ar.SessionId) && ar.VoidedAt == null, cancellationToken);

            foreach (var student in activeGroupStudents)
            {
                var streak = 0;
                foreach (var s in completedSessions)
                {
                    AttendanceStatus? status = null;
                    if (s.Id == session.Id)
                    {
                        var rec = existingRecords.FirstOrDefault(r => r.StudentId == student.StudentId);
                        if (rec != null)
                        {
                            status = rec.Status;
                        }
                        else if (unattendedStudentIds.Contains(student.StudentId))
                        {
                            status = AttendanceStatus.Absent;
                        }
                    }
                    else
                    {
                        var rec = allAttendanceRecords.FirstOrDefault(r => r.SessionId == s.Id && r.StudentId == student.StudentId);
                        status = rec?.Status;
                    }

                    if (status == AttendanceStatus.Absent)
                    {
                        streak++;
                    }
                    else
                    {
                        streak = 0;
                        break;
                    }
                }

                if (streak >= group.ConsecutiveAbsenceThreshold.Value)
                {
                    if (group.AbsenceAction == AbsenceAction.MarkSuspended)
                    {
                        student.Suspend(request.ActorUserId);
                        await _unitOfWork.GroupStudentStatusHistories.AddAsync(GroupStudentStatusHistory.Record(
                            student.Id,
                            GroupStudentStatus.Active,
                            GroupStudentStatus.Suspended,
                            now,
                            request.ActorUserId,
                            "Consecutive absence threshold reached"), cancellationToken);

                        studentsSuspendedCount++;
                    }
                    else if (group.AbsenceAction == AbsenceAction.Archive)
                    {
                        student.Withdraw(now, request.ActorUserId);
                        await _unitOfWork.GroupStudentStatusHistories.AddAsync(GroupStudentStatusHistory.Record(
                            student.Id,
                            GroupStudentStatus.Active,
                            GroupStudentStatus.Withdrawn,
                            now,
                            request.ActorUserId,
                            "Consecutive absence threshold reached"), cancellationToken);

                        studentsSuspendedCount++;
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var summary = new SessionCompletedSummaryDto(
            SessionId: session.Id,
            CompletedAt: now,
            TotalActiveRoster: totalActiveRoster,
            ExistingCheckInsCount: existingCheckInsCount,
            GeneratedAbsencesCount: generatedAbsencesCount,
            StagedOutboxMessagesCount: stagedOutboxMessagesCount,
            StudentsSuspendedCount: studentsSuspendedCount);

        return Result.Success(summary);
    }
}
