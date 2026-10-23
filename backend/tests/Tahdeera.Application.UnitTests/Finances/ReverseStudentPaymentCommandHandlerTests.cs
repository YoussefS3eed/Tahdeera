using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Commands.ReverseStudentPayment;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Finances;

public class ReverseStudentPaymentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ReverseStudentPaymentHandler _handler;
    private readonly ReverseStudentPaymentValidator _validator;

    public ReverseStudentPaymentCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _handler = new ReverseStudentPaymentHandler(_unitOfWork, _currentUserService);
        _validator = new ReverseStudentPaymentValidator();
    }

    [Fact]
    public async Task Handle_ValidCommand_ReversesPayment_AndVoidsAllocations()
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
            "REC-202610-0001",
            300m,
            PaymentMethod.Cash,
            Guid.NewGuid());

        var chargeId1 = Guid.NewGuid();
        var chargeId2 = Guid.NewGuid();
        var alloc1 = PaymentAllocation.Create(payment.Id, chargeId1, 200m);
        var alloc2 = PaymentAllocation.Create(payment.Id, chargeId2, 100m);
        payment.AddAllocation(alloc1);
        payment.AddAllocation(alloc2);

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.StudentPayments.GetByIdWithAllocationsAsync(payment.Id, Arg.Any<CancellationToken>())
            .Returns(payment);

        var command = new ReverseStudentPaymentCommand(workspaceId, studentId, payment.Id, "Bounced check / refund");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Reversed");
        result.ReversalReason.Should().Be("Bounced check / refund");
        result.ReopenedChargesCount.Should().Be(2);
        result.RestoredArrearsAmount.Should().Be(300m);

        payment.Status.Should().Be(PaymentStatus.Reversed);
        alloc1.IsReversed.Should().BeTrue();
        alloc2.IsReversed.Should().BeTrue();

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPaymentAlreadyReversed_ThrowsDomainException()
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
            "REC-202610-0002",
            100m,
            PaymentMethod.Cash,
            Guid.NewGuid());
        payment.Reverse("First reversal", Guid.NewGuid());

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.StudentPayments.GetByIdWithAllocationsAsync(payment.Id, Arg.Any<CancellationToken>())
            .Returns(payment);

        var command = new ReverseStudentPaymentCommand(workspaceId, studentId, payment.Id, "Duplicate reversal attempt");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("PAYMENT_ALREADY_REVERSED");
    }

    [Fact]
    public async Task Handle_WhenPaymentNotFound_ThrowsNotFound()
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

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.StudentPayments.GetByIdWithAllocationsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((StudentPayment?)null);

        var command = new ReverseStudentPaymentCommand(workspaceId, studentId, Guid.NewGuid(), "Valid reason for reversal");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("PAYMENT_NOT_FOUND");
    }

    [Theory]
    [InlineData("")]
    [InlineData("rev")]
    public void Validator_WhenReasonTooShort_FailsValidation(string reason)
    {
        var command = new ReverseStudentPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), reason);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("REVERSAL_REASON_TOO_SHORT");
    }
}
