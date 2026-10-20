using FluentAssertions;
using Tahdeera.Application.Features.Sessions.Commands.ScheduleSession;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Features.Sessions;

public class ScheduleSessionCommandHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WhenValid_ShouldScheduleSessionAndInheritGraceMinutes()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId, lateGraceMinutes: 25);
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var handler = new ScheduleSessionCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var startsAt = DateTimeOffset.UtcNow.AddHours(2);
        var endsAt = startsAt.AddHours(2);
        var command = new ScheduleSessionCommand(_workspaceId, group.Id, startsAt, endsAt, null, "Room 101", "Thermodynamics", _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GroupId.Should().Be(group.Id);
        result.Value.LateGraceMinutes.Should().Be(25); // Inherited from group
        result.Value.RoomName.Should().Be("Room 101");
        result.Value.Topic.Should().Be("Thermodynamics");
        result.Value.Status.Should().Be("Scheduled");
    }

    [Fact]
    public async Task Handle_WhenNonOwningWorkspace_ShouldReturnUnauthorized()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var foreignWorkspaceId = Guid.NewGuid();
        var handler = new ScheduleSessionCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var startsAt = DateTimeOffset.UtcNow.AddHours(2);
        var endsAt = startsAt.AddHours(2);
        var command = new ScheduleSessionCommand(foreignWorkspaceId, group.Id, startsAt, endsAt, null, null, null, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_WORKSPACE_ACCESS");
    }

    [Fact]
    public async Task Handle_WhenGroupArchived_ShouldReturnArchivedError()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        group.SetStatus(GroupStatus.Archived, _actorUserId);
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        var handler = new ScheduleSessionCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var startsAt = DateTimeOffset.UtcNow.AddHours(2);
        var endsAt = startsAt.AddHours(2);
        var command = new ScheduleSessionCommand(_workspaceId, group.Id, startsAt, endsAt, null, null, null, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GROUP_ARCHIVED_CANNOT_SCHEDULE");
    }

    [Fact]
    public void Validator_WhenEndsAtBeforeOrEqualStartsAt_ShouldFailValidation()
    {
        // Arrange
        var validator = new ScheduleSessionCommandValidator();
        var startsAt = DateTimeOffset.UtcNow.AddHours(2);
        var endsAt = startsAt.AddHours(-1); // Before start time
        var command = new ScheduleSessionCommand(_workspaceId, Guid.NewGuid(), startsAt, endsAt, null, null, null, _actorUserId);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SESSION_INVALID_TIME_RANGE");
    }
}
