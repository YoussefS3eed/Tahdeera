using FluentAssertions;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Entities;

public class GroupWorkspaceAccessTests
{
    [Fact]
    public void Grant_FactoryMethod_InitializesAccessPermissions()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var granterUserId = Guid.NewGuid();

        // Act
        var access = GroupWorkspaceAccess.Grant(
            groupId: groupId,
            workspaceId: workspaceId,
            grantedByUserId: granterUserId,
            canViewRoster: true,
            canRegisterAttendance: true,
            canEditAttendance: false,
            notes: "  Delegate attendance registration to Host Center  ");

        // Assert
        access.Id.Should().NotBeEmpty();
        access.GroupId.Should().Be(groupId);
        access.WorkspaceId.Should().Be(workspaceId);
        access.GrantedByUserId.Should().Be(granterUserId);
        access.CanViewRoster.Should().BeTrue();
        access.CanRegisterAttendance.Should().BeTrue();
        access.CanEditAttendance.Should().BeFalse();
        access.Notes.Should().Be("Delegate attendance registration to Host Center");
        access.GrantedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        access.RevokedAt.Should().BeNull();
        access.RevokedByUserId.Should().BeNull();
    }

    [Fact]
    public void Revoke_WithValidTimestamp_RevokesAccess()
    {
        // Arrange
        var access = GroupWorkspaceAccess.Grant(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), canRegisterAttendance: true);
        var revokerUserId = Guid.NewGuid();
        var revokedAt = DateTimeOffset.UtcNow.AddMinutes(10);

        // Act
        access.Revoke(revokerUserId, revokedAt);

        // Assert
        access.RevokedAt.Should().Be(revokedAt);
        access.RevokedByUserId.Should().Be(revokerUserId);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsInvalidOperationException()
    {
        // Arrange
        var access = GroupWorkspaceAccess.Grant(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var revokerId = Guid.NewGuid();
        access.Revoke(revokerId, DateTimeOffset.UtcNow.AddMinutes(1));

        // Act
        var act = () => access.Revoke(revokerId, DateTimeOffset.UtcNow.AddMinutes(2));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Group workspace access is already revoked.");
    }

    [Fact]
    public void Revoke_WhenRevocationBeforeGrant_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var access = GroupWorkspaceAccess.Grant(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var pastTime = access.GrantedAt.AddMinutes(-5);

        // Act
        var act = () => access.Revoke(Guid.NewGuid(), pastTime);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("revokedAt");
    }

    [Fact]
    public void UpdatePermissions_WhenActive_UpdatesPermissionFlags()
    {
        // Arrange
        var access = GroupWorkspaceAccess.Grant(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), canViewRoster: false, canRegisterAttendance: false, canEditAttendance: false);

        // Act
        access.UpdatePermissions(canViewRoster: true, canRegisterAttendance: true, canEditAttendance: true);

        // Assert
        access.CanViewRoster.Should().BeTrue();
        access.CanRegisterAttendance.Should().BeTrue();
        access.CanEditAttendance.Should().BeTrue();
    }

    [Fact]
    public void UpdatePermissions_WhenRevoked_ThrowsInvalidOperationException()
    {
        // Arrange
        var access = GroupWorkspaceAccess.Grant(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        access.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(1));

        // Act
        var act = () => access.UpdatePermissions(true, true, true);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update permissions on revoked workspace access.");
    }
}
