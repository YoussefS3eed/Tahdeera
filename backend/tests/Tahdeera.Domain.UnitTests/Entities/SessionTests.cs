using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.UnitTests.Entities;

public class SessionTests
{
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly DateTimeOffset _startsAt = new(2026, 10, 20, 16, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _endsAt = new(2026, 10, 20, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Throws_When_EndsAt_Before_Or_Equal_StartsAt()
    {
        var actEqual = () => Session.Create(_groupId, _startsAt, _startsAt);
        var actBefore = () => Session.Create(_groupId, _startsAt, _startsAt.AddMinutes(-30));

        actEqual.Should().Throw<ArgumentException>()
            .WithParameterName("endsAt");
        actBefore.Should().Throw<ArgumentException>()
            .WithParameterName("endsAt");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1441)]
    public void Create_Throws_When_LateGraceMinutes_Negative_Or_Above_1440(int invalidGrace)
    {
        var act = () => Session.Create(_groupId, _startsAt, _endsAt, lateGraceMinutes: invalidGrace);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("lateGraceMinutes");
    }

    [Fact]
    public void UpdateSchedule_Succeeds_When_Scheduled()
    {
        var session = Session.Create(_groupId, _startsAt, _endsAt, lateGraceMinutes: 20);
        var newStartsAt = _startsAt.AddHours(1);
        var newEndsAt = _endsAt.AddHours(1);

        session.UpdateSchedule(newStartsAt, newEndsAt, 15, Guid.NewGuid());

        session.StartsAt.Should().Be(newStartsAt);
        session.EndsAt.Should().Be(newEndsAt);
        session.LateGraceMinutes.Should().Be(15);
    }

    [Fact]
    public void UpdateSchedule_Throws_When_Completed_Or_Cancelled()
    {
        var sessionCompleted = Session.Create(_groupId, _startsAt, _endsAt);
        sessionCompleted.MarkCompleted(DateTimeOffset.UtcNow);

        var actCompleted = () => sessionCompleted.UpdateSchedule(
            _startsAt.AddHours(1), _endsAt.AddHours(1), 15, Guid.NewGuid());

        actCompleted.Should().Throw<InvalidOperationException>()
            .WithMessage("Schedule cannot be modified for a completed or cancelled session.");

        var sessionCancelled = Session.Create(_groupId, _startsAt, _endsAt);
        sessionCancelled.Cancel("Teacher sick", Guid.NewGuid(), DateTimeOffset.UtcNow);

        var actCancelled = () => sessionCancelled.UpdateSchedule(
            _startsAt.AddHours(1), _endsAt.AddHours(1), 15, Guid.NewGuid());

        actCancelled.Should().Throw<InvalidOperationException>()
            .WithMessage("Schedule cannot be modified for a completed or cancelled session.");
    }

    [Fact]
    public void Cancel_Sets_Cancellation_Reason_And_Audit_Fields()
    {
        var session = Session.Create(_groupId, _startsAt, _endsAt);
        var actorId = Guid.NewGuid();
        var cancelTime = DateTimeOffset.UtcNow;

        session.Cancel(" Emergency holiday ", actorId, cancelTime);

        session.Status.Should().Be(SessionStatus.Cancelled);
        session.CancellationReason.Should().Be("Emergency holiday");
        session.CancelledByUserId.Should().Be(actorId);
        session.CancelledAt.Should().Be(cancelTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Cancel_Throws_When_Reason_Is_Null_Or_Whitespace(string? invalidReason)
    {
        var session = Session.Create(_groupId, _startsAt, _endsAt);

        var act = () => session.Cancel(invalidReason!, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EvaluatePunctuality_Returns_Present_When_CheckIn_At_Or_Before_GraceDeadline()
    {
        // startsAt = 16:00:00, grace = 20 min -> deadline = 16:20:00
        var session = Session.Create(_groupId, _startsAt, _endsAt, lateGraceMinutes: 20);

        var checkInAtStart = _startsAt;
        var checkInAtDeadline = _startsAt.AddMinutes(20);

        session.EvaluatePunctuality(checkInAtStart).Should().Be(AttendanceStatus.Present);
        session.EvaluatePunctuality(checkInAtDeadline).Should().Be(AttendanceStatus.Present);
    }

    [Fact]
    public void EvaluatePunctuality_Returns_Late_When_CheckIn_One_Second_After_GraceDeadline()
    {
        // startsAt = 16:00:00, grace = 20 min -> deadline = 16:20:00
        var session = Session.Create(_groupId, _startsAt, _endsAt, lateGraceMinutes: 20);

        var checkInLate = _startsAt.AddMinutes(20).AddSeconds(1);

        session.EvaluatePunctuality(checkInLate).Should().Be(AttendanceStatus.Late);
    }
}
