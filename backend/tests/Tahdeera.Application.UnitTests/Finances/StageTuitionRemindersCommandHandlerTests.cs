using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Linq.Expressions;
using System.Net;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Commands.StageTuitionReminders;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Finances;

public class StageTuitionRemindersCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<StageTuitionRemindersHandler> _logger;
    private readonly StageTuitionRemindersHandler _handler;

    public StageTuitionRemindersCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _logger = Substitute.For<ILogger<StageTuitionRemindersHandler>>();

        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 10, 22, 14, 0, 0, TimeSpan.Zero));

        _handler = new StageTuitionRemindersHandler(
            _unitOfWork,
            _dateTimeProvider,
            _logger);
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsGroupNotFoundInWorkspace()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        _unitOfWork.Groups.GetByIdAsync(groupId, Arg.Any<CancellationToken>())
            .Returns((Group?)null);

        var command = new StageTuitionRemindersCommand(workspaceId, groupId, 100m);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("GROUP_NOT_FOUND_IN_WORKSPACE");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_DebtorWithOptInAndValidPhone_StagesOutboxReminder()
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
            GuardianPhone = "01012345678"
        };

        var account = StudentAccount.Create(ws.Id);
        var charge = StudentCharge.Create(account.Id, "October Fee", 500m);
        var student = new Student { Id = studentId, FullName = "Yousef Ahmed" };

        _unitOfWork.WorkspaceStudents.FindAsync(Arg.Any<Expression<Func<WorkspaceStudent, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceStudent> { ws });
        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.StudentCharges.GetOpenChargesByAccountIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge> { charge });
        _unitOfWork.StudentPayments.GetPaymentsByAccountIdWithAllocationsAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentPayment>());
        _unitOfWork.MessageOutboxes.AnyAsync(Arg.Any<Expression<Func<MessageOutbox, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _unitOfWork.Students.GetByIdAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(student);

        var command = new StageTuitionRemindersCommand(workspaceId, null, 100m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalDebtorsFound.Should().Be(1);
        result.RemindersStagedCount.Should().Be(1);
        result.SkippedCooldownCount.Should().Be(0);
        result.SkippedOptedOutCount.Should().Be(0);
        result.SkippedInvalidPhoneCount.Should().Be(0);
        result.TotalOutstandingArrearsNotified.Should().Be(500m);

        await _unitOfWork.MessageOutboxes.Received(1).AddAsync(
            Arg.Is<MessageOutbox>(m => m.MessageType == "TuitionReminder" && m.RecipientPhone == "01012345678"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SevenDayCooldown_SkipsDuplicateStaging()
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
            GuardianPhone = "01012345678"
        };

        var account = StudentAccount.Create(ws.Id);
        var charge = StudentCharge.Create(account.Id, "October Fee", 500m);

        _unitOfWork.WorkspaceStudents.FindAsync(Arg.Any<Expression<Func<WorkspaceStudent, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceStudent> { ws });
        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.StudentCharges.GetOpenChargesByAccountIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge> { charge });
        _unitOfWork.StudentPayments.GetPaymentsByAccountIdWithAllocationsAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentPayment>());

        // Cooldown active: Recent reminder exists within 7 days
        _unitOfWork.MessageOutboxes.AnyAsync(Arg.Any<Expression<Func<MessageOutbox, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new StageTuitionRemindersCommand(workspaceId, null, 100m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalDebtorsFound.Should().Be(1);
        result.RemindersStagedCount.Should().Be(0);
        result.SkippedCooldownCount.Should().Be(1);

        await _unitOfWork.MessageOutboxes.DidNotReceive().AddAsync(Arg.Any<MessageOutbox>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OptedOutStudent_SkipsStaging()
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
            WhatsappOptIn = false, // Not opted-in
            GuardianPhone = "01012345678"
        };

        var account = StudentAccount.Create(ws.Id);
        var charge = StudentCharge.Create(account.Id, "October Fee", 500m);

        _unitOfWork.WorkspaceStudents.FindAsync(Arg.Any<Expression<Func<WorkspaceStudent, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceStudent> { ws });
        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.StudentCharges.GetOpenChargesByAccountIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge> { charge });
        _unitOfWork.StudentPayments.GetPaymentsByAccountIdWithAllocationsAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentPayment>());

        var command = new StageTuitionRemindersCommand(workspaceId, null, 100m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalDebtorsFound.Should().Be(1);
        result.RemindersStagedCount.Should().Be(0);
        result.SkippedOptedOutCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_InvalidPhoneFormat_SkipsStaging()
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
            GuardianPhone = "12345" // Invalid phone
        };

        var account = StudentAccount.Create(ws.Id);
        var charge = StudentCharge.Create(account.Id, "October Fee", 500m);

        _unitOfWork.WorkspaceStudents.FindAsync(Arg.Any<Expression<Func<WorkspaceStudent, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceStudent> { ws });
        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(account);
        _unitOfWork.StudentCharges.GetOpenChargesByAccountIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge> { charge });
        _unitOfWork.StudentPayments.GetPaymentsByAccountIdWithAllocationsAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentPayment>());

        var command = new StageTuitionRemindersCommand(workspaceId, null, 100m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalDebtorsFound.Should().Be(1);
        result.RemindersStagedCount.Should().Be(0);
        result.SkippedInvalidPhoneCount.Should().Be(1);
    }
}
