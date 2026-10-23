using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Commands.RecordStudentPayment;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Finances.Services;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Finances;

public class RecordStudentPaymentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentAllocationEngine _allocationEngine;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RecordStudentPaymentHandler> _logger;
    private readonly RecordStudentPaymentHandler _handler;
    private readonly RecordStudentPaymentValidator _validator;

    public RecordStudentPaymentCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _allocationEngine = Substitute.For<IPaymentAllocationEngine>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _logger = Substitute.For<ILogger<RecordStudentPaymentHandler>>();

        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 10, 22, 10, 0, 0, TimeSpan.Zero));
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _handler = new RecordStudentPaymentHandler(_unitOfWork, _allocationEngine, _currentUserService, _dateTimeProvider, _logger);
        _validator = new RecordStudentPaymentValidator();
    }

    [Fact]
    public async Task Handle_ValidCommand_RecordsPaymentAndReturnsDto()
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
            WhatsappOptIn = false
        };
        var account = StudentAccount.Create(ws.Id);

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.WorkspaceReceiptSequences.GetNextSequenceAsync(workspaceId, 202610, Arg.Any<CancellationToken>())
            .Returns(42);

        _allocationEngine.AllocatePaymentAsync(Arg.Any<StudentPayment>(), account.Id, Arg.Any<IReadOnlyList<PaymentAllocationRequestDto>?>(), Arg.Any<CancellationToken>())
            .Returns(new AllocationResult(300m, 50m, new List<PaymentAllocation>()));

        var command = new RecordStudentPaymentCommand(
            workspaceId,
            studentId,
            350.00m,
            PaymentMethod.Cash,
            _dateTimeProvider.UtcNow,
            "CASH-REF",
            "Payment at front desk",
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(350.00m);
        result.Method.Should().Be("Cash");
        result.ReceiptNumber.Should().StartWith("REC-202610-");
        result.AllocatedAmount.Should().Be(300m);
        result.UnallocatedCredit.Should().Be(50m);
        result.WhatsAppReceiptStaged.Should().BeFalse();

        await _unitOfWork.StudentPayments.Received(1).AddAsync(Arg.Is<StudentPayment>(p => p.Amount == 350.00m), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStudentNotInWorkspace_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns((WorkspaceStudent?)null);

        var command = new RecordStudentPaymentCommand(
            workspaceId,
            studentId,
            100m,
            PaymentMethod.Cash,
            null,
            null,
            null,
            null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("STUDENT_NOT_IN_WORKSPACE");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validator_WhenAmountNonPositive_FailsValidation(decimal amount)
    {
        var command = new RecordStudentPaymentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            PaymentMethod.Cash,
            null,
            null,
            null,
            null);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("AMOUNT_MUST_BE_POSITIVE");
    }

    [Fact]
    public void Validator_WhenPaidAtInFuture_FailsValidation()
    {
        var command = new RecordStudentPaymentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            PaymentMethod.Cash,
            DateTimeOffset.UtcNow.AddHours(2),
            null,
            null,
            null);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PaidAt)
            .WithErrorMessage("PAYMENT_DATE_CANNOT_BE_FUTURE");
    }
}
