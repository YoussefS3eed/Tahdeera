using FluentAssertions;
using Tahdeera.Application.Features.Sessions.Commands.CancelSession;
using Tahdeera.Application.Features.Sessions.Commands.CompleteSession;
using Tahdeera.Application.Features.Sessions.Commands.ScheduleSession;
using Tahdeera.Application.Features.Sessions.Commands.UpdateSessionSchedule;

namespace Tahdeera.Application.UnitTests.Validators;

public class SessionsValidatorTests
{
    [Fact]
    public void ScheduleSessionCommandValidator_ValidCommand_Passes()
    {
        var validator = new ScheduleSessionCommandValidator();
        var now = DateTimeOffset.UtcNow;
        var cmd = new ScheduleSessionCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            StartsAt: now,
            EndsAt: now.AddHours(2),
            LateGraceMinutes: 15,
            RoomName: "قاعة أ",
            Topic: "مقدمة في التفاضل والتكامل",
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ScheduleSessionCommandValidator_EndsAtBeforeOrEqualStartsAt_Fails()
    {
        var validator = new ScheduleSessionCommandValidator();
        var now = DateTimeOffset.UtcNow;
        var cmd = new ScheduleSessionCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            StartsAt: now.AddHours(2),
            EndsAt: now,
            LateGraceMinutes: 15,
            RoomName: null,
            Topic: null,
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SESSION_INVALID_TIME_RANGE");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1441)]
    public void ScheduleSessionCommandValidator_InvalidLateGrace_Fails(int lateGrace)
    {
        var validator = new ScheduleSessionCommandValidator();
        var now = DateTimeOffset.UtcNow;
        var cmd = new ScheduleSessionCommand(
            WorkspaceId: Guid.NewGuid(),
            GroupId: Guid.NewGuid(),
            StartsAt: now,
            EndsAt: now.AddHours(1),
            LateGraceMinutes: lateGrace,
            RoomName: null,
            Topic: null,
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ScheduleSessionCommand.LateGraceMinutes));
    }

    [Fact]
    public void UpdateSessionScheduleCommandValidator_ValidCommand_Passes()
    {
        var validator = new UpdateSessionScheduleCommandValidator();
        var now = DateTimeOffset.UtcNow;
        var cmd = new UpdateSessionScheduleCommand(
            WorkspaceId: Guid.NewGuid(),
            SessionId: Guid.NewGuid(),
            StartsAt: now,
            EndsAt: now.AddHours(1).AddMinutes(30),
            LateGraceMinutes: 10,
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateSessionScheduleCommandValidator_EndsAtBeforeStartsAt_Fails()
    {
        var validator = new UpdateSessionScheduleCommandValidator();
        var now = DateTimeOffset.UtcNow;
        var cmd = new UpdateSessionScheduleCommand(
            WorkspaceId: Guid.NewGuid(),
            SessionId: Guid.NewGuid(),
            StartsAt: now.AddHours(1),
            EndsAt: now,
            LateGraceMinutes: 10,
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "SESSION_INVALID_TIME_RANGE");
    }

    [Fact]
    public void CompleteSessionCommandValidator_ValidCommand_Passes()
    {
        var validator = new CompleteSessionCommandValidator();
        var cmd = new CompleteSessionCommand(
            WorkspaceId: Guid.NewGuid(),
            SessionId: Guid.NewGuid(),
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CompleteSessionCommandValidator_EmptyIds_Fails()
    {
        var validator = new CompleteSessionCommandValidator();
        var cmd = new CompleteSessionCommand(
            WorkspaceId: Guid.Empty,
            SessionId: Guid.Empty,
            ActorUserId: Guid.Empty);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().Be(3);
    }

    [Fact]
    public void CancelSessionCommandValidator_ValidCommand_Passes()
    {
        var validator = new CancelSessionCommandValidator();
        var cmd = new CancelSessionCommand(
            WorkspaceId: Guid.NewGuid(),
            SessionId: Guid.NewGuid(),
            CancellationReason: "عطل مفاجئ في التيار الكهربائي",
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CancelSessionCommandValidator_EmptyReason_Fails()
    {
        var validator = new CancelSessionCommandValidator();
        var cmd = new CancelSessionCommand(
            WorkspaceId: Guid.NewGuid(),
            SessionId: Guid.NewGuid(),
            CancellationReason: "",
            ActorUserId: Guid.NewGuid());

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorCode == "CANCELLATION_REASON_REQUIRED");
    }
}
