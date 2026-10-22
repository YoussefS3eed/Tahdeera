using System.Net;
using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Queries.GetPrintableReceipt;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Xunit;

namespace Tahdeera.Application.UnitTests.Finances;

public class GetPrintableReceiptQueryHandlerTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly GetPrintableReceiptHandler _handler;

    public GetPrintableReceiptQueryHandlerTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _handler = new GetPrintableReceiptHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_ValidPayment_ReturnsFormattedPrintableReceiptDto()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();

        var workspace = new Workspace
        {
            Id = workspaceId,
            Name = "Al-Najah Center",
            ContactPhone = "01012345678",
            CityAddress = "Nasr City, Cairo"
        };

        var student = new Student
        {
            Id = studentId,
            StudentCode = "STD-1001",
            FullName = "Ahmed Mahmoud"
        };

        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            SeatNumber = "A-15",
            Workspace = workspace,
            Student = student,
            Status = WorkspaceStudentStatus.Active
        };

        var account = StudentAccount.Create(ws.Id);

        var cashier = new AppUser
        {
            Id = cashierId,
            DisplayName = "Sara Ibrahim"
        };

        var payment = StudentPayment.Create(
            account.Id,
            "REC-202610-0042",
            450.00m,
            PaymentMethod.Cash,
            cashierId,
            paidAt: DateTimeOffset.UtcNow);

        typeof(StudentPayment).GetProperty("RecordedByUser")!.SetValue(payment, cashier);

        var charge = StudentCharge.Create(
            account.Id,
            "October Tuition",
            450.00m,
            billingPeriodLabel: "2026-10");

        var alloc = PaymentAllocation.Create(payment.Id, charge.Id, 450.00m);
        typeof(PaymentAllocation).GetProperty("Charge")!.SetValue(alloc, charge);
        payment.AddAllocation(alloc);

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var accountDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { account });
        var paymentDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment });
        var chargeDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { charge });

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(accountDbSet);
        _dbContext.StudentPayments.Returns(paymentDbSet);
        _dbContext.StudentCharges.Returns(chargeDbSet);

        var query = new GetPrintableReceiptQuery(workspaceId, studentId, payment.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ReceiptNumber.Should().Be("REC-202610-0042");
        result.WorkspaceName.Should().Be("Al-Najah Center");
        result.StudentCode.Should().Be("STD-1001");
        result.StudentName.Should().Be("Ahmed Mahmoud");
        result.SeatNumber.Should().Be("A-15");
        result.Amount.Should().Be(450.00m);
        result.AmountInWordsArabic.Should().Be("فقط أربعمائة وخمسون جنيهاً مصرياً لا غير");
        result.CashierName.Should().Be("Sara Ibrahim");
        result.BalanceStatus.Should().Be("Settled");
        result.RemainingAccountBalance.Should().Be(0.00m);
        result.VerificationHash.Should().NotBeNullOrWhiteSpace();
        result.VerificationPath.Should().StartWith("/verify-receipt/");
        result.Settlements.Should().HaveCount(1);
        result.Settlements[0].AllocatedAmount.Should().Be(450.00m);
        result.Settlements[0].BillingPeriodLabel.Should().Be("2026-10");
    }

    [Fact]
    public async Task Handle_ReversedPayment_ThrowsCannotPrintReversedPayment()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = studentId };
        var account = StudentAccount.Create(ws.Id);

        var payment = StudentPayment.Create(
            account.Id,
            "REC-202610-0099",
            300.00m,
            PaymentMethod.Cash,
            Guid.NewGuid());

        payment.Reverse("Mistaken entry", Guid.NewGuid());

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var accountDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { account });
        var paymentDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment });
        var chargeDbSet = AsyncQueryTestHelpers.CreateMockDbSet(Array.Empty<StudentCharge>());

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(accountDbSet);
        _dbContext.StudentPayments.Returns(paymentDbSet);
        _dbContext.StudentCharges.Returns(chargeDbSet);

        var query = new GetPrintableReceiptQuery(workspaceId, studentId, payment.Id);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("CANNOT_PRINT_REVERSED_PAYMENT");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ThrowsPaymentNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = studentId };
        var account = StudentAccount.Create(ws.Id);

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var accountDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { account });
        var paymentDbSet = AsyncQueryTestHelpers.CreateMockDbSet(Array.Empty<StudentPayment>());
        var chargeDbSet = AsyncQueryTestHelpers.CreateMockDbSet(Array.Empty<StudentCharge>());

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(accountDbSet);
        _dbContext.StudentPayments.Returns(paymentDbSet);
        _dbContext.StudentCharges.Returns(chargeDbSet);

        var query = new GetPrintableReceiptQuery(workspaceId, studentId, Guid.NewGuid());

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("PAYMENT_NOT_FOUND");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
