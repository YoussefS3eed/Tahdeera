using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Commands.CancelStudentCharge;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Finances;

public class CancelStudentChargeCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly CancelStudentChargeHandler _handler;
    private readonly CancelStudentChargeValidator _validator;

    public CancelStudentChargeCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _handler = new CancelStudentChargeHandler(_unitOfWork, _currentUserService);
        _validator = new CancelStudentChargeValidator();
    }

    [Fact]
    public async Task Handle_ValidCommand_CancelsCharge_AndVoidsActiveAllocations()
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
        var charge = StudentCharge.Create(account.Id, "October Tuition", 300.00m);

        var allocation1 = PaymentAllocation.Create(Guid.NewGuid(), charge.Id, 100.00m);
        var allocation2 = PaymentAllocation.Create(Guid.NewGuid(), charge.Id, 50.00m);
        var activeAllocations = new List<PaymentAllocation> { allocation1, allocation2 };

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdForUpdateAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.StudentCharges.GetByIdAsync(charge.Id, Arg.Any<CancellationToken>())
            .Returns(charge);
        _unitOfWork.PaymentAllocations.GetActiveByChargeIdAsync(charge.Id, Arg.Any<CancellationToken>())
            .Returns(activeAllocations);

        var command = new CancelStudentChargeCommand(workspaceId, studentId, charge.Id, "Enrolled by mistake");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Cancelled");
        result.CancellationReason.Should().Be("Enrolled by mistake");
        result.VoidedAllocationsCount.Should().Be(2);
        result.FreedCreditAmount.Should().Be(150.00m);

        charge.Status.ToString().Should().Be("Cancelled");
        allocation1.IsReversed.Should().BeTrue();
        allocation2.IsReversed.Should().BeTrue();

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

        var command = new CancelStudentChargeCommand(workspaceId, studentId, Guid.NewGuid(), "Valid cancellation reason");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("CHARGE_NOT_FOUND");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    public void Validator_WhenReasonTooShort_FailsValidation(string reason)
    {
        var command = new CancelStudentChargeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), reason);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("CANCELLATION_REASON_TOO_SHORT");
    }
}
