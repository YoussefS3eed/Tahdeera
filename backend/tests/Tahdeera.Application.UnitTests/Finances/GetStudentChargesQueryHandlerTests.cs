using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Queries.GetStudentCharges;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Xunit;

namespace Tahdeera.Application.UnitTests.Finances;

public class GetStudentChargesQueryHandlerTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly GetStudentChargesHandler _handler;

    public GetStudentChargesQueryHandlerTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _handler = new GetStudentChargesHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_WithStatusAndPeriodFilter_ReturnsMatchingCharges()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Status = WorkspaceStudentStatus.Active
        };
        var account = StudentAccount.Create(ws.Id);

        var charge1 = StudentCharge.Create(account.Id, "October Tuition", 300m, billingPeriodLabel: "2026-10");
        var charge2 = StudentCharge.Create(account.Id, "September Tuition", 300m, billingPeriodLabel: "2026-09");
        var charge3 = StudentCharge.Create(account.Id, "October Revision", 100m, billingPeriodLabel: "2026-10");
        charge3.Cancel("Cancelled charge", Guid.NewGuid());

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var accountDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { account });
        var chargesDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { charge1, charge2, charge3 });

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(accountDbSet);
        _dbContext.StudentCharges.Returns(chargesDbSet);

        var query = new GetStudentChargesQuery(workspaceId, studentId, Status: ChargeStatus.Open, BillingPeriod: "2026-10");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(charge1.Id);
        result[0].BillingPeriodLabel.Should().Be("2026-10");
        result[0].Status.Should().Be("Open");
        result[0].Amount.Should().Be(300m);
    }
}
