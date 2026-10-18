using FluentAssertions;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests;

public class WorkspaceSubscriptionTests
{
    [Fact]
    public void IsLockedOut_WhenSubscriptionIsActiveAndNotExpired_ReturnsFalse()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var sub = new WorkspaceSubscription
        {
            StartsAt = now.AddDays(-10),
            EndsAt = now.AddDays(10),
            Status = "Active"
        };

        // Act & Assert
        sub.IsLockedOut(now).Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_WhenSubscriptionIsExpired_ReturnsTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var sub = new WorkspaceSubscription
        {
            StartsAt = now.AddDays(-20),
            EndsAt = now.AddDays(-1),
            Status = "Active"
        };

        // Act & Assert
        sub.IsLockedOut(now).Should().BeTrue();
    }

    [Fact]
    public void IsLockedOut_WhenManualSuspensionIsApplied_ReturnsTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var adminId = Guid.NewGuid();
        var sub = new WorkspaceSubscription
        {
            StartsAt = now.AddDays(-10),
            EndsAt = now.AddDays(10),
            Status = "Active"
        };

        // Act
        sub.Suspend(adminId, "Payment overdue", now);

        // Assert
        sub.IsLockedOut(now).Should().BeTrue();
        sub.ManualSuspendedAt.Should().Be(now);
        sub.ManualSuspendedByUserId.Should().Be(adminId);
        sub.SuspensionReason.Should().Be("Payment overdue");
    }

    [Fact]
    public void Suspend_WithEmptyReason_ThrowsDomainException()
    {
        // Arrange
        var sub = new WorkspaceSubscription();

        // Act
        var act = () => sub.Suspend(Guid.NewGuid(), "   ", DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Suspension reason cannot be empty*");
    }

    [Fact]
    public void Reactivate_ClearsSuspension()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var sub = new WorkspaceSubscription
        {
            StartsAt = now.AddDays(-10),
            EndsAt = now.AddDays(10),
            Status = "Active"
        };
        sub.Suspend(Guid.NewGuid(), "Administrative review", now);

        // Act
        sub.Reactivate(now);

        // Assert
        sub.IsLockedOut(now).Should().BeFalse();
        sub.ManualSuspendedAt.Should().BeNull();
        sub.SuspensionReason.Should().BeNull();
    }

    [Fact]
    public void Renew_ExtendsSubscription_AndSetsAgreedPrice()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var sub = new WorkspaceSubscription
        {
            StartsAt = now.AddDays(-30),
            EndsAt = now.AddDays(10),
            Status = "Active",
            AgreedPrice = 500m
        };

        // Act
        sub.Renew(365, 1200m, now);

        // Assert
        sub.EndsAt.Should().Be(now.AddDays(10).AddDays(365));
        sub.AgreedPrice.Should().Be(1200m);
        sub.Status.Should().Be("Active");
    }
}
