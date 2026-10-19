using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.UnitTests.Entities;

public class AttendanceRecordTests
{
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly DateTimeOffset _sessionStart = new(2026, 10, 20, 16, 0, 0, TimeSpan.Zero);
    private readonly DateTimeOffset _sessionEnd = new(2026, 10, 20, 18, 0, 0, TimeSpan.Zero);

    private Session CreateSampleSession(int graceMinutes = 20)
    {
        return Session.Create(_groupId, _sessionStart, _sessionEnd, lateGraceMinutes: graceMinutes);
    }

    [Fact]
    public void CreateCheckIn_Evaluates_Punctuality_And_Sets_CheckInAt()
    {
        var session = CreateSampleSession(20);
        var checkInTime = _sessionStart.AddMinutes(10);
        var userId = Guid.NewGuid();

        var record = AttendanceRecord.CreateCheckIn(session, _studentId, _workspaceId, checkInTime, userId);

        record.Id.Should().NotBeEmpty();
        record.SessionId.Should().Be(session.Id);
        record.StudentId.Should().Be(_studentId);
        record.OwnerWorkspaceId.Should().Be(_workspaceId);
        record.Status.Should().Be(AttendanceStatus.Present);
        record.Source.Should().Be(AttendanceSource.User);
        record.CheckInAt.Should().Be(checkInTime);
        record.RecordedByUserId.Should().Be(userId);
        record.AutoEvaluationLocked.Should().BeFalse();
        record.IsGuestMakeup.Should().BeFalse();
        record.HomeGroupId.Should().BeNull();
    }

    [Fact]
    public void CreateSystemAbsence_Sets_Absent_And_System_Source()
    {
        var sessionId = Guid.NewGuid();
        var systemNow = DateTimeOffset.UtcNow;

        var record = AttendanceRecord.CreateSystemAbsence(sessionId, _studentId, _workspaceId, systemNow);

        record.SessionId.Should().Be(sessionId);
        record.Status.Should().Be(AttendanceStatus.Absent);
        record.Source.Should().Be(AttendanceSource.System);
        record.RecordedByUserId.Should().BeNull();
        record.SystemGeneratedAt.Should().Be(systemNow);
        record.CheckInAt.Should().BeNull();
        record.AutoEvaluationLocked.Should().BeFalse();
    }

    [Fact]
    public void RegisterMakeup_Sets_IsGuestMakeup_True_And_HomeGroupId()
    {
        var session = CreateSampleSession(20);
        var homeGroupId = Guid.NewGuid();
        var checkInTime = _sessionStart.AddMinutes(5);

        var record = AttendanceRecord.RegisterMakeup(session, _studentId, _workspaceId, homeGroupId, checkInTime, Guid.NewGuid());

        record.IsGuestMakeup.Should().BeTrue();
        record.HomeGroupId.Should().Be(homeGroupId);
        record.Status.Should().Be(AttendanceStatus.Present);
    }

    [Fact]
    public void ManualOverride_Sets_AutoEvaluationLocked_True_And_Updates_LastModified()
    {
        var session = CreateSampleSession(20);
        var record = AttendanceRecord.CreateCheckIn(session, _studentId, _workspaceId, _sessionStart, null);

        var actorId = Guid.NewGuid();
        var modifiedAt = DateTimeOffset.UtcNow;

        record.ManualOverride(AttendanceStatus.Excused, actorId, modifiedAt, "Doctor note provided");

        record.Status.Should().Be(AttendanceStatus.Excused);
        record.AutoEvaluationLocked.Should().BeTrue();
        record.LastModifiedByUserId.Should().Be(actorId);
        record.LastModifiedAt.Should().Be(modifiedAt);
    }

    [Fact]
    public void ReevaluatePunctuality_Updates_Status_When_Unlocked()
    {
        // Initially grace is 10 min, check in at 15 min -> Late
        var session = CreateSampleSession(10);
        var checkInTime = _sessionStart.AddMinutes(15);
        var record = AttendanceRecord.CreateCheckIn(session, _studentId, _workspaceId, checkInTime, null);
        record.Status.Should().Be(AttendanceStatus.Late);

        // Teacher updates grace to 30 min -> check in at 15 min is now Present
        session.UpdateSchedule(_sessionStart, _sessionEnd, 30, Guid.NewGuid());
        record.ReevaluatePunctuality(session);

        record.Status.Should().Be(AttendanceStatus.Present);
    }

    [Fact]
    public void ReevaluatePunctuality_Ignores_When_AutoEvaluationLocked()
    {
        var session = CreateSampleSession(10);
        var checkInTime = _sessionStart.AddMinutes(15);
        var record = AttendanceRecord.CreateCheckIn(session, _studentId, _workspaceId, checkInTime, null);

        // Lock record via manual override
        record.ManualOverride(AttendanceStatus.Excused, Guid.NewGuid(), DateTimeOffset.UtcNow, "Manual excuse");

        // Schedule change happens
        session.UpdateSchedule(_sessionStart, _sessionEnd, 30, Guid.NewGuid());
        record.ReevaluatePunctuality(session);

        // Must stay Excused, not overwritten by auto-evaluation
        record.Status.Should().Be(AttendanceStatus.Excused);
    }

    [Fact]
    public void Void_Sets_VoidedAt_And_VoidReason()
    {
        var session = CreateSampleSession(20);
        var record = AttendanceRecord.CreateCheckIn(session, _studentId, _workspaceId, _sessionStart, null);
        var voidTime = DateTimeOffset.UtcNow;

        record.Void(" Session cancelled by admin ", voidTime);

        record.VoidedAt.Should().Be(voidTime);
        record.VoidReason.Should().Be("Session cancelled by admin");
        record.LastModifiedAt.Should().Be(voidTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Void_Throws_When_Reason_Is_Empty(string? invalidReason)
    {
        var session = CreateSampleSession(20);
        var record = AttendanceRecord.CreateCheckIn(session, _studentId, _workspaceId, _sessionStart, null);

        var act = () => record.Void(invalidReason!, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }
}
