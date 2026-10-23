using FluentAssertions;
using NSubstitute;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Queries.VerifyPrintableReceipt;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Application.UnitTests.Finances;

public class VerifyPrintableReceiptQueryHandlerTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly VerifyPrintableReceiptHandler _handler;

    public VerifyPrintableReceiptQueryHandlerTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<VerifyPrintableReceiptHandler>>();
        _handler = new VerifyPrintableReceiptHandler(_dbContext, logger);
    }

    [Fact]
    public async Task Handle_ValidReceiptAndMatchingHash_ReturnsVerifiedDtoWithMaskedName()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace { Id = workspaceId, Name = "أكاديمية التفوق" };
        var student = new Student { Id = Guid.NewGuid(), FullName = "أحمد محمد علي", StudentCode = "ST-1042" };
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student, Workspace = workspace };
        var account = StudentAccount.Create(ws.Id);
        typeof(StudentAccount).GetProperty("WorkspaceStudent")!.SetValue(account, ws);

        var cashierId = Guid.NewGuid();
        var receiptNumber = "REC-202610-WS001-00042";
        var paidAt = DateTimeOffset.UtcNow.AddDays(-1);
        var payment = StudentPayment.Create(account.Id, receiptNumber, 450.00m, PaymentMethod.Cash, cashierId, paidAt: paidAt);
        typeof(StudentPayment).GetProperty("StudentAccount")!.SetValue(payment, account);

        // Generate matching HMAC
        var payloadToSign = $"{payment.ReceiptNumber}:{payment.Amount:F2}:{payment.PaidAt:O}:{workspaceId}:{payment.RecordedByUserId}";
        var keyBytes = Encoding.UTF8.GetBytes(workspaceId.ToString());
        var hashBytes = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payloadToSign));
        var validHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var paymentsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment });
        _dbContext.StudentPayments.Returns(paymentsDbSet);

        var query = new VerifyPrintableReceiptQuery(receiptNumber, validHash);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ReceiptNumber.Should().Be(receiptNumber);
        result.WorkspaceName.Should().Be("أكاديمية التفوق");
        result.StudentNameMasked.Should().Be("أحمد م***");
        result.AmountEgp.Should().Be(450.00m);
        result.Status.Should().Be("Received");
        result.ReversedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AlteredHash_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace { Id = workspaceId, Name = "أكاديمية التفوق" };
        var student = new Student { Id = Guid.NewGuid(), FullName = "محمود عادل", StudentCode = "ST-1043" };
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student, Workspace = workspace };
        var account = StudentAccount.Create(ws.Id);
        typeof(StudentAccount).GetProperty("WorkspaceStudent")!.SetValue(account, ws);

        var receiptNumber = "REC-202610-WS001-00043";
        var payment = StudentPayment.Create(account.Id, receiptNumber, 300.00m, PaymentMethod.Cash, Guid.NewGuid());
        typeof(StudentPayment).GetProperty("StudentAccount")!.SetValue(payment, account);

        var paymentsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment });
        _dbContext.StudentPayments.Returns(paymentsDbSet);

        var tamperedHash = new string('0', 64);
        var query = new VerifyPrintableReceiptQuery(receiptNumber, tamperedHash);

        // Act & Assert
        var act = () => _handler.Handle(query, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("INVALID_RECEIPT_VERIFICATION_TOKEN");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_NonexistentReceiptNumber_ThrowsNotFound()
    {
        // Arrange
        var paymentsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(Array.Empty<StudentPayment>());
        _dbContext.StudentPayments.Returns(paymentsDbSet);

        var query = new VerifyPrintableReceiptQuery("REC-NONEXISTENT", new string('a', 64));

        // Act & Assert
        var act = () => _handler.Handle(query, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("INVALID_RECEIPT_VERIFICATION_TOKEN");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_ReversedPayment_ReturnsIsValidFalseAndPopulatesReversedAt()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace { Id = workspaceId, Name = "أكاديمية التفوق" };
        var student = new Student { Id = Guid.NewGuid(), FullName = "يوسف أحمد", StudentCode = "ST-1044" };
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student, Workspace = workspace };
        var account = StudentAccount.Create(ws.Id);
        typeof(StudentAccount).GetProperty("WorkspaceStudent")!.SetValue(account, ws);

        var cashierId = Guid.NewGuid();
        var receiptNumber = "REC-202610-WS001-00044";
        var payment = StudentPayment.Create(account.Id, receiptNumber, 500.00m, PaymentMethod.Card, cashierId);
        payment.Reverse("Refund requested by parent", Guid.NewGuid());
        typeof(StudentPayment).GetProperty("StudentAccount")!.SetValue(payment, account);

        var payloadToSign = $"{payment.ReceiptNumber}:{payment.Amount:F2}:{payment.PaidAt:O}:{workspaceId}:{payment.RecordedByUserId}";
        var keyBytes = Encoding.UTF8.GetBytes(workspaceId.ToString());
        var hashBytes = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payloadToSign));
        var validHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var paymentsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { payment });
        _dbContext.StudentPayments.Returns(paymentsDbSet);

        var query = new VerifyPrintableReceiptQuery(receiptNumber, validHash);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be("Reversed");
        result.ReversedAtUtc.Should().NotBeNull();
    }
}
