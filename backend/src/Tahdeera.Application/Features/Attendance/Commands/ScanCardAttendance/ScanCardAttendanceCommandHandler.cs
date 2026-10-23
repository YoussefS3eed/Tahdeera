using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Attendance.Commands.ScanCardAttendance;

public sealed class ScanCardAttendanceCommandHandler : IRequestHandler<ScanCardAttendanceCommand, Result<AttendanceScanResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public ScanCardAttendanceCommandHandler(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AttendanceScanResultDto>> Handle(
        ScanCardAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var now = _dateTimeProvider.UtcNow;

        // 1. Hash credential token
        var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(request.RawToken));

        // 2. Locate credential
        var credential = await _unitOfWork.StudentCredentials
            .FirstOrDefaultAsync(c => c.TokenHash == tokenHash, cancellationToken);

        if (credential is null)
        {
            return DomainErrors.Attendance.CredentialNotFound;
        }

        if (credential.RevokedAt.HasValue || (credential.ExpiresAt.HasValue && credential.ExpiresAt.Value <= now))
        {
            return DomainErrors.Attendance.CredentialRevoked;
        }

        // 3. Load session and validate status
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

        // 4. Verify workspace isolation
        var workspaceId = session.Group.OwnerWorkspaceId;
        if (_currentUserService.ActiveWorkspaceId.HasValue &&
            _currentUserService.ActiveWorkspaceId.Value != workspaceId)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        // 5. Verify student active in workspace
        var workspaceStudent = await _unitOfWork.WorkspaceStudents
            .FirstOrDefaultAsync(ws => ws.WorkspaceId == workspaceId && ws.StudentId == credential.StudentId, cancellationToken);

        if (workspaceStudent is null || workspaceStudent.Status == WorkspaceStudentStatus.Blocked)
        {
            return DomainErrors.Attendance.StudentBlockedInWorkspace;
        }

        // 6. Verify academic grade compatibility
        var academicHistory = await _unitOfWork.StudentAcademicHistories
            .FirstOrDefaultAsync(h => h.StudentId == credential.StudentId && h.AcademicYearId == session.Group.AcademicYearId, cancellationToken);

        if (academicHistory != null && academicHistory.GradeId != session.Group.GradeId)
        {
            return DomainErrors.Attendance.GradeMismatch;
        }

        // 7. Check double-tap idempotency
        var existingRecord = await _unitOfWork.AttendanceRecords
            .FirstOrDefaultAsync(r => r.SessionId == request.SessionId && r.StudentId == credential.StudentId && r.VoidedAt == null, cancellationToken);

        var student = await _unitOfWork.Students.GetByIdAsync(credential.StudentId, cancellationToken);
        var studentName = student?.FullName ?? string.Empty;
        var studentCode = student?.StudentCode ?? string.Empty;

        if (existingRecord != null)
        {
            stopwatch.Stop();
            return Result.Success(new AttendanceScanResultDto(
                existingRecord.Id,
                session.Id,
                credential.StudentId,
                studentName,
                studentCode,
                existingRecord.Status.ToString(),
                existingRecord.CheckInAt ?? existingRecord.RecordedAt,
                existingRecord.IsGuestMakeup,
                existingRecord.HomeGroupId,
                NotificationStaged: false,
                stopwatch.ElapsedMilliseconds));
        }

        // 8. Create attendance record
        var record = AttendanceRecord.CreateCheckIn(
            session,
            credential.StudentId,
            workspaceId,
            now,
            _currentUserService.UserId);

        await _unitOfWork.AttendanceRecords.AddAsync(record, cancellationToken);

        // 9. Append history audit entry
        await _unitOfWork.AttendanceHistories.AddAsync(AttendanceHistory.Create(
            attendanceRecordId: record.Id,
            oldStatus: null,
            newStatus: record.Status,
            oldSource: null,
            newSource: record.Source,
            changedByUserId: _currentUserService.UserId,
            changedAt: now,
            reason: "Live door card scan check-in",
            changeKind: AttendanceChangeKind.InitialUserEntry), cancellationToken);

        // 10. Transactional WhatsApp Outbox Staging
        var notificationStaged = false;
        if (workspaceStudent.WhatsappOptIn && !string.IsNullOrWhiteSpace(workspaceStudent.GuardianPhone))
        {
            var idempotencyKey = $"att_{record.Id}_{record.Status}";
            var payload = $"{{\"studentId\":\"{credential.StudentId}\",\"sessionId\":\"{session.Id}\",\"status\":\"{record.Status}\",\"checkInAt\":\"{now:O}\"}}";

            var outboxMessage = MessageOutbox.CreateForAttendance(
                workspaceId: workspaceId,
                studentId: credential.StudentId,
                attendanceRecordId: record.Id,
                recipientPhone: workspaceStudent.GuardianPhone,
                messageType: "AttendanceCheckIn",
                payload: payload,
                idempotencyKey: idempotencyKey,
                languageCode: "ar");
            outboxMessage.TemplateName = "attendance_check_in_v1";

            await _unitOfWork.MessageOutboxes.AddAsync(outboxMessage, cancellationToken);
            notificationStaged = true;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Concurrent duplicate scan - reload existing record idempotently
            var concurrentRecord = await _unitOfWork.AttendanceRecords
                .FirstOrDefaultAsync(r => r.SessionId == session.Id && r.StudentId == credential.StudentId && r.VoidedAt == null, cancellationToken);

            if (concurrentRecord != null)
            {
                stopwatch.Stop();
                return Result.Success(new AttendanceScanResultDto(
                    concurrentRecord.Id,
                    session.Id,
                    credential.StudentId,
                    studentName,
                    studentCode,
                    concurrentRecord.Status.ToString(),
                    concurrentRecord.CheckInAt ?? concurrentRecord.RecordedAt,
                    concurrentRecord.IsGuestMakeup,
                    concurrentRecord.HomeGroupId,
                    NotificationStaged: false,
                    stopwatch.ElapsedMilliseconds));
            }

            throw;
        }

        stopwatch.Stop();

        return Result.Success(new AttendanceScanResultDto(
            record.Id,
            session.Id,
            credential.StudentId,
            studentName,
            studentCode,
            record.Status.ToString(),
            now,
            record.IsGuestMakeup,
            record.HomeGroupId,
            notificationStaged,
            stopwatch.ElapsedMilliseconds));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        if (message.Contains("ux_attendance_records_session_student") || message.Contains("23505") || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var sqlStateProp = ex.InnerException?.GetType().GetProperty("SqlState");
        if (sqlStateProp?.GetValue(ex.InnerException) is string sqlState && sqlState == "23505")
        {
            return true;
        }

        return false;
    }
}