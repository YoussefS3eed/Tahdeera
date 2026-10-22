using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Commands.WriteOffStudentCharge;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;
using Xunit;

namespace Tahdeera.Application.UnitTests.Finances;

public class WriteOffStudentChargeCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly WriteOffStudentChargeHandler _handler;
    private readonly WriteOffStudentChargeValidator _validator;

    public WriteOffStudentChargeCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _handler = new WriteOffStudentChargeHandler(_unitOfWork, _currentUserService);
        _validator = new WriteOffStudentChargeValidator();
    }

    [Fact]
    public async Task Handle_ValidCommand_WritesOffCharge_AndVoidsActiveAllocations()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Status = Domain.Enums.WorkspaceStudentStatus.Active
        };
        var account = StudentAccount.Create(ws.Id);
        var charge = StudentCharge.Create(account.Id, "Old Outstanding Debt", 500.00m);

        var allocation = PaymentAllocation.Create(Guid.NewGuid(), charge.Id, 100.00m);
        var activeAllocations = new List<PaymentAllocation> { allocation };

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.StudentCharges.GetByIdAsync(charge.Id, Arg.Any<CancellationToken>())
            .Returns(charge);
        _unitOfWork.PaymentAllocations.GetActiveByChargeIdAsync(charge.Id, Arg.Any<CancellationToken>())
            .Returns(activeAllocations);

        var command = new WriteOffStudentChargeCommand(workspaceId, studentId, charge.Id, "Student departed financial hardship");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("WrittenOff");
        result.WriteOffReason.Should().Be("Student departed financial hardship");
        result.VoidedAllocationsCount.Should().Be(1);
        result.FreedCreditAmount.Should().Be(100.00m);

        charge.Status.ToString().Should().Be("WrittenOff");
        allocation.IsReversed.Should().BeTrue();

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenChargeNotFound_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Status = Domain.Enums.WorkspaceStudentStatus.Active
        };
        var account = StudentAccount.Create(ws.Id);

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.StudentCharges.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((StudentCharge?)null);

        var command = new WriteOffStudentChargeCommand(workspaceId, studentId, Guid.NewGuid(), "Valid write-off reason");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("CHARGE_NOT_FOUND");
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad")]
    public void Validator_WhenReasonTooShort_FailsValidation(string reason)
    {
        var command = new WriteOffStudentChargeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), reason);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("CANCELLATION_REASON_TOO_SHORT");
    }
}
