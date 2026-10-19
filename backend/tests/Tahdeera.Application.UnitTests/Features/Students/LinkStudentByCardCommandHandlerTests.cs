using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Students.Commands.LinkStudentByCard;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Students;

public class LinkStudentByCardCommandHandlerTests
{
    private readonly ICredentialSecurityService _securityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public LinkStudentByCardCommandHandlerTests()
    {
        _securityService = Substitute.For<ICredentialSecurityService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _auditLogService = Substitute.For<IAuditLogService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _currentUserService.UserId.Returns(_userId);
        _currentUserService.IsPlatformAdmin.Returns(true);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        _securityService.HashToken(Arg.Any<string>())
            .Returns(x => System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes((string)x[0])));
    }

    [Fact]
    public async Task Handle_WhenValidCard_ShouldLinkStudentAndInheritGrade()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var activeYear = new AcademicYear
        {
            Name = "2026/2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        };
        context.AcademicYears.Add(activeYear);

        var grade = new Grade
        {
            Id = 11,
            Code = "SEC2",
            NameAr = "الصف الثاني الثانوي",
            NameEn = "Secondary 2",
            SortOrder = 11,
            IsActive = true
        };
        context.Grades.Add(grade);

        var student = new Student
        {
            StudentCode = "STU-2026-99999",
            FullName = "كريم حسن",
            IsActive = true
        };
        context.Students.Add(student);

        // Grade history for active year exists
        context.StudentAcademicHistories.Add(new StudentAcademicHistory
        {
            StudentId = student.Id,
            AcademicYearId = activeYear.Id,
            GradeId = grade.Id
        });

        // Active credential issued by Center A
        var token = "nfc_token_student_karim";
        var tokenHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        context.StudentCredentials.Add(new StudentCredential
        {
            StudentId = student.Id,
            WorkspaceId = Guid.NewGuid(), // Center A
            CredentialType = CredentialType.Nfc,
            TokenHash = tokenHash,
            RevokedAt = null
        });

        await context.SaveChangesAsync();

        var handler = new LinkStudentByCardCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCardCommand(
            WorkspaceId: _workspaceId, // Center B
            ScannedToken: token,
            GradeId: null, // Grade is omitted and should be inherited
            GuardianName: "حسن علي",
            GuardianPhone: "01011112222",
            StudentPhone: null,
            WhatsappOptIn: true,
            SeatNumber: "B-10",
            LocalNotes: "ملاحظات سنتر ب"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StudentId.Should().Be(student.Id);
        result.Value.GradeId.Should().Be(11); // Automatically inherited
        result.Value.GuardianPhone.Should().Be("+201011112222");

        context.WorkspaceStudents.Should().ContainSingle(ws => ws.WorkspaceId == _workspaceId && ws.StudentId == student.Id);
    }

    [Fact]
    public async Task Handle_WhenCardIsRevoked_ShouldReturnValidationError()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var token = "revoked_token_123";
        var tokenHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));

        var student = new Student { FullName = "سامي", IsActive = true };
        context.Students.Add(student);
        context.StudentCredentials.Add(new StudentCredential
        {
            StudentId = student.Id,
            WorkspaceId = Guid.NewGuid(),
            TokenHash = tokenHash,
            RevokedAt = DateTimeOffset.UtcNow.AddDays(-1),
            RevocationReason = "Card Lost"
        });
        await context.SaveChangesAsync();

        var handler = new LinkStudentByCardCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCardCommand(
            WorkspaceId: _workspaceId,
            ScannedToken: token,
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
        result.Error.Code.Should().Be("INVALID_OR_REVOKED_CARD");
    }

    [Fact]
    public async Task Handle_WhenAlreadyEnrolledInWorkspace_ShouldReturnConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var token = "card_token_enrolled";
        var tokenHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));

        var student = new Student { FullName = "علي", IsActive = true };
        context.Students.Add(student);
        context.StudentCredentials.Add(new StudentCredential
        {
            StudentId = student.Id,
            WorkspaceId = _workspaceId,
            TokenHash = tokenHash,
            RevokedAt = null
        });
        // Already enrolled
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = student.Id,
            Status = WorkspaceStudentStatus.Active
        });
        await context.SaveChangesAsync();

        var handler = new LinkStudentByCardCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCardCommand(
            WorkspaceId: _workspaceId,
            ScannedToken: token,
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
        result.Error.Code.Should().Be("STUDENT_ALREADY_ENROLLED_IN_WORKSPACE");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenGuardianPhoneExistsInWorkspaceAndAllowSiblingFalse_ShouldReturnConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var token = "card_token_sibling";
        var tokenHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));

        var student = new Student { FullName = "علي", IsActive = true };
        context.Students.Add(student);
        context.StudentCredentials.Add(new StudentCredential
        {
            StudentId = student.Id,
            WorkspaceId = Guid.NewGuid(), // different center
            TokenHash = tokenHash,
            RevokedAt = null
        });

        // Another student already has this phone in _workspaceId
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = Guid.NewGuid(),
            GuardianPhone = "+201096817311",
            Status = WorkspaceStudentStatus.Active
        });
        await context.SaveChangesAsync();

        var handler = new LinkStudentByCardCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCardCommand(
            WorkspaceId: _workspaceId,
            ScannedToken: token,
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

        var handler = new LinkStudentByCardCommandHandler(
            new UnitOfWork(context),
            _securityService,
            currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCardCommand(
            WorkspaceId: _workspaceId,
            ScannedToken: "some_token",
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
        var token = "card_token_susp";
        var tokenHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));

        var student = new Student { FullName = "علي", IsActive = true };
        context.Students.Add(student);
        context.StudentCredentials.Add(new StudentCredential
        {
            StudentId = student.Id,
            WorkspaceId = Guid.NewGuid(),
            TokenHash = tokenHash,
            RevokedAt = null
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

        var handler = new LinkStudentByCardCommandHandler(
            new UnitOfWork(context),
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new LinkStudentByCardCommand(
            WorkspaceId: _workspaceId,
            ScannedToken: token,
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
