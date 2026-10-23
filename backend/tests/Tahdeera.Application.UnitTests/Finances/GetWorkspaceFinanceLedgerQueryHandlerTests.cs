using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceLedger;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Finances;

public class GetWorkspaceFinanceLedgerQueryHandlerTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly GetWorkspaceFinanceLedgerHandler _handler;

    public GetWorkspaceFinanceLedgerQueryHandlerTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _handler = new GetWorkspaceFinanceLedgerHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_OrdersItemsChronologicallyDescending()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), FullName = "طارق عمر", StudentCode = "ST-501" };
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student };
        var account = StudentAccount.Create(ws.Id);
        ws.StudentAccount = account;

        var user = new AppUser { Id = Guid.NewGuid(), DisplayName = "أحمد المحاسب", Username = "cashier1" };

        var charge = StudentCharge.Create(account.Id, "October Tuition", 400m);
        // Set CreatedAt of charge to 2 hours ago
        typeof(StudentCharge).GetProperty("CreatedAt")!.SetValue(charge, DateTimeOffset.UtcNow.AddHours(-2));
        typeof(StudentCharge).GetProperty("CreatedByUser")!.SetValue(charge, user);

        var payment = StudentPayment.Create(
            account.Id,
            "REC-202610-001",
            400m,
            PaymentMethod.Cash,
            user.Id,
            paidAt: DateTimeOffset.UtcNow.AddHours(-1));
        typeof(StudentPayment).GetProperty("RecordedByUser")!.SetValue(payment, user);

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var chargesDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { charge });
        var paymentsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment });

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentCharges.Returns(chargesDbSet);
        _dbContext.StudentPayments.Returns(paymentsDbSet);

        var query = new GetWorkspaceFinanceLedgerQuery(workspaceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);

        // Newer item (payment at -1h) should come before older item (charge at -2h)
        result.Items[0].Id.Should().Be("REC-202610-001");
        result.Items[0].Type.Should().Be("Payment");
        result.Items[0].RecordedByUserName.Should().Be("أحمد المحاسب");

        result.Items[1].Id.Should().Be(charge.Id.ToString());
        result.Items[1].Type.Should().Be("Charge");
        result.Items[1].RecordedByUserName.Should().Be("أحمد المحاسب");
    }

    [Fact]
    public async Task Handle_WithTransactionTypeFilter_FiltersCorrectly()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), FullName = "منى سعيد", StudentCode = "ST-502" };
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student };
        var account = StudentAccount.Create(ws.Id);
        ws.StudentAccount = account;

        var charge = StudentCharge.Create(account.Id, "Fee", 250m);
        var payment = StudentPayment.Create(account.Id, "REC-502", 250m, PaymentMethod.Wallet, Guid.NewGuid());

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var chargesDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { charge });
        var paymentsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment });

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentCharges.Returns(chargesDbSet);
        _dbContext.StudentPayments.Returns(paymentsDbSet);

        var query = new GetWorkspaceFinanceLedgerQuery(workspaceId, TransactionType: "Payment");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be("REC-502");
        result.Items[0].Type.Should().Be("Payment");
    }

    [Fact]
    public async Task Handle_PreservesReversalReasonAndStatus()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), FullName = "حسن إبراهيم", StudentCode = "ST-503" };
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student };
        var account = StudentAccount.Create(ws.Id);
        ws.StudentAccount = account;

        var payment = StudentPayment.Create(account.Id, "REC-REV-001", 350m, PaymentMethod.Cash, Guid.NewGuid());
        payment.Reverse("Recorded by mistake on wrong student", Guid.NewGuid());

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var chargesDbSet = AsyncQueryTestHelpers.CreateMockDbSet(Array.Empty<StudentCharge>());
        var paymentsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment });

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentCharges.Returns(chargesDbSet);
        _dbContext.StudentPayments.Returns(paymentsDbSet);

        var query = new GetWorkspaceFinanceLedgerQuery(workspaceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Reversed");
        result.Items[0].Type.Should().Be("Reversal");
        result.Items[0].ReversalReason.Should().Be("Recorded by mistake on wrong student");
    }
}
