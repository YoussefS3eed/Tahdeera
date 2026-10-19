using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Students.Commands.ChangeStudentStatus;
using Tahdeera.Application.Features.Students.Commands.UpdateStudentContact;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Students;

public class StudentStatusAndContactCommandTests
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();

    public StudentStatusAndContactCommandTests()
    {
        _currentUserService = Substitute.For<ICurrentUserService>();
        _auditLogService = Substitute.For<IAuditLogService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _currentUserService.UserId.Returns(_userId);
        _currentUserService.IsPlatformAdmin.Returns(true);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ChangeStatus_ToActive_WhenAtQuotaCapacity_ShouldReturnQuotaError()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var plan = new SubscriptionPlan
        {
            Code = "TEST",
            NameAr = "خطة",
            NameEn = "Plan",
            MaxStudents = 1,
            DefaultPrice = 100
        };
        context.SubscriptionPlans.Add(plan);

        context.WorkspaceSubscriptions.Add(new WorkspaceSubscription
        {
            WorkspaceId = _workspaceId,
            PlanId = plan.Id,
            Plan = plan,
            StartsAt = DateTimeOffset.UtcNow.AddDays(-10),
            EndsAt = DateTimeOffset.UtcNow.AddDays(30),
            Status = "Active"
        });

        // 1 active student already exists
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = Guid.NewGuid(),
            Status = WorkspaceStudentStatus.Active
        });

        // Target student currently Inactive
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = _studentId,
            Status = WorkspaceStudentStatus.Inactive
        });
        await context.SaveChangesAsync();

        var handler = new ChangeStudentStatusCommandHandler(new UnitOfWork(context), _currentUserService, _auditLogService, _dateTimeProvider);

        // Act
        var result = await handler.Handle(new ChangeStudentStatusCommand(_workspaceId, _studentId, "Active"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUOTA_MAX_STUDENTS_REACHED");
    }

    [Fact]
    public async Task ChangeStatus_ToActive_WhenUnderQuota_ShouldSucceed()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var plan = new SubscriptionPlan
        {
            Code = "TEST2",
            NameAr = "خطة",
            NameEn = "Plan",
            MaxStudents = 50,
            DefaultPrice = 100
        };
        context.SubscriptionPlans.Add(plan);

        context.WorkspaceSubscriptions.Add(new WorkspaceSubscription
        {
            WorkspaceId = _workspaceId,
            PlanId = plan.Id,
            Plan = plan,
            StartsAt = DateTimeOffset.UtcNow.AddDays(-10),
            EndsAt = DateTimeOffset.UtcNow.AddDays(30),
            Status = "Active"
        });

        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = _studentId,
            Status = WorkspaceStudentStatus.Inactive
        });
        await context.SaveChangesAsync();

        var handler = new ChangeStudentStatusCommandHandler(new UnitOfWork(context), _currentUserService, _auditLogService, _dateTimeProvider);

        // Act
        var result = await handler.Handle(new ChangeStudentStatusCommand(_workspaceId, _studentId, "Active"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var wsStudent = context.WorkspaceStudents.First(ws => ws.StudentId == _studentId);
        wsStudent.Status.Should().Be(WorkspaceStudentStatus.Active);
    }

    [Fact]
    public async Task ChangeStatus_ToBlocked_ShouldSucceedWithoutQuotaCheck()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = _studentId,
            Status = WorkspaceStudentStatus.Active
        });
        await context.SaveChangesAsync();

        var handler = new ChangeStudentStatusCommandHandler(new UnitOfWork(context), _currentUserService, _auditLogService, _dateTimeProvider);

        // Act
        var result = await handler.Handle(new ChangeStudentStatusCommand(_workspaceId, _studentId, "Blocked", "Behavior issue"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var wsStudent = context.WorkspaceStudents.First(ws => ws.StudentId == _studentId);
        wsStudent.Status.Should().Be(WorkspaceStudentStatus.Blocked);
    }

    [Fact]
    public async Task UpdateContact_ShouldUpdateStudentFullNameAndLocalFields()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var student = new Student
        {
            Id = _studentId,
            StudentCode = "STU-2026-00001",
            FullName = "أحمد محمد علي"
        };
        context.Students.Add(student);

        var wsStudent = new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = _studentId,
            Student = student,
            GuardianPhone = "+201012345678",
            StudentPhone = "+201099999999",
            SeatNumber = "A1"
        };
        context.WorkspaceStudents.Add(wsStudent);
        await context.SaveChangesAsync();

        var handler = new UpdateStudentContactCommandHandler(new UnitOfWork(context), _currentUserService, _auditLogService);

        var command = new UpdateStudentContactCommand(
            WorkspaceId: _workspaceId,
            StudentId: _studentId,
            FullName: "أحمد محمد علي محمود",
            GuardianName: "علي محمود",
            GuardianPhone: "01011112222",
            StudentPhone: "01033334444",
            WhatsappOptIn: true,
            SeatNumber: "B5",
            LocalNotes: "متفوق"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        student.FullName.Should().Be("أحمد محمد علي محمود");
        wsStudent.GuardianPhone.Should().Be("+201011112222");
        wsStudent.StudentPhone.Should().Be("+201033334444");
        wsStudent.SeatNumber.Should().Be("B5");
        wsStudent.LocalNotes.Should().Be("متفوق");
        wsStudent.WhatsappOptIn.Should().BeTrue();
        wsStudent.WhatsappOptInAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateContact_DuplicateGuardianPhone_WithoutBypass_ShouldFailWithConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        // Student A
        var studentA = new Student { Id = _studentId, StudentCode = "STU-2026-00001", FullName = "طالب أول" };
        context.Students.Add(studentA);
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = _studentId,
            Student = studentA,
            GuardianPhone = "+201012345678"
        });

        // Student B with target phone
        var siblingId = Guid.NewGuid();
        var studentB = new Student { Id = siblingId, StudentCode = "STU-2026-00002", FullName = "طالب ثان" };
        context.Students.Add(studentB);
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = siblingId,
            Student = studentB,
            GuardianPhone = "+201099998888"
        });

        await context.SaveChangesAsync();

        var handler = new UpdateStudentContactCommandHandler(new UnitOfWork(context), _currentUserService, _auditLogService);

        var command = new UpdateStudentContactCommand(
            WorkspaceId: _workspaceId,
            StudentId: _studentId,
            FullName: null,
            GuardianName: null,
            GuardianPhone: "01099998888", // duplicate of Student B
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            AllowSiblingPhone: false
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GUARDIAN_PHONE_ALREADY_EXISTS_CONFIRM_SIBLING");
    }

    [Fact]
    public async Task UpdateContact_DuplicateGuardianPhone_WithBypass_ShouldSucceed()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var studentA = new Student { Id = _studentId, StudentCode = "STU-2026-00001", FullName = "طالب أول" };
        context.Students.Add(studentA);
        var wsA = new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = _studentId,
            Student = studentA,
            GuardianPhone = "+201012345678"
        };
        context.WorkspaceStudents.Add(wsA);

        var siblingId = Guid.NewGuid();
        var studentB = new Student { Id = siblingId, StudentCode = "STU-2026-00002", FullName = "طالب ثان" };
        context.Students.Add(studentB);
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = siblingId,
            Student = studentB,
            GuardianPhone = "+201099998888"
        });

        await context.SaveChangesAsync();

        var handler = new UpdateStudentContactCommandHandler(new UnitOfWork(context), _currentUserService, _auditLogService);

        var command = new UpdateStudentContactCommand(
            WorkspaceId: _workspaceId,
            StudentId: _studentId,
            FullName: null,
            GuardianName: null,
            GuardianPhone: "01099998888",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            AllowSiblingPhone: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        wsA.GuardianPhone.Should().Be("+201099998888");
    }

    [Fact]
    public async Task ChangeStatus_WhenUserIsNotWorkspaceMember_ShouldReturnForbidden()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(Guid.NewGuid());
        currentUserService.IsPlatformAdmin.Returns(false);

        var handler = new ChangeStudentStatusCommandHandler(
            new UnitOfWork(context),
            currentUserService,
            _auditLogService,
            _dateTimeProvider);

        // Act
        var result = await handler.Handle(new ChangeStudentStatusCommand(_workspaceId, _studentId, "Active"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_WORKSPACE_ACCESS");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateContact_WhenUserIsNotWorkspaceMember_ShouldReturnForbidden()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(Guid.NewGuid());
        currentUserService.IsPlatformAdmin.Returns(false);

        var handler = new UpdateStudentContactCommandHandler(
            new UnitOfWork(context),
            currentUserService,
            _auditLogService);

        var command = new UpdateStudentContactCommand(
            WorkspaceId: _workspaceId,
            StudentId: _studentId,
            FullName: "طالب",
            GuardianName: null,
            GuardianPhone: "01099998888",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_WORKSPACE_ACCESS");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }
}
