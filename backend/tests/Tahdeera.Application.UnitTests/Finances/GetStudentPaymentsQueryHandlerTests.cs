using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Queries.GetStudentPayments;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

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
        var outboxDbSet = AsyncQueryTestHelpers.CreateMockDbSet(Array.Empty<MessageOutbox>());

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(accountDbSet);
        _dbContext.StudentPayments.Returns(paymentsDbSet);
        _dbContext.MessageOutboxes.Returns(outboxDbSet);

        var query = new GetStudentPaymentsQuery(workspaceId, studentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].ReceiptNumber.Should().Be("REC-202610-0022"); // Newer first
        result[0].Method.Should().Be("Wallet");
        result[0].WhatsAppDeliveryStatus.Should().Be("None");
        result[1].ReceiptNumber.Should().Be("REC-202610-0021");
        result[1].Method.Should().Be("Cash");
        result[1].WhatsAppDeliveryStatus.Should().Be("None");
    }

    [Fact]
    public async Task Handle_WithMatchingOutbox_ProjectsWhatsAppDeliveryStatusDelivered()
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

        var payment = StudentPayment.Create(
            account.Id,
            "REC-202610-0023",
            300m,
            PaymentMethod.Cash,
            Guid.NewGuid(),
            paidAt: DateTimeOffset.UtcNow);

        var outbox = new MessageOutbox
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            IdempotencyKey = "pay_" + payment.Id,
            Status = OutboxMessageStatus.Delivered,
            DeliveredAt = DateTimeOffset.UtcNow,
            MessageType = "PaymentReceipt",
            TemplateName = "payment_receipt_v1",
            RecipientPhone = "+201012345678"
        };

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        var accountDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { account });
        var paymentsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment });
        var outboxDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { outbox });

        _dbContext.WorkspaceStudents.Returns(wsDbSet);
        _dbContext.StudentAccounts.Returns(accountDbSet);
        _dbContext.StudentPayments.Returns(paymentsDbSet);
        _dbContext.MessageOutboxes.Returns(outboxDbSet);

        var query = new GetStudentPaymentsQuery(workspaceId, studentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].WhatsAppDeliveryStatus.Should().Be("Delivered");
        result[0].WhatsAppDeliveredAt.Should().Be(outbox.DeliveredAt);
    }
}
