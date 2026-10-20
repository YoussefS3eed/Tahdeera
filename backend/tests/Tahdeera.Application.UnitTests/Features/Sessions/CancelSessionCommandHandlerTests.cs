using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Features.Sessions.Commands.CancelSession;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Sessions;

public class CancelSessionCommandHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WhenValidCancellation_ShouldCancelSessionAndCascadeVoidRecords()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var session = Session.Create(group.Id, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(3), 20, _actorUserId);
        context.Sessions.Add(session);

        var studentId = Guid.NewGuid();
        var record = AttendanceRecord.CreateCheckIn(session, studentId, _workspaceId, session.StartsAt, _actorUserId);
        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync();

        var handler = new CancelSessionCommandHandler(new UnitOfWork(context));
        var command = new CancelSessionCommand(_workspaceId, session.Id, "Teacher emergency illness", _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CancellationReason.Should().Be("Teacher emergency illness");
        result.Value.VoidedAttendanceRecordsCount.Should().Be(1);

        var sessionInDb = await context.Sessions.FirstAsync(s => s.Id == session.Id);
        sessionInDb.Status.Should().Be(SessionStatus.Cancelled);
        sessionInDb.CancellationReason.Should().Be("Teacher emergency illness");

        var recordInDb = await context.AttendanceRecords.FirstAsync(r => r.Id == record.Id);
        recordInDb.VoidedAt.Should().NotBeNull();
        recordInDb.VoidReason.Should().Be("Teacher emergency illness");

        var history = await context.AttendanceHistories.FirstOrDefaultAsync(h => h.AttendanceRecordId == record.Id);
        history.Should().NotBeNull();
        history!.ChangeKind.Should().Be(AttendanceChangeKind.VoidedOnCancellation);
        history.Reason.Should().Be("Teacher emergency illness");
    }

    [Fact]
    public void Validator_WhenReasonMissing_ShouldFailWithCancellationReasonRequired()
    {
        // Arrange
        var validator = new CancelSessionCommandValidator();
        var command = new CancelSessionCommand(_workspaceId, Guid.NewGuid(), "", _actorUserId);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CANCELLATION_REASON_REQUIRED");
    }

    [Fact]
    public async Task Handle_WhenNonOwningWorkspace_ShouldReturnUnauthorized()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _actorUserId);
        context.Groups.Add(group);

        var session = Session.Create(group.Id, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(3), 20, _actorUserId);
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var foreignWorkspaceId = Guid.NewGuid();
        var handler = new CancelSessionCommandHandler(new UnitOfWork(context));
        var command = new CancelSessionCommand(foreignWorkspaceId, session.Id, "Cancelling from partner", _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_WORKSPACE_ACCESS");
    }
}
