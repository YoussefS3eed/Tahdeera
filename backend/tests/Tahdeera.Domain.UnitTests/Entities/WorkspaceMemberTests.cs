using FluentAssertions;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Entities;

public class WorkspaceMemberTests
{
    [Fact]
    public void WorkspaceMember_DefaultInitialization_SetsActiveTeacher()
    {
        // Act
        var member = new WorkspaceMember();

        // Assert
        member.Id.Should().NotBeEmpty();
        member.Role.Should().Be("Teacher");
        member.IsActive.Should().BeTrue();
        member.JoinedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        member.LeftAt.Should().BeNull();
        member.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_SetsInactiveAndRecordsLeftAtAndUpdatedAt()
    {
        // Arrange
        var member = new WorkspaceMember
        {
            WorkspaceId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = "Assistant"
        };
        var deactivationTime = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        member.Deactivate(deactivationTime);

        // Assert
        member.IsActive.Should().BeFalse();
        member.LeftAt.Should().Be(deactivationTime);
        member.UpdatedAt.Should().Be(deactivationTime);
    }

    [Fact]
    public void Reactivate_RestoresActiveStateAndClearsLeftAt()
    {
        // Arrange
        var member = new WorkspaceMember
        {
            WorkspaceId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = "CenterAdmin"
        };
        member.Deactivate(DateTimeOffset.UtcNow.AddDays(-5));

        // Act
        member.Reactivate();

        // Assert
        member.IsActive.Should().BeTrue();
        member.LeftAt.Should().BeNull();
        member.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
