using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Services;

namespace Tahdeera.Application.UnitTests.Features.Attendance;

public class AttendanceReconciliationServiceTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task ReconcileStudentStreakAsync_WhenStreakReachesThreshold_ShouldSuspendStudent()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Chemistry A", _actorUserId);
        group.ConsecutiveAbsenceThreshold = 3;
        group.AbsenceAction = AbsenceAction.MarkSuspended;
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_STREAK_1", FullName = "Tamer" };
        var groupStudent = GroupStudent.Create(group.Id, student.Id, baseTime.AddDays(-30));
        context.Students.Add(student);
        context.GroupStudents.Add(groupStudent);

        // 3 Completed sessions, all with Absent status
        var s1 = Session.Create(group.Id, baseTime.AddDays(-14), baseTime.AddDays(-14).AddHours(2), 20, _actorUserId);
        s1.MarkCompleted(baseTime.AddDays(-14).AddHours(2));
        var s2 = Session.Create(group.Id, baseTime.AddDays(-7), baseTime.AddDays(-7).AddHours(2), 20, _actorUserId);
        s2.MarkCompleted(baseTime.AddDays(-7).AddHours(2));
        var s3 = Session.Create(group.Id, baseTime.AddDays(-1), baseTime.AddDays(-1).AddHours(2), 20, _actorUserId);
        s3.MarkCompleted(baseTime.AddDays(-1).AddHours(2));

        var a1 = AttendanceRecord.CreateSystemAbsence(s1.Id, student.Id, _workspaceId, s1.EndsAt);
        var a2 = AttendanceRecord.CreateSystemAbsence(s2.Id, student.Id, _workspaceId, s2.EndsAt);
        var a3 = AttendanceRecord.CreateSystemAbsence(s3.Id, student.Id, _workspaceId, s3.EndsAt);

        context.Sessions.AddRange(s1, s2, s3);
        context.AttendanceRecords.AddRange(a1, a2, a3);
        await context.SaveChangesAsync();

        var service = new AttendanceReconciliationService(context);

        // Act
        var streak = await service.CalculateConsecutiveAbsenceStreakAsync(group.Id, student.Id);
        await service.ReconcileStudentStreakAsync(group.Id, student.Id);

        // Assert
        streak.Should().Be(3);

        var groupStudentInDb = await context.GroupStudents.FirstAsync(gs => gs.Id == groupStudent.Id);
        groupStudentInDb.Status.Should().Be(GroupStudentStatus.Suspended);

        var historyInDb = await context.GroupStudentStatusHistories.FirstOrDefaultAsync(h => h.GroupStudentId == groupStudent.Id);
        historyInDb.Should().NotBeNull();
        historyInDb!.OldStatus.Should().Be(GroupStudentStatus.Active);
        historyInDb.NewStatus.Should().Be(GroupStudentStatus.Suspended);
    }

    [Fact]
    public async Task ReconcileStudentStreakAsync_WhenAbsenceExcused_ShouldReinstateSuspendedStudent()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Chemistry A", _actorUserId);
        group.ConsecutiveAbsenceThreshold = 3;
        group.AbsenceAction = AbsenceAction.MarkSuspended;
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_STREAK_2", FullName = "Tamer" };
        var groupStudent = GroupStudent.Create(group.Id, student.Id, baseTime.AddDays(-30));
        groupStudent.Suspend(null); // Currently suspended

        context.Students.Add(student);
        context.GroupStudents.Add(groupStudent);

        // 3 completed sessions: s1 Absent, s2 Excused, s3 Absent -> current streak from latest is only 1!
        var s1 = Session.Create(group.Id, baseTime.AddDays(-14), baseTime.AddDays(-14).AddHours(2), 20, _actorUserId);
        s1.MarkCompleted(baseTime.AddDays(-14).AddHours(2));
        var s2 = Session.Create(group.Id, baseTime.AddDays(-7), baseTime.AddDays(-7).AddHours(2), 20, _actorUserId);
        s2.MarkCompleted(baseTime.AddDays(-7).AddHours(2));
        var s3 = Session.Create(group.Id, baseTime.AddDays(-1), baseTime.AddDays(-1).AddHours(2), 20, _actorUserId);
        s3.MarkCompleted(baseTime.AddDays(-1).AddHours(2));

        var a1 = AttendanceRecord.CreateSystemAbsence(s1.Id, student.Id, _workspaceId, s1.EndsAt);
        var a2 = AttendanceRecord.CreateSystemAbsence(s2.Id, student.Id, _workspaceId, s2.EndsAt);
        a2.Status = AttendanceStatus.Excused; // Excused!
        var a3 = AttendanceRecord.CreateSystemAbsence(s3.Id, student.Id, _workspaceId, s3.EndsAt);

        context.Sessions.AddRange(s1, s2, s3);
        context.AttendanceRecords.AddRange(a1, a2, a3);
        await context.SaveChangesAsync();

        var service = new AttendanceReconciliationService(context);

        // Act
        var streak = await service.CalculateConsecutiveAbsenceStreakAsync(group.Id, student.Id);
        await service.ReconcileStudentStreakAsync(group.Id, student.Id);

        // Assert
        streak.Should().Be(1); // Streak broken by Excused record

        var groupStudentInDb = await context.GroupStudents.FirstAsync(gs => gs.Id == groupStudent.Id);
        groupStudentInDb.Status.Should().Be(GroupStudentStatus.Active); // Reinstated!

        var reinstatementHistory = await context.GroupStudentStatusHistories
            .FirstOrDefaultAsync(h => h.GroupStudentId == groupStudent.Id && h.NewStatus == GroupStudentStatus.Active);
        reinstatementHistory.Should().NotBeNull();
        reinstatementHistory!.OldStatus.Should().Be(GroupStudentStatus.Suspended);
    }

    [Fact]
    public async Task ReconcileStudentStreakAsync_WhenAbsenceActionIsArchive_ShouldWithdrawStudent()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics B", _actorUserId);
        group.ConsecutiveAbsenceThreshold = 2;
        group.AbsenceAction = AbsenceAction.Archive;
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_ARCHIVE_1", FullName = "Kareem" };
        var groupStudent = GroupStudent.Create(group.Id, student.Id, baseTime.AddDays(-30));
        context.Students.Add(student);
        context.GroupStudents.Add(groupStudent);

        var s1 = Session.Create(group.Id, baseTime.AddDays(-7), baseTime.AddDays(-7).AddHours(2), 20, _actorUserId);
        s1.MarkCompleted(baseTime.AddDays(-7).AddHours(2));
        var s2 = Session.Create(group.Id, baseTime.AddDays(-1), baseTime.AddDays(-1).AddHours(2), 20, _actorUserId);
        s2.MarkCompleted(baseTime.AddDays(-1).AddHours(2));

        var a1 = AttendanceRecord.CreateSystemAbsence(s1.Id, student.Id, _workspaceId, s1.EndsAt);
        var a2 = AttendanceRecord.CreateSystemAbsence(s2.Id, student.Id, _workspaceId, s2.EndsAt);

        context.Sessions.AddRange(s1, s2);
        context.AttendanceRecords.AddRange(a1, a2);
        await context.SaveChangesAsync();

        var service = new AttendanceReconciliationService(context);

        // Act
        await service.ReconcileStudentStreakAsync(group.Id, student.Id);

        // Assert
        var groupStudentInDb = await context.GroupStudents.FirstAsync(gs => gs.Id == groupStudent.Id);
        groupStudentInDb.Status.Should().Be(GroupStudentStatus.Withdrawn);

        var historyInDb = await context.GroupStudentStatusHistories.FirstOrDefaultAsync(h => h.GroupStudentId == groupStudent.Id);
        historyInDb.Should().NotBeNull();
        historyInDb!.OldStatus.Should().Be(GroupStudentStatus.Active);
        historyInDb.NewStatus.Should().Be(GroupStudentStatus.Withdrawn);
    }

    [Fact]
    public async Task ReconcileStudentAbsencesAsync_ThresholdExceeded_ReturnsAccurateResult()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 11, "Biology C", _actorUserId);
        group.ConsecutiveAbsenceThreshold = 2;
        group.AbsenceAction = AbsenceAction.MarkSuspended;
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_RECONCILE_1", FullName = "Sara" };
        var groupStudent = GroupStudent.Create(group.Id, student.Id, baseTime.AddDays(-15));
        context.Students.Add(student);
        context.GroupStudents.Add(groupStudent);

        var s1 = Session.Create(group.Id, baseTime.AddDays(-5), baseTime.AddDays(-5).AddHours(2), 20, _actorUserId);
        s1.MarkCompleted(baseTime.AddDays(-5).AddHours(2));
        var s2 = Session.Create(group.Id, baseTime.AddDays(-1), baseTime.AddDays(-1).AddHours(2), 20, _actorUserId);
        s2.MarkCompleted(baseTime.AddDays(-1).AddHours(2));

        var a1 = AttendanceRecord.CreateSystemAbsence(s1.Id, student.Id, _workspaceId, s1.EndsAt);
        var a2 = AttendanceRecord.CreateSystemAbsence(s2.Id, student.Id, _workspaceId, s2.EndsAt);

        context.Sessions.AddRange(s1, s2);
        context.AttendanceRecords.AddRange(a1, a2);
        await context.SaveChangesAsync();

        var service = new AttendanceReconciliationService(context);

        // Act
        var result = await service.ReconcileStudentAbsencesAsync(_workspaceId, group.Id, student.Id);

        // Assert
        result.Should().NotBeNull();
        result.GroupId.Should().Be(group.Id);
        result.SanctionsAppliedCount.Should().Be(1);
        result.StudentAbsences.Should().HaveCount(1);
        result.StudentAbsences.First().ConsecutiveAbsenceCount.Should().Be(2);
        result.StudentAbsences.First().ThresholdExceeded.Should().BeTrue();
        result.StudentAbsences.First().RecommendedAction.Should().Be(AbsenceAction.MarkSuspended);
    }

    [Fact]
    public async Task CalculateConsecutiveAbsenceStreakAsync_WhenSessionHasNoRecord_TreatsAsUnexcusedAbsence()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var baseTime = DateTimeOffset.UtcNow;

        var group = Group.Create(_workspaceId, _academicYearId, 12, "Math 101", _actorUserId);
        context.Groups.Add(group);

        var student = new Student { Id = Guid.NewGuid(), StudentCode = "STU_NOREC_1", FullName = "Hany" };
        context.Students.Add(student);

        // Session completed with no record created for student
        var s1 = Session.Create(group.Id, baseTime.AddDays(-1), baseTime.AddDays(-1).AddHours(2), 20, _actorUserId);
        s1.MarkCompleted(baseTime.AddDays(-1).AddHours(2));
        context.Sessions.Add(s1);
        await context.SaveChangesAsync();

        var service = new AttendanceReconciliationService(context);

        // Act
        var streak = await service.CalculateConsecutiveAbsenceStreakAsync(group.Id, student.Id);

        // Assert
        streak.Should().Be(1);
    }
}
