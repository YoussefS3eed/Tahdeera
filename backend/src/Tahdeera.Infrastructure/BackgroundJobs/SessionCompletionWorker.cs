using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;

namespace Tahdeera.Infrastructure.BackgroundJobs;

public sealed class SessionCompletionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AttendanceWorkerOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<SessionCompletionWorker> _logger;

    public SessionCompletionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AttendanceWorkerOptions> options,
        IDateTimeProvider dateTimeProvider,
        ILogger<SessionCompletionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionCompletionWorker started with interval {Interval}m", _options.SessionCompletionIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredSessionsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred executing SessionCompletionWorker cycle");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_options.SessionCompletionIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("SessionCompletionWorker stopped");
    }

    public async Task<int> ProcessExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var reconciliationService = scope.ServiceProvider.GetRequiredService<IAttendanceReconciliationService>();
        var now = _dateTimeProvider.UtcNow;

        // Query Scheduled sessions with group and owner workspace
        var scheduledSessions = await context.Sessions
            .Include(s => s.Group)
            .ThenInclude(g => g.OwnerWorkspace)
            .Where(s => s.Status == SessionStatus.Scheduled)
            .ToListAsync(cancellationToken);

        // Filter expired sessions past the per-workspace buffer
        var expiredSessions = scheduledSessions
            .Where(s => s.EndsAt.AddMinutes(s.Group.OwnerWorkspace.AutoAbsenceBufferMinutes) <= now)
            .Take(_options.BatchSize)
            .ToList();

        if (expiredSessions.Count == 0)
        {
            return 0;
        }

        foreach (var session in expiredSessions)
        {
            session.MarkCompleted(now);

            // Fetch active enrolled students
            var activeStudents = await context.GroupStudents
                .Where(gs => gs.GroupId == session.GroupId && gs.Status == GroupStudentStatus.Active)
                .ToListAsync(cancellationToken);

            // Fetch already checked-in students
            var existingAttendance = await context.AttendanceRecords
                .Where(r => r.SessionId == session.Id && r.VoidedAt == null)
                .ToListAsync(cancellationToken);

            var existingStudentIds = existingAttendance.Select(r => r.StudentId).ToHashSet();
            var unattendedStudents = activeStudents
                .Where(gs => !existingStudentIds.Contains(gs.StudentId))
                .ToList();

            var unattendedIds = unattendedStudents.Select(u => u.StudentId).ToList();

            var workspaceStudents = await context.WorkspaceStudents
                .Where(ws => ws.WorkspaceId == session.Group.OwnerWorkspaceId && unattendedIds.Contains(ws.StudentId))
                .ToListAsync(cancellationToken);

            var contactDict = workspaceStudents.ToDictionary(ws => ws.StudentId);

            foreach (var student in unattendedStudents)
            {
                var absence = AttendanceRecord.CreateSystemAbsence(
                    session.Id,
                    student.StudentId,
                    session.Group.OwnerWorkspaceId,
                    now);

                context.AttendanceRecords.Add(absence);

                context.AttendanceHistories.Add(AttendanceHistory.Create(
                    attendanceRecordId: absence.Id,
                    oldStatus: null,
                    newStatus: AttendanceStatus.Absent,
                    oldSource: null,
                    newSource: AttendanceSource.System,
                    changedByUserId: null,
                    changedAt: now,
                    reason: "System automated absence on session completion",
                    changeKind: AttendanceChangeKind.InitialSystemAbsence));

                if (contactDict.TryGetValue(student.StudentId, out var wsStudent) &&
                    wsStudent.WhatsappOptIn &&
                    !string.IsNullOrWhiteSpace(wsStudent.GuardianPhone))
                {
                    var idempotencyKey = $"att_{absence.Id}_Absent";
                    var payload = $"{{\"studentId\":\"{student.StudentId}\",\"sessionId\":\"{session.Id}\",\"status\":\"Absent\",\"recordedAt\":\"{now:O}\"}}";

                    var outbox = MessageOutbox.CreateForAttendance(
                        workspaceId: session.Group.OwnerWorkspaceId,
                        studentId: student.StudentId,
                        attendanceRecordId: absence.Id,
                        recipientPhone: wsStudent.GuardianPhone,
                        messageType: "AbsenceAlert",
                        payload: payload,
                        idempotencyKey: idempotencyKey,
                        languageCode: "ar");
                    outbox.TemplateName = "absence_alert_v1";

                    context.MessageOutboxes.Add(outbox);
                }

                await reconciliationService.ReconcileStudentStreakAsync(session.GroupId, student.StudentId, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return expiredSessions.Count;
    }
}
