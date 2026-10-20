using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;

namespace Tahdeera.Infrastructure.Services;

public sealed class AttendanceReconciliationService : IAttendanceReconciliationService
{
    private readonly TahdeeraDbContext _context;

    public AttendanceReconciliationService(TahdeeraDbContext context)
    {
        _context = context;
    }

    public async Task<int> CalculateConsecutiveAbsenceStreakAsync(
        Guid groupId,
        Guid studentId,
        CancellationToken ct = default)
    {
        // 1. Fetch completed sessions in this group ordered by StartsAt DESC
        var completedSessions = await _context.Sessions
            .Where(s => s.GroupId == groupId && s.Status == SessionStatus.Completed)
            .OrderByDescending(s => s.StartsAt)
            .ToListAsync(ct);

        if (completedSessions.Count == 0)
        {
            return 0;
        }

        var completedSessionIds = completedSessions.Select(s => s.Id).ToList();

        // 2. Fetch non-voided attendance records for this student in these sessions
        var records = await _context.AttendanceRecords
            .Where(r => completedSessionIds.Contains(r.SessionId) && r.StudentId == studentId && r.VoidedAt == null)
            .ToListAsync(ct);

        var recordDict = records.ToDictionary(r => r.SessionId);

        var streak = 0;
        foreach (var session in completedSessions)
        {
            if (recordDict.TryGetValue(session.Id, out var record))
            {
                if (record.Status == AttendanceStatus.Absent)
                {
                    streak++;
                }
                else
                {
                    // Present, Late, or Excused breaks the unexcused absence streak
                    break;
                }
            }
            else
            {
                // Completed session with no record is treated as unexcused absence
                streak++;
            }
        }

        return streak;
    }

    public async Task ReconcileStudentStreakAsync(
        Guid groupId,
        Guid studentId,
        CancellationToken ct = default)
    {
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);

        if (group is null)
        {
            return;
        }

        var groupStudent = await _context.GroupStudents
            .FirstOrDefaultAsync(gs => gs.GroupId == groupId && gs.StudentId == studentId, ct);

        if (groupStudent is null)
        {
            return;
        }

        var streak = await CalculateConsecutiveAbsenceStreakAsync(groupId, studentId, ct);
        var now = DateTimeOffset.UtcNow;

        // Condition 1: Streak meets or exceeds group absence threshold
        if (streak >= group.ConsecutiveAbsenceThreshold)
        {
            if (groupStudent.Status == GroupStudentStatus.Active)
            {
                if (group.AbsenceAction == AbsenceAction.MarkSuspended)
                {
                    var oldStatus = groupStudent.Status;
                    groupStudent.Suspend(null);

                    _context.GroupStudentStatusHistories.Add(GroupStudentStatusHistory.Record(
                        groupStudent.Id,
                        oldStatus,
                        GroupStudentStatus.Suspended,
                        now,
                        null,
                        $"Consecutive absences ({streak}) reached threshold ({group.ConsecutiveAbsenceThreshold})"));
                }
                else if (group.AbsenceAction == AbsenceAction.Archive)
                {
                    var oldStatus = groupStudent.Status;
                    groupStudent.Withdraw(now, null);

                    _context.GroupStudentStatusHistories.Add(GroupStudentStatusHistory.Record(
                        groupStudent.Id,
                        oldStatus,
                        GroupStudentStatus.Withdrawn,
                        now,
                        null,
                        $"Consecutive absences ({streak}) reached threshold ({group.ConsecutiveAbsenceThreshold})"));
                }
            }
        }
        // Condition 2: Streak drops below threshold (e.g. after excusal)
        else if (streak < group.ConsecutiveAbsenceThreshold)
        {
            if (groupStudent.Status == GroupStudentStatus.Suspended)
            {
                var oldStatus = groupStudent.Status;
                groupStudent.Reinstate(null);

                _context.GroupStudentStatusHistories.Add(GroupStudentStatusHistory.Record(
                    groupStudent.Id,
                    oldStatus,
                    GroupStudentStatus.Active,
                    now,
                    null,
                    $"Absence excused - streak ({streak}) dropped below threshold ({group.ConsecutiveAbsenceThreshold})"));
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<ReconciliationResult> ReconcileStudentAbsencesAsync(
        Guid workspaceId,
        Guid groupId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        await ReconcileStudentStreakAsync(groupId, studentId, cancellationToken);
        var streak = await CalculateConsecutiveAbsenceStreakAsync(groupId, studentId, cancellationToken);
        var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        var thresholdExceeded = group != null && streak >= group.ConsecutiveAbsenceThreshold;
        var recommendedAction = group?.AbsenceAction ?? AbsenceAction.NotifyOnly;

        var studentInfo = new AbsenceCountInfo(
            studentId,
            streak,
            null,
            thresholdExceeded,
            recommendedAction);

        return new ReconciliationResult(groupId, new[] { studentInfo }, thresholdExceeded ? 1 : 0);
    }
}
