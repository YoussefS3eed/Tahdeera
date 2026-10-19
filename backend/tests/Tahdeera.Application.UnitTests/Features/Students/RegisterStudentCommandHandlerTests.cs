using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Students.Commands.RegisterStudent;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Students;

public class RegisterStudentCommandHandlerTests
{
    private readonly ICredentialSecurityService _securityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public RegisterStudentCommandHandlerTests()
    {
        _securityService = Substitute.For<ICredentialSecurityService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _auditLogService = Substitute.For<IAuditLogService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _currentUserService.UserId.Returns(_userId);
        _currentUserService.IsPlatformAdmin.Returns(true);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        _securityService.GenerateStudentCode(Arg.Any<int>())
            .Returns("STU-2026-12345");
        _securityService.HashToken(Arg.Any<string>())
            .Returns(x => System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes((string)x[0])));
    }

    [Fact]
    public async Task Handle_WhenValidRegistration_ShouldSucceedAndPersistEntities()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.AcademicYears.Add(new AcademicYear
        {
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        });
        context.Grades.Add(new Grade
        {
            Id = 10,
            Code = "SEC1",
            NameAr = "الصف الأول الثانوي",
            NameEn = "Secondary 1",
            SortOrder = 10,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var handler = new RegisterStudentCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new RegisterStudentCommand(
            WorkspaceId: _workspaceId,
            FullName: "أحمد علي",
            GradeId: 10,
            GuardianName: "علي حسن",
            GuardianPhone: "01096817311",
            StudentPhone: "01234567890",
            WhatsappOptIn: true,
            SeatNumber: "A-12",
            LocalNotes: "ملاحظات",
            CardToken: "card_secret_123",
            CardUidDisplay: "Card #101"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StudentCode.Should().Be("STU-2026-12345");
        result.Value.GuardianPhone.Should().Be("+201096817311");
        result.Value.AssignedCredentialId.Should().NotBeNull();

        context.Students.Should().ContainSingle(s => s.FullName == "أحمد علي");
        context.WorkspaceStudents.Should().ContainSingle(ws => ws.WorkspaceId == _workspaceId && ws.GuardianPhone == "+201096817311");
        context.StudentCredentials.Should().ContainSingle(c => c.WorkspaceId == _workspaceId);
    }

    [Fact]
    public async Task Handle_WhenQuotaExceeded_ShouldReturnUnprocessable()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.AcademicYears.Add(new AcademicYear { IsCurrent = true, StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30) });
        var plan = new SubscriptionPlan
        {
            Code = "STARTER",
            NameAr = "البداية",
            NameEn = "Starter",
            MaxStudents = 1,
            IsActive = true
        };
        context.SubscriptionPlans.Add(plan);
        context.WorkspaceSubscriptions.Add(new WorkspaceSubscription
        {
            WorkspaceId = _workspaceId,
            PlanId = plan.Id,
            Plan = plan,
            StartsAt = DateTimeOffset.UtcNow.AddDays(-10),
            EndsAt = DateTimeOffset.UtcNow.AddDays(20),
            Status = "Active"
        });
        // 1 active student already exists
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = Guid.NewGuid(),
            Status = WorkspaceStudentStatus.Active,
            GuardianPhone = "+201000000001"
        });
        await context.SaveChangesAsync();

        var handler = new RegisterStudentCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new RegisterStudentCommand(
            WorkspaceId: _workspaceId,
            FullName: "محمد كمال",
            GradeId: 10,
            GuardianName: null,
            GuardianPhone: "01096817311",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUOTA_MAX_STUDENTS_REACHED");
        result.Error.Type.Should().Be(ErrorType.Unprocessable);
    }

    [Fact]
    public async Task Handle_WhenDuplicateGuardianPhoneWithoutSiblingConfirmation_ShouldReturnConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.AcademicYears.Add(new AcademicYear { IsCurrent = true, StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30) });
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = Guid.NewGuid(),
            GuardianPhone = "+201096817311",
            Status = WorkspaceStudentStatus.Active
        });
        await context.SaveChangesAsync();

        var handler = new RegisterStudentCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new RegisterStudentCommand(
            WorkspaceId: _workspaceId,
            FullName: "طالب جديد شقيق",
            GradeId: 10,
            GuardianName: null,
            GuardianPhone: "01096817311",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null,
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
    public async Task Handle_WhenDuplicateGuardianPhoneWithSiblingConfirmation_ShouldSucceed()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.AcademicYears.Add(new AcademicYear { IsCurrent = true, StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30) });
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = Guid.NewGuid(),
            GuardianPhone = "+201096817311",
            Status = WorkspaceStudentStatus.Active
        });
        await context.SaveChangesAsync();

        var handler = new RegisterStudentCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new RegisterStudentCommand(
            WorkspaceId: _workspaceId,
            FullName: "طالب جديد شقيق",
            GradeId: 10,
            GuardianName: null,
            GuardianPhone: "01096817311",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null,
            AllowSiblingPhone: true
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserIsNotWorkspaceMember_ShouldReturnForbidden()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(Guid.NewGuid());
        currentUserService.IsPlatformAdmin.Returns(false);

        var handler = new RegisterStudentCommandHandler(
            new UnitOfWork(context),
            _securityService,
            currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new RegisterStudentCommand(
            WorkspaceId: _workspaceId,
            FullName: "طالب جديد",
            GradeId: 10,
            GuardianName: null,
            GuardianPhone: "01096817311",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null
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
        context.AcademicYears.Add(new AcademicYear { IsCurrent = true, StartsOn = new DateOnly(2026, 9, 1), EndsOn = new DateOnly(2027, 6, 30) });

        var plan = new SubscriptionPlan
        {
            Code = "SUSP",
            NameAr = "خطة",
            NameEn = "Plan",
            MaxStudents = 100,
            IsActive = true
        };
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

        var handler = new RegisterStudentCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new RegisterStudentCommand(
            WorkspaceId: _workspaceId,
            FullName: "طالب جديد",
            GradeId: 10,
            GuardianName: null,
            GuardianPhone: "01096817311",
            StudentPhone: null,
            WhatsappOptIn: false,
            SeatNumber: null,
            LocalNotes: null,
            CardToken: null,
            CardUidDisplay: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WORKSPACE_SUSPENDED");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }
}

