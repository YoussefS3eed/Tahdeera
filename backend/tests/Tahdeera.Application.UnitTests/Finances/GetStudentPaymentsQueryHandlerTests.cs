using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Queries.GetStudentPayments;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Xunit;

namespace Tahdeera.Application.UnitTests.Finances;

public class GetStudentPaymentsQueryHandlerTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly GetStudentPaymentsHandler _handler;

    public GetStudentPaymentsQueryHandlerTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _handler = new GetStudentPaymentsHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_ReturnsPaymentsWithAllocationsOrderedByPaidAt()
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

        var payment1 = StudentPayment.Create(
            account.Id,
            "REC-202610-0021",
            200m,
            PaymentMethod.Cash,
            Guid.NewGuid(),
            paidAt: DateTimeOffset.UtcNow.AddDays(-2));

        var payment2 = StudentPayment.Create(
            account.Id,
            "REC-202610-0022",
            150m,
            PaymentMethod.Wallet,
            Guid.NewGuid(),
            paidAt: DateTimeOffset.UtcNow.AddDays(-1));

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var accountDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { account });
        var paymentsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment1, payment2 });

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(accountDbSet);
        _dbContext.StudentPayments.Returns(paymentsDbSet);

        var query = new GetStudentPaymentsQuery(workspaceId, studentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].ReceiptNumber.Should().Be("REC-202610-0022"); // Newer first
        result[0].Method.Should().Be("Wallet");
        result[1].ReceiptNumber.Should().Be("REC-202610-0021");
        result[1].Method.Should().Be("Cash");
    }
}
