using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.UnitTests.Finances;

public class StudentChargeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-100.50)]
    public void Create_WithNonPositiveAmount_ShouldThrowDomainRuleViolationException(decimal invalidAmount)
    {
        // Act
        var act = () => StudentCharge.Create(Guid.NewGuid(), "Tuition Fee", invalidAmount);

        // Assert
        var ex = act.Should().Throw<DomainRuleViolationException>().Which;
        ex.ErrorCode.Should().Be("AMOUNT_MUST_BE_POSITIVE");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    [InlineData(null)]
    public void Create_WithInvalidDescription_ShouldThrowDomainRuleViolationException(string? invalidDescription)
    {
        // Act
        var act = () => StudentCharge.Create(Guid.NewGuid(), invalidDescription!, 500m);

        // Assert
        var ex = act.Should().Throw<DomainRuleViolationException>().Which;
        ex.ErrorCode.Should().Be("CHARGE_DESCRIPTION_REQUIRED");
    }

    [Fact]
    public void Create_WithValidParameters_ShouldInitializeOpenCharge()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var charge = StudentCharge.Create(
            accountId,
            "October Physics Tuition",
            450m,
            groupId,
            "2026-10",
            DateTimeOffset.UtcNow.AddDays(7),
            "First month fee",
            userId);

        // Assert
        charge.Id.Should().NotBeEmpty();
        charge.StudentAccountId.Should().Be(accountId);
        charge.GroupId.Should().Be(groupId);
        charge.Description.Should().Be("October Physics Tuition");
        charge.Amount.Should().Be(450m);
        charge.BillingPeriodLabel.Should().Be("2026-10");
        charge.Status.Should().Be(ChargeStatus.Open);
        charge.CancelledAt.Should().BeNull();
        charge.CancellationReason.Should().BeNull();
        charge.Allocations.Should().BeEmpty();
    }

    [Fact]
    public void Cancel_WhenOpenAndReasonValid_ShouldTransitionToCancelled()
    {
        // Arrange
        var charge = StudentCharge.Create(Guid.NewGuid(), "October Tuition", 300m);
        var staffUserId = Guid.NewGuid();
        const string reason = "Student withdrew before class started";

        // Act
        charge.Cancel(reason, staffUserId);

        // Assert
        charge.Status.Should().Be(ChargeStatus.Cancelled);
        charge.CancelledByUserId.Should().Be(staffUserId);
        charge.CancellationReason.Should().Be(reason);
        charge.CancelledAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("four")]
    public void Cancel_WithShortReason_ShouldThrowDomainRuleViolationException(string shortReason)
    {
        // Arrange
        var charge = StudentCharge.Create(Guid.NewGuid(), "October Tuition", 300m);

        // Act
        var act = () => charge.Cancel(shortReason, Guid.NewGuid());

        // Assert
        var ex = act.Should().Throw<DomainRuleViolationException>().Which;
        ex.ErrorCode.Should().Be("CANCELLATION_REASON_TOO_SHORT");
    }

    [Fact]
    public void Cancel_WhenNotOpen_ShouldThrowDomainRuleViolationException()
    {
        // Arrange
        var charge = StudentCharge.Create(Guid.NewGuid(), "October Tuition", 300m);
        charge.Cancel("First cancellation reason", Guid.NewGuid());

        // Act
        var act = () => charge.Cancel("Second cancellation attempt", Guid.NewGuid());

        // Assert
        var ex = act.Should().Throw<DomainRuleViolationException>().Which;
        ex.ErrorCode.Should().Be("CANNOT_CANCEL_NON_OPEN_CHARGE");
    }

    [Fact]
    public void WriteOff_WhenOpenAndReasonValid_ShouldTransitionToWrittenOff()
    {
        // Arrange
        var charge = StudentCharge.Create(Guid.NewGuid(), "October Tuition", 300m);
        var staffUserId = Guid.NewGuid();
        const string reason = "Orphan student hardship full waiver";

        // Act
        charge.WriteOff(reason, staffUserId);

        // Assert
        charge.Status.Should().Be(ChargeStatus.WrittenOff);
        charge.CancelledByUserId.Should().Be(staffUserId);
        charge.CancellationReason.Should().Be(reason);
        charge.CancelledAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void WriteOff_WhenAlreadyWrittenOffOrCancelled_ShouldThrowDomainRuleViolationException()
    {
        // Arrange
        var charge = StudentCharge.Create(Guid.NewGuid(), "October Tuition", 300m);
        charge.WriteOff("First write-off reason", Guid.NewGuid());

        // Act
        var act = () => charge.WriteOff("Second write-off attempt", Guid.NewGuid());

        // Assert
        var ex = act.Should().Throw<DomainRuleViolationException>().Which;
        ex.ErrorCode.Should().Be("CANNOT_WRITEOFF_NON_OPEN_CHARGE");
    }
}
