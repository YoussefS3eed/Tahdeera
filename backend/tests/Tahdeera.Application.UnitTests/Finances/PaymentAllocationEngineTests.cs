using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Finances.Services;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;
using Xunit;

namespace Tahdeera.Application.UnitTests.Finances;

public class PaymentAllocationEngineTests
{
    private readonly IStudentChargeRepository _chargeRepository;
    private readonly PaymentAllocationEngine _engine;

    public PaymentAllocationEngineTests()
    {
        _chargeRepository = Substitute.For<IStudentChargeRepository>();
        _engine = new PaymentAllocationEngine(_chargeRepository);
    }

    [Fact]
    public async Task Fifo_SettlesOldestChargeFirst_AndLeavesRemainingForNext()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var charge1 = StudentCharge.Create(accountId, "Charge 1 (Oldest)", 200m, dueAt: DateTimeOffset.UtcNow.AddDays(-10));
        var charge2 = StudentCharge.Create(accountId, "Charge 2 (Newer)", 300m, dueAt: DateTimeOffset.UtcNow.AddDays(5));

        _chargeRepository.GetOpenChargesByAccountIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge> { charge1, charge2 });

        var payment = StudentPayment.Create(accountId, "REC-001", 350m, PaymentMethod.Cash, Guid.NewGuid());

        // Act
        var result = await _engine.AllocatePaymentAsync(payment, accountId, null, CancellationToken.None);

        // Assert
        result.TotalAllocated.Should().Be(350m);
        result.UnallocatedCredit.Should().Be(0m);
        result.Allocations.Should().HaveCount(2);

        result.Allocations[0].ChargeId.Should().Be(charge1.Id);
        result.Allocations[0].Amount.Should().Be(200m);

        result.Allocations[1].ChargeId.Should().Be(charge2.Id);
        result.Allocations[1].Amount.Should().Be(150m);
    }

    [Fact]
    public async Task Fifo_PartialPayment_ReducesRemainingBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var charge = StudentCharge.Create(accountId, "Tuition", 500m);

        _chargeRepository.GetOpenChargesByAccountIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge> { charge });

        var payment = StudentPayment.Create(accountId, "REC-002", 200m, PaymentMethod.Wallet, Guid.NewGuid());

        // Act
        var result = await _engine.AllocatePaymentAsync(payment, accountId, null, CancellationToken.None);

        // Assert
        result.TotalAllocated.Should().Be(200m);
        result.UnallocatedCredit.Should().Be(0m);
        result.Allocations.Should().HaveCount(1);
        result.Allocations[0].Amount.Should().Be(200m);
    }

    [Fact]
    public async Task Fifo_Overpayment_LeavesUnallocatedCredit()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var charge = StudentCharge.Create(accountId, "Tuition", 150m);

        _chargeRepository.GetOpenChargesByAccountIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge> { charge });

        var payment = StudentPayment.Create(accountId, "REC-003", 500m, PaymentMethod.BankTransfer, Guid.NewGuid());

        // Act
        var result = await _engine.AllocatePaymentAsync(payment, accountId, null, CancellationToken.None);

        // Assert
        result.TotalAllocated.Should().Be(150m);
        result.UnallocatedCredit.Should().Be(350m);
        result.Allocations.Should().HaveCount(1);
        result.Allocations[0].Amount.Should().Be(150m);
    }

    [Fact]
    public async Task Explicit_WhenAllocatedExceedsChargeRemainingBalance_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var charge = StudentCharge.Create(accountId, "Tuition", 200m);

        _chargeRepository.GetByIdAsync(charge.Id, Arg.Any<CancellationToken>())
            .Returns(charge);

        var payment = StudentPayment.Create(accountId, "REC-004", 300m, PaymentMethod.Card, Guid.NewGuid());
        var explicitAllocations = new List<PaymentAllocationRequestDto>
        {
            new(charge.Id, 250m) // Exceeds 200m charge
        };

        // Act
        var act = () => _engine.AllocatePaymentAsync(payment, accountId, explicitAllocations, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("ALLOCATION_EXCEEDS_CHARGE_BALANCE");
    }

    [Fact]
    public async Task Explicit_WhenAllocationsSumExceedsPaymentAmount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var charge = StudentCharge.Create(accountId, "Tuition", 500m);

        var payment = StudentPayment.Create(accountId, "REC-005", 200m, PaymentMethod.Cash, Guid.NewGuid());
        var explicitAllocations = new List<PaymentAllocationRequestDto>
        {
            new(charge.Id, 300m) // Exceeds 200m payment
        };

        // Act
        var act = () => _engine.AllocatePaymentAsync(payment, accountId, explicitAllocations, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("ALLOCATION_EXCEEDS_PAYMENT");
    }
}
