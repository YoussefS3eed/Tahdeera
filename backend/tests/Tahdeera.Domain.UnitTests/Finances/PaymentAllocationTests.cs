using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.UnitTests.Finances;

public class PaymentAllocationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Create_WithNonPositiveAmount_ShouldThrowDomainRuleViolationException(decimal invalidAmount)
    {
        // Act
        var act = () => PaymentAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), invalidAmount);

        // Assert
        var ex = act.Should().Throw<DomainRuleViolationException>().Which;
        ex.ErrorCode.Should().Be("AMOUNT_MUST_BE_POSITIVE");
    }

    [Fact]
    public void Create_WithValidAmount_ShouldInitializeActiveAllocation()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var chargeId = Guid.NewGuid();

        // Act
        var allocation = PaymentAllocation.Create(paymentId, chargeId, 250m);

        // Assert
        allocation.Id.Should().NotBeEmpty();
        allocation.PaymentId.Should().Be(paymentId);
        allocation.ChargeId.Should().Be(chargeId);
        allocation.Amount.Should().Be(250m);
        allocation.IsReversed.Should().BeFalse();
        allocation.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Reverse_ShouldSetIsReversedToTrue()
    {
        // Arrange
        var allocation = PaymentAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), 250m);

        // Act
        allocation.Reverse();

        // Assert
        allocation.IsReversed.Should().BeTrue();
    }
}
