using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Commands.RecordStudentPayment;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Finances.Services;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Xunit;

namespace Tahdeera.Application.UnitTests.Finances;

public class RecordStudentPaymentOutboxTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentAllocationEngine _allocationEngine;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RecordStudentPaymentHandler> _logger;
    private readonly RecordStudentPaymentHandler _handler;

    public RecordStudentPaymentOutboxTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _allocationEngine = Substitute.For<IPaymentAllocationEngine>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _logger = Substitute.For<ILogger<RecordStudentPaymentHandler>>();

        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero));
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _handler = new RecordStudentPaymentHandler(_unitOfWork, _allocationEngine, _currentUserService, _dateTimeProvider, _logger);
    }

    [Fact]
    public async Task Handle_WhenOptedInAndValidPhone_StagesOutboxMessage()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Status = WorkspaceStudentStatus.Active,
            WhatsappOptIn = true,
            GuardianPhone = "+201012345678"
        };
        var account = StudentAccount.Create(ws.Id);
        var student = new Student
        {
            Id = studentId,
            FullName = "Ahmed Ali"
        };

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.WorkspaceReceiptSequences.GetNextSequenceAsync(workspaceId, 202610, Arg.Any<CancellationToken>())
            .Returns(101);
        _unitOfWork.Students.GetByIdAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(student);

        _allocationEngine.AllocatePaymentAsync(Arg.Any<StudentPayment>(), account.Id, null, Arg.Any<CancellationToken>())
            .Returns(new AllocationResult(250m, 0m, new List<PaymentAllocation>()));

        var command = new RecordStudentPaymentCommand(
            workspaceId,
            studentId,
            250.00m,
            PaymentMethod.Wallet,
            null,
            "VODAFONE-CASH",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.WhatsAppReceiptStaged.Should().BeTrue();

        await _unitOfWork.MessageOutbox.Received(1).AddAsync(
            Arg.Is<MessageOutbox>(m =>
                m.WorkspaceId == workspaceId &&
                m.RecipientPhone == "+201012345678" &&
                m.MessageType == "PaymentReceipt" &&
                m.TemplateName == "payment_receipt_v1" &&
                m.IdempotencyKey == $"pay_{result.PaymentId}"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOptedOut_SkipsOutboxStaging()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Status = WorkspaceStudentStatus.Active,
            WhatsappOptIn = false,
            GuardianPhone = "+201012345678"
        };
        var account = StudentAccount.Create(ws.Id);

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.WorkspaceReceiptSequences.GetNextSequenceAsync(workspaceId, 202610, Arg.Any<CancellationToken>())
            .Returns(102);

        _allocationEngine.AllocatePaymentAsync(Arg.Any<StudentPayment>(), account.Id, null, Arg.Any<CancellationToken>())
            .Returns(new AllocationResult(200m, 0m, new List<PaymentAllocation>()));

        var command = new RecordStudentPaymentCommand(
            workspaceId,
            studentId,
            200.00m,
            PaymentMethod.Cash,
            null,
            null,
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.WhatsAppReceiptStaged.Should().BeFalse();
        await _unitOfWork.MessageOutbox.DidNotReceive().AddAsync(Arg.Any<MessageOutbox>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOptedInButPhoneMalformed_SkipsOutboxAndCompletesPayment()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Status = WorkspaceStudentStatus.Active,
            WhatsappOptIn = true,
            GuardianPhone = "not-a-valid-phone-123"
        };
        var account = StudentAccount.Create(ws.Id);

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.WorkspaceReceiptSequences.GetNextSequenceAsync(workspaceId, 202610, Arg.Any<CancellationToken>())
            .Returns(103);

        _allocationEngine.AllocatePaymentAsync(Arg.Any<StudentPayment>(), account.Id, null, Arg.Any<CancellationToken>())
            .Returns(new AllocationResult(100m, 0m, new List<PaymentAllocation>()));

        var command = new RecordStudentPaymentCommand(
            workspaceId,
            studentId,
            100.00m,
            PaymentMethod.Cash,
            null,
            null,
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.WhatsAppReceiptStaged.Should().BeFalse();
        await _unitOfWork.MessageOutbox.DidNotReceive().AddAsync(Arg.Any<MessageOutbox>(), Arg.Any<CancellationToken>());
        await _unitOfWork.StudentPayments.Received(1).AddAsync(Arg.Any<StudentPayment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
