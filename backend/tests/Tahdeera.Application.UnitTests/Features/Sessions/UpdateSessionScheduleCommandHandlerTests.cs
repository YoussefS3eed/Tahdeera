using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Features.Sessions.Commands.UpdateSessionSchedule;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Sessions;

public class UpdateSessionScheduleCommandHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WhenExtendingStartGrace_ShouldReevaluateLateToPresentAndLogHistory()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var originalStartsAt = DateTimeOffset.UtcNow.AddHours(1);
        var originalEndsAt = originalStartsAt.AddHours(2);
        var session = Session.Create(group.Id, originalStartsAt, originalEndsAt, 15, _actorUserId);
        context.Sessions.Add(session);

        var studentId = Guid.NewGuid();
        // Check-in at 20 min after start -> Initially Late with 15 min grace
        var checkInAt = originalStartsAt.AddMinutes(20);
        var record = AttendanceRecord.CreateCheckIn(session, studentId, _workspaceId, checkInAt, _actorUserId);
        record.Status.Should().Be(AttendanceStatus.Late);
        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync();

        var handler = new UpdateSessionScheduleCommandHandler(new UnitOfWork(context));
        // Extend grace minutes to 30 -> 20 min check-in becomes Present
        var command = new UpdateSessionScheduleCommand(
            WorkspaceId: _workspaceId,
            SessionId: session.Id,
            StartsAt: originalStartsAt,
            EndsAt: originalEndsAt,
            LateGraceMinutes: 30,
            ActorUserId: _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReevaluatedRecordsCount.Should().Be(1);

        var recordInDb = await context.AttendanceRecords.FirstAsync(r => r.Id == record.Id);
        recordInDb.Status.Should().Be(AttendanceStatus.Present);

        var history = await context.AttendanceHistories.FirstOrDefaultAsync(h => h.AttendanceRecordId == record.Id);
        history.Should().NotBeNull();
        history!.OldStatus.Should().Be(AttendanceStatus.Late);
        history.NewStatus.Should().Be(AttendanceStatus.Present);
        history.ChangeKind.Should().Be(AttendanceChangeKind.SystemReevaluation);
    }

    [Fact]
    public async Task Handle_WhenRecordIsManuallyLocked_ShouldNotReevaluate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var originalStartsAt = DateTimeOffset.UtcNow.AddHours(1);
        var originalEndsAt = originalStartsAt.AddHours(2);
        var session = Session.Create(group.Id, originalStartsAt, originalEndsAt, 15, _actorUserId);
        context.Sessions.Add(session);

        var studentId = Guid.NewGuid();
        var checkInAt = originalStartsAt.AddMinutes(20);
        var record = AttendanceRecord.CreateCheckIn(session, studentId, _workspaceId, checkInAt, _actorUserId);
        // Teacher manually set status to Late and locked it
        record.ManualOverride(AttendanceStatus.Late, _actorUserId, DateTimeOffset.UtcNow, "Teacher manual confirmation");
        record.AutoEvaluationLocked.Should().BeTrue();
        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync();

        var handler = new UpdateSessionScheduleCommandHandler(new UnitOfWork(context));
        var command = new UpdateSessionScheduleCommand(
            WorkspaceId: _workspaceId,
            SessionId: session.Id,
            StartsAt: originalStartsAt,
            EndsAt: originalEndsAt,
            LateGraceMinutes: 30,
            ActorUserId: _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReevaluatedRecordsCount.Should().Be(0);

        var recordInDb = await context.AttendanceRecords.FirstAsync(r => r.Id == record.Id);
        recordInDb.Status.Should().Be(AttendanceStatus.Late); // Preserved!
    }

    [Fact]
    public async Task Handle_WhenSessionCompleted_ShouldReturnScheduleFrozenError()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var startsAt = DateTimeOffset.UtcNow.AddHours(-3);
        var endsAt = startsAt.AddHours(2);
        var session = Session.Create(group.Id, startsAt, endsAt, 20, _actorUserId);
        session.MarkCompleted(endsAt);
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var handler = new UpdateSessionScheduleCommandHandler(new UnitOfWork(context));
        var command = new UpdateSessionScheduleCommand(_workspaceId, session.Id, startsAt, endsAt, 25, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SESSION_ALREADY_COMPLETED_SCHEDULE_FROZEN");
    }
}
