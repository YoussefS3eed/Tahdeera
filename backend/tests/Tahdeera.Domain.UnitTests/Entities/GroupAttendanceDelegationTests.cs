using FluentAssertions;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Entities;

public class GroupAttendanceDelegationTests
{
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _assistantId = Guid.NewGuid();
    private readonly Guid _teacherId = Guid.NewGuid();

    [Fact]
    public void Create_Initializes_Active_Delegation_With_Null_Revocation()
    {
        var delegation = GroupAttendanceDelegation.Create(_groupId, _assistantId, _teacherId);

        delegation.Id.Should().NotBeEmpty();
        delegation.GroupId.Should().Be(_groupId);
        delegation.AssistantUserId.Should().Be(_assistantId);
        delegation.GrantedByTeacherUserId.Should().Be(_teacherId);
        delegation.RevokedAt.Should().BeNull();
        delegation.RevokedByUserId.Should().BeNull();
        delegation.RevocationReason.Should().BeNull();
        delegation.GrantedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Revoke_Sets_Revocation_Fields_And_Reason()
    {
        var delegation = GroupAttendanceDelegation.Create(_groupId, _assistantId, _teacherId);
        var revokerId = Guid.NewGuid();
        var revokeTime = DateTimeOffset.UtcNow.AddHours(1);

        delegation.Revoke(revokerId, " Assistant resigned ", revokeTime);

        delegation.RevokedAt.Should().Be(revokeTime);
        delegation.RevokedByUserId.Should().Be(revokerId);
        delegation.RevocationReason.Should().Be("Assistant resigned");
    }

    [Fact]
    public void Revoke_Throws_When_Already_Revoked()
    {
        var delegation = GroupAttendanceDelegation.Create(_groupId, _assistantId, _teacherId);
        var revokerId = Guid.NewGuid();
        delegation.Revoke(revokerId, "Reason 1", DateTimeOffset.UtcNow);

        var act = () => delegation.Revoke(revokerId, "Reason 2", DateTimeOffset.UtcNow.AddMinutes(5));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Attendance delegation is already revoked.");
    }

    [Fact]
    public void Revoke_Throws_When_RevokedAt_Before_GrantedAt()
    {
        var delegation = GroupAttendanceDelegation.Create(_groupId, _assistantId, _teacherId);
        var revokerId = Guid.NewGuid();
        var pastTime = delegation.GrantedAt.AddSeconds(-10);

        var act = () => delegation.Revoke(revokerId, "Invalid time", pastTime);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("revokedAt");
    }
}
