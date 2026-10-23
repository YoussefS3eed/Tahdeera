using FluentAssertions;
using NSubstitute;
using System.Net;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Queries.GetStudentAccountStatement;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Application.UnitTests.Finances;

public class GetStudentAccountStatementQueryHandlerTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly GetStudentAccountStatementHandler _handler;

    public GetStudentAccountStatementQueryHandlerTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _handler = new GetStudentAccountStatementHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_WhenStudentHasOpenChargesAndPayments_CalculatesInArrearsBalance()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var student = new Student { Id = studentId, FullName = "Karim Tarek" };
        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Student = student,
            SeatNumber = "B-12",
            Status = WorkspaceStudentStatus.Active
        };

        var account = StudentAccount.Create(ws.Id);
        var charge1 = StudentCharge.Create(account.Id, "Tuition Oct", 300m);
        var charge2 = StudentCharge.Create(account.Id, "Materials Fee", 200m);

        var payment = StudentPayment.Create(account.Id, "REC-202610-0010", 350m, PaymentMethod.Cash, Guid.NewGuid());
        var alloc1 = PaymentAllocation.Create(payment.Id, charge1.Id, 300m);
        var alloc2 = PaymentAllocation.Create(payment.Id, charge2.Id, 50m);
        payment.AddAllocation(alloc1);
        payment.AddAllocation(alloc2);

        var chargesField = typeof(StudentAccount).GetField("_charges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        ((List<StudentCharge>)chargesField.GetValue(account)!).AddRange(new[] { charge1, charge2 });

        var paymentsField = typeof(StudentAccount).GetField("_payments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        ((List<StudentPayment>)paymentsField.GetValue(account)!).Add(payment);

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var accountDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { account });

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(accountDbSet);

        var query = new GetStudentAccountStatementQuery(workspaceId, studentId, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.StudentName.Should().Be("Karim Tarek");
        result.SeatNumber.Should().Be("B-12");
        result.TotalOpenCharges.Should().Be(500m);
        result.TotalLifetimePayments.Should().Be(350m);
        result.CurrentBalance.Should().Be(150m);
        result.BalanceStatus.Should().Be("InArrears");
        result.TotalUnallocatedCredit.Should().Be(0m);
        result.Transactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WhenPaymentsExceedCharges_CalculatesOverpaidBalance()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Student = new Student { Id = studentId, FullName = "Mona Adel" },
            Status = WorkspaceStudentStatus.Active
        };

        var account = StudentAccount.Create(ws.Id);
        var charge = StudentCharge.Create(account.Id, "Tuition Oct", 200m);

        var payment = StudentPayment.Create(account.Id, "REC-202610-0011", 500m, PaymentMethod.BankTransfer, Guid.NewGuid());
        var alloc = PaymentAllocation.Create(payment.Id, charge.Id, 200m);
        payment.AddAllocation(alloc);

        var chargesField = typeof(StudentAccount).GetField("_charges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        ((List<StudentCharge>)chargesField.GetValue(account)!).Add(charge);

        var paymentsField = typeof(StudentAccount).GetField("_payments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        ((List<StudentPayment>)paymentsField.GetValue(account)!).Add(payment);

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var accountDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { account });

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(accountDbSet);

        var query = new GetStudentAccountStatementQuery(workspaceId, studentId, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalOpenCharges.Should().Be(200m);
        result.TotalLifetimePayments.Should().Be(500m);
        result.CurrentBalance.Should().Be(-300m);
        result.BalanceStatus.Should().Be("Overpaid");
        result.TotalUnallocatedCredit.Should().Be(300m);
    }

    [Fact]
    public async Task Handle_WhenAccountRecordAbsent_ReturnsVirtualZeroBalanceStatement()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Student = new Student { Id = studentId, FullName = "New Student" },
            Status = WorkspaceStudentStatus.Active
        };

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var emptyAccountsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(Array.Empty<StudentAccount>());

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(emptyAccountsDbSet);

        var query = new GetStudentAccountStatementQuery(workspaceId, studentId, null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CurrentBalance.Should().Be(0.00m);
        result.BalanceStatus.Should().Be("Settled");
        result.TotalLifetimeCharges.Should().Be(0.00m);
        result.TotalLifetimePayments.Should().Be(0.00m);
        result.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenStudentNotInWorkspace_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var emptyWsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(Array.Empty<WorkspaceStudent>());
        _dbContext.WorkspaceStudents.Returns(emptyWsDbSet);

        var query = new GetStudentAccountStatementQuery(workspaceId, studentId, null, null);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("STUDENT_NOT_IN_WORKSPACE");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
