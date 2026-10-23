using FluentAssertions;
using System.Net;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Entities;

public class SubscriptionPaymentTests
{
    [Fact]
    public void SubscriptionPayment_DefaultInitialization_SetsReceivedStatus()
    {
        // Act
        var payment = new SubscriptionPayment();

        // Assert
        payment.Id.Should().NotBeEmpty();
        payment.Method.Should().Be("Cash");
        payment.Status.Should().Be("Received");
        payment.ReversalReason.Should().BeNull();
        payment.PaidAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SubscriptionPayment_PropertyAssignment_PreservesProperties()
    {
        // Arrange
        var subId = Guid.NewGuid();
        var recorderId = Guid.NewGuid();
        var paidAt = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var payment = new SubscriptionPayment
        {
            SubscriptionId = subId,
            Amount = 2500m,
            PaidAt = paidAt,
            Method = "BankTransfer",
            Reference = "TXN-987654",
            RecordedByUserId = recorderId,
            Status = "Received",
            Notes = "Annual subscription advance payment"
        };

        // Assert
        payment.SubscriptionId.Should().Be(subId);
        payment.Amount.Should().Be(2500m);
        payment.PaidAt.Should().Be(paidAt);
        payment.Method.Should().Be("BankTransfer");
        payment.Reference.Should().Be("TXN-987654");
        payment.RecordedByUserId.Should().Be(recorderId);
        payment.Notes.Should().Be("Annual subscription advance payment");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reverse_WithEmptyReason_ThrowsDomainException(string? invalidReason)
    {
        // Arrange
        var payment = new SubscriptionPayment
        {
            SubscriptionId = Guid.NewGuid(),
            Amount = 1500m,
            Status = "Received"
        };

        // Act
        var act = () => payment.Reverse(invalidReason!);

        // Assert
        act.Should().Throw<DomainException>()
            .Where(e => e.ErrorCode == "REVERSAL_REASON_REQUIRED" && e.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void Reverse_WithValidReason_SetsReversedStatusAndTrimsReason()
    {
        // Arrange
        var payment = new SubscriptionPayment
        {
            SubscriptionId = Guid.NewGuid(),
            Amount = 1200m,
            Status = "Received"
        };

        // Act
        payment.Reverse("  Payment refunded per owner request  ");

        // Assert
        payment.Status.Should().Be("Reversed");
        payment.ReversalReason.Should().Be("Payment refunded per owner request");
        payment.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
