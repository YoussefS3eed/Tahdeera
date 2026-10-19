using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.UnitTests.Entities;

public class GroupStudentTests
{
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly DateTimeOffset _joinedAt = DateTimeOffset.UtcNow.AddMonths(-1);

    [Fact]
    public void Create_Sets_Status_Active_And_LeftAt_Null()
    {
        var enrollment = GroupStudent.Create(_groupId, _studentId, _joinedAt);

        enrollment.Id.Should().NotBeEmpty();
        enrollment.GroupId.Should().Be(_groupId);
        enrollment.StudentId.Should().Be(_studentId);
        enrollment.Status.Should().Be(GroupStudentStatus.Active);
        enrollment.JoinedAt.Should().Be(_joinedAt);
        enrollment.LeftAt.Should().BeNull();
    }

    [Fact]
    public void Withdraw_Sets_Status_Withdrawn_And_LeftAt_Timestamp()
    {
        var enrollment = GroupStudent.Create(_groupId, _studentId, _joinedAt);
        var actorId = Guid.NewGuid();
        var leftAt = DateTimeOffset.UtcNow;

        enrollment.Withdraw(leftAt, actorId);

        enrollment.Status.Should().Be(GroupStudentStatus.Withdrawn);
        enrollment.LeftAt.Should().Be(leftAt);
        enrollment.UpdatedByUserId.Should().Be(actorId);
        enrollment.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Withdraw_Throws_When_LeftAt_Before_JoinedAt()
    {
        var enrollment = GroupStudent.Create(_groupId, _studentId, _joinedAt);
        var invalidLeftAt = _joinedAt.AddMinutes(-5);

        var act = () => enrollment.Withdraw(invalidLeftAt, Guid.NewGuid());

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("leftAt");
    }

    [Fact]
    public void Suspend_Sets_Status_Suspended_Without_Setting_LeftAt()
    {
        var enrollment = GroupStudent.Create(_groupId, _studentId, _joinedAt);
        var actorId = Guid.NewGuid();

        enrollment.Suspend(actorId);

        enrollment.Status.Should().Be(GroupStudentStatus.Suspended);
        enrollment.LeftAt.Should().BeNull();
        enrollment.UpdatedByUserId.Should().Be(actorId);
    }

    [Fact]
    public void Reinstate_Resets_Status_Active_And_Clears_LeftAt()
    {
        var enrollment = GroupStudent.Create(_groupId, _studentId, _joinedAt);
        enrollment.Withdraw(DateTimeOffset.UtcNow, Guid.NewGuid());
        enrollment.LeftAt.Should().NotBeNull();

        var actorId = Guid.NewGuid();
        enrollment.Reinstate(actorId);

        enrollment.Status.Should().Be(GroupStudentStatus.Active);
        enrollment.LeftAt.Should().BeNull();
        enrollment.UpdatedByUserId.Should().Be(actorId);
    }
}
