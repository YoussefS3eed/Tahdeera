using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.UnitTests.Finances;

public class StudentPaymentTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Create_WithNonPositiveAmount_ShouldThrowDomainRuleViolationException(decimal invalidAmount)
    {
        // Act
        var act = () => StudentPayment.Create(
            Guid.NewGuid(),
            "REC-202610-7B4F52-00001",
            invalidAmount,
            PaymentMethod.Cash,
            Guid.NewGuid());

        // Assert
        var ex = act.Should().Throw<DomainRuleViolationException>().Which;
        ex.ErrorCode.Should().Be("AMOUNT_MUST_BE_POSITIVE");
    }

    [Fact]
    public void Create_WithFutureDate_ShouldThrowDomainRuleViolationException()
    {
        // Act
        var act = () => StudentPayment.Create(
            Guid.NewGuid(),
            "REC-202610-7B4F52-00001",
            500m,
            PaymentMethod.Cash,
            Guid.NewGuid(),
            paidAt: DateTimeOffset.UtcNow.AddMinutes(10));

        // Assert
        var ex = act.Should().Throw<DomainRuleViolationException>().Which;
        ex.ErrorCode.Should().Be("PAYMENT_DATE_CANNOT_BE_FUTURE");
    }

    [Fact]
    public void Create_WithValidParameters_ShouldInitializeReceivedPayment()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        // Act
        var payment = StudentPayment.Create(
            accountId,
            "REC-202610-7B4F52-00001",
            500m,
            PaymentMethod.Wallet, // Wallet
            cashierId,
            reference: "TXN-883921",
            notes: "Parent paid via cashier");

        // Assert
        payment.Id.Should().NotBeEmpty();
        payment.StudentAccountId.Should().Be(accountId);
        payment.ReceiptNumber.Should().Be("REC-202610-7B4F52-00001");
        payment.Amount.Should().Be(500m);
        payment.RecordedByUserId.Should().Be(cashierId);
        payment.Status.Should().Be(PaymentStatus.Received);
        payment.ReversedAt.Should().BeNull();
        payment.ReversalReason.Should().BeNull();
        payment.Allocations.Should().BeEmpty();
    }

    [Fact]
    public void Reverse_WhenReceivedAndReasonValid_ShouldTransitionToReversed_AndCascadeToAllocations()
    {
        // Arrange
        var payment = StudentPayment.Create(
            Guid.NewGuid(),
            "REC-202610-7B4F52-00001",
            500m,
            PaymentMethod.Cash,
            Guid.NewGuid());

        var chargeId = Guid.NewGuid();
        var allocation = PaymentAllocation.Create(payment.Id, chargeId, 300m);
        payment.AddAllocation(allocation);

        var staffUserId = Guid.NewGuid();
        const string reason = "Cashier entered wrong amount, entered 500 instead of 50";

        // Act
        payment.Reverse(reason, staffUserId);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Reversed);
        payment.ReversedByUserId.Should().Be(staffUserId);
        payment.ReversalReason.Should().Be(reason);
        payment.ReversedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        allocation.IsReversed.Should().BeTrue("Payment reversal must cascade and void all child line-item allocations");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad")]
    [InlineData("four")]
    public void Reverse_WithShortReason_ShouldThrowDomainRuleViolationException(string shortReason)
    {
        // Arrange
        var payment = StudentPayment.Create(
            Guid.NewGuid(),
            "REC-202610-7B4F52-00001",
            500m,
            PaymentMethod.Cash,
            Guid.NewGuid());

        // Act
        var act = () => payment.Reverse(shortReason, Guid.NewGuid());

        // Assert
        var ex = act.Should().Throw<DomainRuleViolationException>().Which;
        ex.ErrorCode.Should().Be("REVERSAL_REASON_TOO_SHORT");
    }

    [Fact]
    public void Reverse_WhenAlreadyReversed_ShouldThrowDomainRuleViolationException()
    {
        // Arrange
        var payment = StudentPayment.Create(
            Guid.NewGuid(),
            "REC-202610-7B4F52-00001",
            500m,
            PaymentMethod.Cash,
            Guid.NewGuid());

        payment.Reverse("First reversal reason entered", Guid.NewGuid());

        // Act
        var act = () => payment.Reverse("Second reversal attempt", Guid.NewGuid());

        // Assert
        var ex = act.Should().Throw<DomainRuleViolationException>().Which;
        ex.ErrorCode.Should().Be("PAYMENT_ALREADY_REVERSED");
    }
}
