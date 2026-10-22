using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceSummary;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Xunit;

namespace Tahdeera.Application.UnitTests.Finances;

public class GetWorkspaceFinanceSummaryQueryHandlerTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly GetWorkspaceFinanceSummaryHandler _handler;

    public GetWorkspaceFinanceSummaryQueryHandlerTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _handler = new GetWorkspaceFinanceSummaryHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_AggregatesTotalsAndMethodBreakdownAccurately()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();

        // Student 1: 500 open charges, 300 cash payment -> 200 in arrears (debtor)
        var ws1 = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = Guid.NewGuid() };
        var acc1 = StudentAccount.Create(ws1.Id);
        var charge1 = StudentCharge.Create(acc1.Id, "October Tuition", 500m);
        var payment1 = StudentPayment.Create(acc1.Id, "REC-001", 300m, PaymentMethod.Cash, Guid.NewGuid());

        var chargesField = typeof(StudentAccount).GetField("_charges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        ((List<StudentCharge>)chargesField.GetValue(acc1)!).Add(charge1);
        var paymentsField = typeof(StudentAccount).GetField("_payments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        ((List<StudentPayment>)paymentsField.GetValue(acc1)!).Add(payment1);

        // Student 2: 400 open charges, 400 wallet payment -> 0 in arrears (settled, not debtor)
        var ws2 = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = Guid.NewGuid() };
        var acc2 = StudentAccount.Create(ws2.Id);
        var charge2 = StudentCharge.Create(acc2.Id, "October Tuition", 400m);
        var payment2 = StudentPayment.Create(acc2.Id, "REC-002", 400m, PaymentMethod.Wallet, Guid.NewGuid());

        ((List<StudentCharge>)chargesField.GetValue(acc2)!).Add(charge2);
        ((List<StudentPayment>)paymentsField.GetValue(acc2)!).Add(payment2);

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws1, ws2 });
        var accountsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { acc1, acc2 });

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(accountsDbSet);

        var query = new GetWorkspaceFinanceSummaryQuery(workspaceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalBilled.Should().Be(900m);
        result.TotalCollected.Should().Be(700m);
        result.TotalOutstandingArrears.Should().Be(200m);
        result.ActiveDebtorCount.Should().Be(1);

        result.CollectedByMethod["Cash"].Should().Be(300m);
        result.CollectedByMethod["Wallet"].Should().Be(400m);
    }
}
