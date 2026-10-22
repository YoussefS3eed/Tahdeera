using System.Net;
using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Commands.CreateStudentCharge;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;
using Xunit;

namespace Tahdeera.Application.UnitTests.Finances;

public class CreateStudentChargeCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CreateStudentChargeHandler _handler;
    private readonly CreateStudentChargeValidator _validator;

    public CreateStudentChargeCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _handler = new CreateStudentChargeHandler(_unitOfWork, _currentUserService, _dateTimeProvider);
        _validator = new CreateStudentChargeValidator();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesChargeAndReturnsDto()
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
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);

        var command = new CreateStudentChargeCommand(
            workspaceId,
            studentId,
            null,
            "2026-10",
            "October Tuition with 20% sibling discount",
            320.00m,
            DateTimeOffset.UtcNow.AddDays(7),
            "Discount applied");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(320.00m);
        result.SettledAmount.Should().Be(0.00m);
        result.RemainingAmount.Should().Be(320.00m);
        result.Status.Should().Be("Open");
        result.BillingPeriodLabel.Should().Be("2026-10");
        result.Description.Should().Be("October Tuition with 20% sibling discount");

        await _unitOfWork.StudentCharges.Received(1).AddAsync(Arg.Is<StudentCharge>(c => c.Amount == 320.00m), Arg.Any<CancellationToken>());
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

        var command = new CreateStudentChargeCommand(
            workspaceId,
            studentId,
            null,
            null,
            "Tuition",
            100.00m,
            null,
            null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("STUDENT_NOT_IN_WORKSPACE");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenGroupBelongsToAnotherWorkspace_ThrowsBadRequest()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var otherWorkspaceId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var ws = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = studentId,
            Status = Domain.Enums.WorkspaceStudentStatus.Active
        };
        var account = StudentAccount.Create(ws.Id);
        var group = Group.Create(otherWorkspaceId, Guid.NewGuid(), 1, "Foreign Group", Guid.NewGuid());

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.Groups.GetByIdAsync(groupId)
            .Returns(group);

        var command = new CreateStudentChargeCommand(
            workspaceId,
            studentId,
            groupId,
            null,
            "Group Fee",
            150.00m,
            null,
            null);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("GROUP_NOT_FOUND_IN_WORKSPACE");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Validator_WhenAmountNonPositive_FailsValidation(decimal amount)
    {
        var command = new CreateStudentChargeCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "Valid description",
            amount,
            null,
            null);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("AMOUNT_MUST_BE_POSITIVE");
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Validator_WhenDescriptionTooShort_FailsValidation(string description)
    {
        var command = new CreateStudentChargeCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            description,
            100m,
            null,
            null);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("CHARGE_DESCRIPTION_REQUIRED");
    }
}
