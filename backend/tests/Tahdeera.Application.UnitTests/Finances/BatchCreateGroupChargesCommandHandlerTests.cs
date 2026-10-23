using FluentAssertions;
using NSubstitute;
using System.Linq.Expressions;
using System.Net;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Commands.BatchCreateGroupCharges;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Finances;

public class BatchCreateGroupChargesCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly BatchCreateGroupChargesHandler _handler;

    public BatchCreateGroupChargesCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUserService.UserId.Returns(Guid.NewGuid());

        _handler = new BatchCreateGroupChargesHandler(
            _unitOfWork,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_GroupNotFoundOrDifferentWorkspace_ThrowsNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        _unitOfWork.Groups.GetByIdAsync(groupId, Arg.Any<CancellationToken>())
            .Returns((Group?)null);

        var command = new BatchCreateGroupChargesCommand(workspaceId, groupId, "2026-10", "Tuition Oct", 500m);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("GROUP_NOT_FOUND_IN_WORKSPACE");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Handle_GroupWithZeroActiveStudents_ThrowsBadRequest()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var group = Group.Create(workspaceId, Guid.NewGuid(), (short)10, "Grade 10 - Group A", Guid.NewGuid());

        _unitOfWork.Groups.GetByIdAsync(groupId, Arg.Any<CancellationToken>())
            .Returns(group);
        _unitOfWork.GroupStudents.FindAsync(Arg.Any<Expression<Func<GroupStudent, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GroupStudent>());

        var command = new BatchCreateGroupChargesCommand(workspaceId, groupId, "2026-10", "Tuition Oct", 500m);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<DomainRuleViolationException>();
        ex.Which.ErrorCode.Should().Be("NO_ACTIVE_STUDENTS_IN_GROUP");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Handle_WithActiveStudents_CreatesChargesSuccessfully()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var studentId1 = Guid.NewGuid();
        var studentId2 = Guid.NewGuid();

        var group = Group.Create(workspaceId, Guid.NewGuid(), (short)10, "Grade 10 - Group A", Guid.NewGuid());
        typeof(Group).GetProperty("Id")!.SetValue(group, groupId);

        var gs1 = GroupStudent.Create(groupId, studentId1, DateTimeOffset.UtcNow);
        var gs2 = GroupStudent.Create(groupId, studentId2, DateTimeOffset.UtcNow);

        var ws1 = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = studentId1, Status = WorkspaceStudentStatus.Active };
        var ws2 = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = studentId2, Status = WorkspaceStudentStatus.Active };

        var acc1 = StudentAccount.Create(ws1.Id);
        var acc2 = StudentAccount.Create(ws2.Id);

        _unitOfWork.Groups.GetByIdAsync(groupId, Arg.Any<CancellationToken>())
            .Returns(group);

        _unitOfWork.GroupStudents.FindAsync(Arg.Any<Expression<Func<GroupStudent, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GroupStudent> { gs1, gs2 });

        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId1, Arg.Any<CancellationToken>())
            .Returns(ws1);
        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId2, Arg.Any<CancellationToken>())
            .Returns(ws2);

        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws1.Id, Arg.Any<CancellationToken>())
            .Returns(acc1);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws2.Id, Arg.Any<CancellationToken>())
            .Returns(acc2);

        _unitOfWork.StudentCharges.GetOpenChargesByAccountIdAsync(acc1.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge>());
        _unitOfWork.StudentCharges.GetOpenChargesByAccountIdAsync(acc2.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge>());

        _unitOfWork.StudentPayments.GetPaymentsByAccountIdWithAllocationsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentPayment>());

        var command = new BatchCreateGroupChargesCommand(workspaceId, groupId, "2026-10", "Tuition Oct", 450m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalActiveEnrolled.Should().Be(2);
        result.ChargesCreatedCount.Should().Be(2);
        result.SkippedDuplicatesCount.Should().Be(0);
        result.TotalBilledAmount.Should().Be(900m);
        result.CreatedChargeIds.Should().HaveCount(2);

        await _unitOfWork.StudentCharges.Received(2).AddAsync(Arg.Any<StudentCharge>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReRunForSamePeriod_SkipsDuplicateOpenCharges()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var group = Group.Create(workspaceId, Guid.NewGuid(), (short)10, "Grade 10 - Group A", Guid.NewGuid());
        typeof(Group).GetProperty("Id")!.SetValue(group, groupId);

        var gs = GroupStudent.Create(groupId, studentId, DateTimeOffset.UtcNow);
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = studentId, Status = WorkspaceStudentStatus.Active };
        var acc = StudentAccount.Create(ws.Id);

        var existingCharge = StudentCharge.Create(
            acc.Id,
            "Tuition Oct",
            450m,
            groupId,
            "2026-10");

        _unitOfWork.Groups.GetByIdAsync(groupId, Arg.Any<CancellationToken>())
            .Returns(group);
        _unitOfWork.GroupStudents.FindAsync(Arg.Any<Expression<Func<GroupStudent, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GroupStudent> { gs });
        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(acc);
        _unitOfWork.StudentCharges.GetOpenChargesByAccountIdAsync(acc.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge> { existingCharge });

        var command = new BatchCreateGroupChargesCommand(workspaceId, groupId, "2026-10", "Tuition Oct", 450m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ChargesCreatedCount.Should().Be(0);
        result.SkippedDuplicatesCount.Should().Be(1);
        result.TotalBilledAmount.Should().Be(0m);
        await _unitOfWork.StudentCharges.DidNotReceive().AddAsync(Arg.Any<StudentCharge>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StudentWithPrepaidCredit_AutoSettlesNewlyCreatedCharge()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var group = Group.Create(workspaceId, Guid.NewGuid(), (short)10, "Grade 10 - Group A", Guid.NewGuid());
        typeof(Group).GetProperty("Id")!.SetValue(group, groupId);

        var gs = GroupStudent.Create(groupId, studentId, DateTimeOffset.UtcNow);
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = studentId, Status = WorkspaceStudentStatus.Active };
        var acc = StudentAccount.Create(ws.Id);

        var prepaidPayment = StudentPayment.Create(
            acc.Id,
            "REC-202610-0099",
            600m,
            PaymentMethod.Cash,
            Guid.NewGuid());

        _unitOfWork.Groups.GetByIdAsync(groupId, Arg.Any<CancellationToken>())
            .Returns(group);
        _unitOfWork.GroupStudents.FindAsync(Arg.Any<Expression<Func<GroupStudent, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GroupStudent> { gs });
        _unitOfWork.WorkspaceStudents.GetByWorkspaceAndStudentIdAsync(workspaceId, studentId, Arg.Any<CancellationToken>())
            .Returns(ws);
        _unitOfWork.StudentAccounts.GetByWorkspaceStudentIdAsync(ws.Id, Arg.Any<CancellationToken>())
            .Returns(acc);
        _unitOfWork.StudentCharges.GetOpenChargesByAccountIdAsync(acc.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentCharge>());
        _unitOfWork.StudentPayments.GetPaymentsByAccountIdWithAllocationsAsync(acc.Id, Arg.Any<CancellationToken>())
            .Returns(new List<StudentPayment> { prepaidPayment });

        var command = new BatchCreateGroupChargesCommand(workspaceId, groupId, "2026-10", "Tuition Oct", 400m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ChargesCreatedCount.Should().Be(1);
        result.AutoSettledFromCreditCount.Should().Be(1);
        result.TotalBilledAmount.Should().Be(400m);

        await _unitOfWork.PaymentAllocations.Received(1).AddAsync(Arg.Is<PaymentAllocation>(a => a.Amount == 400m), Arg.Any<CancellationToken>());
    }
}
