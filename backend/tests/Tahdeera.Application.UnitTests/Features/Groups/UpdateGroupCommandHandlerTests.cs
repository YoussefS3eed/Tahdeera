using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Features.Groups.Commands.ChangeGroupStatus;
using Tahdeera.Application.Features.Groups.Commands.UpdateGroup;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Features.Groups;

public class UpdateGroupCommandHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WhenRecalculateNowIsFalse_ShouldUpdateGroupWithoutMutatingStudents()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(
            ownerWorkspaceId: _workspaceId,
            academicYearId: _academicYearId,
            gradeId: 10,
            name: "Math 101",
            responsibleTeacherUserId: _actorUserId,
            consecutiveAbsenceThreshold: 5,
            absenceAction: AbsenceAction.MarkSuspended);
        context.Groups.Add(group);

        var studentId = Guid.NewGuid();
        var groupStudent = GroupStudent.Create(group.Id, studentId, DateTimeOffset.UtcNow.AddMonths(-1));
        context.GroupStudents.Add(groupStudent);
        await context.SaveChangesAsync();

        var handler = new UpdateGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new UpdateGroupCommand(
            WorkspaceId: _workspaceId,
            GroupId: group.Id,
            Name: "Math 101 Advanced",
            RoomName: "Room 303",
            ScheduleSummary: "Tue/Thu 4pm",
            LateGraceMinutes: 10,
            DefaultMonthlyFee: 300m,
            ConsecutiveAbsenceThreshold: 2,
            AbsenceAction: AbsenceAction.MarkSuspended,
            ActorUserId: _actorUserId,
            RecalculateNow: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Math 101 Advanced");
        result.Value.LateGraceMinutes.Should().Be(10);
        result.Value.ConsecutiveAbsenceThreshold.Should().Be(2);

        var studentInDb = await context.GroupStudents.FirstAsync(gs => gs.Id == groupStudent.Id);
        studentInDb.Status.Should().Be(GroupStudentStatus.Active);
    }

    [Fact]
    public async Task Handle_WhenRecalculateNowIsTrue_AndStudentBreachesThreshold_ShouldSuspendAndRecordHistory()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(
            ownerWorkspaceId: _workspaceId,
            academicYearId: _academicYearId,
            gradeId: 10,
            name: "Science 101",
            responsibleTeacherUserId: _actorUserId,
            consecutiveAbsenceThreshold: 5,
            absenceAction: AbsenceAction.MarkSuspended);
        context.Groups.Add(group);

        var studentId = Guid.NewGuid();
        var groupStudent = GroupStudent.Create(group.Id, studentId, DateTimeOffset.UtcNow.AddMonths(-1));
        context.GroupStudents.Add(groupStudent);

        // Add 2 completed sessions
        var s1 = Session.Create(group.Id, DateTimeOffset.UtcNow.AddDays(-5), DateTimeOffset.UtcNow.AddDays(-5).AddHours(2));
        s1.MarkCompleted(DateTimeOffset.UtcNow.AddDays(-5).AddHours(2));
        var s2 = Session.Create(group.Id, DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-2).AddHours(2));
        s2.MarkCompleted(DateTimeOffset.UtcNow.AddDays(-2).AddHours(2));
        context.Sessions.AddRange(s1, s2);

        // Add Absent records for both sessions
        var a1 = AttendanceRecord.CreateSystemAbsence(s1.Id, studentId, _workspaceId, s1.EndsAt);
        var a2 = AttendanceRecord.CreateSystemAbsence(s2.Id, studentId, _workspaceId, s2.EndsAt);
        context.AttendanceRecords.AddRange(a1, a2);

        await context.SaveChangesAsync();

        var handler = new UpdateGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        // Lower threshold to 2 and set RecalculateNow = true
        var command = new UpdateGroupCommand(
            WorkspaceId: _workspaceId,
            GroupId: group.Id,
            Name: "Science 101",
            RoomName: "Lab 2",
            ScheduleSummary: null,
            LateGraceMinutes: 20,
            DefaultMonthlyFee: 200m,
            ConsecutiveAbsenceThreshold: 2,
            AbsenceAction: AbsenceAction.MarkSuspended,
            ActorUserId: _actorUserId,
            RecalculateNow: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var studentInDb = await context.GroupStudents.FirstAsync(gs => gs.Id == groupStudent.Id);
        studentInDb.Status.Should().Be(GroupStudentStatus.Suspended);

        var history = await context.GroupStudentStatusHistories.FirstOrDefaultAsync(h => h.GroupStudentId == groupStudent.Id);
        history.Should().NotBeNull();
        history!.OldStatus.Should().Be(GroupStudentStatus.Active);
        history.NewStatus.Should().Be(GroupStudentStatus.Suspended);
        history.Reason.Should().Be("Retroactive policy recalculation");
    }

    [Fact]
    public async Task Handle_WhenDuplicateNameOnUpdate_ShouldReturnConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var g1 = Group.Create(_workspaceId, _academicYearId, 10, "Arabic A", _actorUserId);
        var g2 = Group.Create(_workspaceId, _academicYearId, 10, "Arabic B", _actorUserId);
        context.Groups.AddRange(g1, g2);
        await context.SaveChangesAsync();

        var handler = new UpdateGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new UpdateGroupCommand(
            WorkspaceId: _workspaceId,
            GroupId: g2.Id,
            Name: "arabic a", // clashes with g1
            RoomName: null,
            ScheduleSummary: null,
            LateGraceMinutes: 20,
            DefaultMonthlyFee: 0,
            ConsecutiveAbsenceThreshold: null,
            AbsenceAction: AbsenceAction.NotifyOnly,
            ActorUserId: _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GROUP_NAME_DUPLICATE");
    }

    [Fact]
    public async Task HandleChangeStatus_ShouldTransitionGroupStatus()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "History 101", _actorUserId);
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var handler = new ChangeGroupStatusCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new ChangeGroupStatusCommand(_workspaceId, group.Id, GroupStatus.Archived, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var groupInDb = await context.Groups.FirstAsync(g => g.Id == group.Id);
        groupInDb.Status.Should().Be(GroupStatus.Archived);
    }
}
