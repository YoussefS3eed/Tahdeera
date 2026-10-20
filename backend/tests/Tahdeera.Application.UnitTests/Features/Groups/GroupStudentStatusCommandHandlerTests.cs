using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Features.Groups.Commands.ChangeGroupStudentStatus;
using Tahdeera.Application.Features.Groups.Commands.WithdrawStudentFromGroup;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Features.Groups;

public class GroupStudentStatusCommandHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task HandleWithdrawal_ShouldSetLeftAtAndRecordHistory()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var studentId = Guid.NewGuid();
        var enrollment = GroupStudent.Create(group.Id, studentId, DateTimeOffset.UtcNow.AddMonths(-1));
        context.GroupStudents.Add(enrollment);
        await context.SaveChangesAsync();

        var handler = new WithdrawStudentFromGroupCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new WithdrawStudentFromGroupCommand(_workspaceId, group.Id, studentId, "Family relocation", _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var enrollmentInDb = await context.GroupStudents.FirstAsync(gs => gs.Id == enrollment.Id);
        enrollmentInDb.Status.Should().Be(GroupStudentStatus.Withdrawn);
        enrollmentInDb.LeftAt.Should().NotBeNull();

        var history = await context.GroupStudentStatusHistories.FirstOrDefaultAsync(h => h.GroupStudentId == enrollment.Id);
        history.Should().NotBeNull();
        history!.OldStatus.Should().Be(GroupStudentStatus.Active);
        history.NewStatus.Should().Be(GroupStudentStatus.Withdrawn);
        history.Reason.Should().Be("Family relocation");
    }

    [Fact]
    public async Task HandleChangeStatus_ToSuspended_ShouldSuspendAndRecordHistory()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var studentId = Guid.NewGuid();
        var enrollment = GroupStudent.Create(group.Id, studentId, DateTimeOffset.UtcNow.AddMonths(-1));
        context.GroupStudents.Add(enrollment);
        await context.SaveChangesAsync();

        var handler = new ChangeGroupStudentStatusCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new ChangeGroupStudentStatusCommand(_workspaceId, group.Id, studentId, GroupStudentStatus.Suspended, "Disciplinary action", _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var enrollmentInDb = await context.GroupStudents.FirstAsync(gs => gs.Id == enrollment.Id);
        enrollmentInDb.Status.Should().Be(GroupStudentStatus.Suspended);

        var history = await context.GroupStudentStatusHistories.FirstOrDefaultAsync(h => h.GroupStudentId == enrollment.Id);
        history.Should().NotBeNull();
        history!.NewStatus.Should().Be(GroupStudentStatus.Suspended);
        history.Reason.Should().Be("Disciplinary action");
    }

    [Fact]
    public async Task HandleChangeStatus_ReinstatementToActive_ShouldClearLeftAtAndRecordHistory()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var studentId = Guid.NewGuid();
        var enrollment = GroupStudent.Create(group.Id, studentId, DateTimeOffset.UtcNow.AddMonths(-1));
        enrollment.Withdraw(DateTimeOffset.UtcNow.AddDays(-5), _actorUserId);
        context.GroupStudents.Add(enrollment);
        await context.SaveChangesAsync();

        var handler = new ChangeGroupStudentStatusCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new ChangeGroupStudentStatusCommand(_workspaceId, group.Id, studentId, GroupStudentStatus.Active, "Administrative clearance", _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var enrollmentInDb = await context.GroupStudents.FirstAsync(gs => gs.Id == enrollment.Id);
        enrollmentInDb.Status.Should().Be(GroupStudentStatus.Active);
        enrollmentInDb.LeftAt.Should().BeNull();

        var latestHistory = await context.GroupStudentStatusHistories
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync(h => h.GroupStudentId == enrollment.Id);
        latestHistory.Should().NotBeNull();
        latestHistory!.OldStatus.Should().Be(GroupStudentStatus.Withdrawn);
        latestHistory.NewStatus.Should().Be(GroupStudentStatus.Active);
        latestHistory.Reason.Should().Be("Administrative clearance");
    }

    [Fact]
    public void Validator_WhenReasonMissing_ShouldFailWithCorrectionReasonRequired()
    {
        // Arrange
        var validator = new ChangeGroupStudentStatusCommandValidator();
        var command = new ChangeGroupStudentStatusCommand(_workspaceId, Guid.NewGuid(), Guid.NewGuid(), GroupStudentStatus.Suspended, "", _actorUserId);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CORRECTION_REASON_REQUIRED");
    }
}
