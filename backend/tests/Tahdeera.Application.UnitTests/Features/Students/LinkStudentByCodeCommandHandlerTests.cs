using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Students.Commands.LinkStudentByCode;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Students;

public class LinkStudentByCodeCommandHandlerTests
{
    private readonly ICredentialSecurityService _securityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public LinkStudentByCodeCommandHandlerTests()
    {
        _securityService = Substitute.For<ICredentialSecurityService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _auditLogService = Substitute.For<IAuditLogService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _currentUserService.UserId.Returns(_userId);
        _currentUserService.IsPlatformAdmin.Returns(true);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_WhenValidCodeAndMatchingRegisteredPhone_ShouldLinkSuccessfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var activeYear = new AcademicYear { IsCurrent = true, StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30) };
        context.AcademicYears.Add(activeYear);

        var grade = new Grade { Id = 10, Code = "SEC1", NameAr = "الصف الأول الثانوي", NameEn = "Secondary 1", SortOrder = 10, IsActive = true };
        context.Grades.Add(grade);

        var student = new Student { StudentCode = "STU-2026-11111", FullName = "أحمد خالد", IsActive = true };
        context.Students.Add(student);

        // Registered in Center A with guardian phone
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = Guid.NewGuid(),
            StudentId = student.Id,
            GuardianPhone = "+201096817311",
            Status = WorkspaceStudentStatus.Active
        });

        context.StudentAcademicHistories.Add(new StudentAcademicHistory
        {
            StudentId = student.Id,
            AcademicYearId = activeYear.Id,
            GradeId = grade.Id
        });

        await context.SaveChangesAsync();

        var handler = new LinkStudentByCodeCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCodeCommand(
            WorkspaceId: _workspaceId,
            StudentCode: "STU-2026-11111",
            VerificationFactor: "01096817311", // matching phone
            GradeId: null,
            GuardianName: "خالد",
            GuardianPhone: "01096817311",
            StudentPhone: null,
            WhatsappOptIn: true,
            SeatNumber: "C-01",
            LocalNotes: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StudentId.Should().Be(student.Id);
        result.Value.GradeId.Should().Be(10);
        context.WorkspaceStudents.Should().ContainSingle(ws => ws.WorkspaceId == _workspaceId && ws.StudentId == student.Id);
    }

    [Fact]
    public async Task Handle_WhenNonExistentCode_ShouldTriggerDummyDelayAndFailUniformly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new LinkStudentByCodeCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCodeCommand(
            WorkspaceId: _workspaceId,
            StudentCode: "STU-2026-00000",
            VerificationFactor: "01096817311",
            GradeId: null,
            GuardianName: null,
            GuardianPhone: "01096817311",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("STUDENT_ENROLLMENT_VERIFICATION_FAILED");
        await _securityService.Received(1).ExecuteDummyVerificationDelayAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMismatchedPhone_ShouldTriggerDummyDelayAndFailUniformly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var student = new Student { StudentCode = "STU-2026-22222", FullName = "يوسف", IsActive = true };
        context.Students.Add(student);
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = Guid.NewGuid(),
            StudentId = student.Id,
            GuardianPhone = "+201096817311"
        });
        await context.SaveChangesAsync();

        var handler = new LinkStudentByCodeCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCodeCommand(
            WorkspaceId: _workspaceId,
            StudentCode: "STU-2026-22222",
            VerificationFactor: "01299999999", // Wrong phone
            GradeId: null,
            GuardianName: null,
            GuardianPhone: "01096817311",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("STUDENT_ENROLLMENT_VERIFICATION_FAILED");
        await _securityService.Received(1).ExecuteDummyVerificationDelayAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenGuardianPhoneExistsInWorkspaceAndAllowSiblingFalse_ShouldReturnConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var student = new Student { StudentCode = "STU-2026-33333", FullName = "سالم", IsActive = true };
        context.Students.Add(student);
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = Guid.NewGuid(), // enrolled in center A
            StudentId = student.Id,
            GuardianPhone = "+201096817311"
        });

        // Another student already has this phone in target _workspaceId
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = Guid.NewGuid(),
            GuardianPhone = "+201096817311",
            Status = WorkspaceStudentStatus.Active
        });
        await context.SaveChangesAsync();

        var handler = new LinkStudentByCodeCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCodeCommand(
            WorkspaceId: _workspaceId,
            StudentCode: "STU-2026-33333",
            VerificationFactor: "01096817311",
            GradeId: 10,
            GuardianName: null,
            GuardianPhone: "01096817311",
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
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotWorkspaceMember_ShouldReturnForbidden()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(Guid.NewGuid());
        currentUserService.IsPlatformAdmin.Returns(false);

        var handler = new LinkStudentByCodeCommandHandler(
            new UnitOfWork(context),
            _securityService,
            currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCodeCommand(
            WorkspaceId: _workspaceId,
            StudentCode: "STU-2026-33333",
            VerificationFactor: "01096817311",
            GradeId: 10,
            GuardianName: null,
            GuardianPhone: "01096817311",
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

    [Fact]
    public async Task Handle_WhenWorkspaceSuspended_ShouldReturnForbidden()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var student = new Student { StudentCode = "STU-2026-33333", FullName = "سالم", IsActive = true };
        context.Students.Add(student);
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = Guid.NewGuid(),
            StudentId = student.Id,
            GuardianPhone = "+201096817311"
        });

        var plan = new SubscriptionPlan { Code = "S1", NameAr = "خطة", NameEn = "P", MaxStudents = 50, IsActive = true };
        context.SubscriptionPlans.Add(plan);
        context.WorkspaceSubscriptions.Add(new WorkspaceSubscription
        {
            WorkspaceId = _workspaceId,
            PlanId = plan.Id,
            Plan = plan,
            StartsAt = DateTimeOffset.UtcNow.AddDays(-10),
            EndsAt = DateTimeOffset.UtcNow.AddDays(20),
            Status = "Suspended",
            ManualSuspendedAt = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var handler = new LinkStudentByCodeCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCodeCommand(
            WorkspaceId: _workspaceId,
            StudentCode: "STU-2026-33333",
            VerificationFactor: "01096817311",
            GradeId: 10,
            GuardianName: null,
            GuardianPhone: "01096817311",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WORKSPACE_SUSPENDED");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }
}
