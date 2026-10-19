using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Credentials.Commands.IssueNfcCredential;
using Tahdeera.Application.Features.Credentials.Commands.IssueQrCredential;
using Tahdeera.Application.Features.Credentials.Commands.RevokeCredential;
using Tahdeera.Application.Features.Credentials.Queries.GetStudentCredentials;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Credentials;

public class CredentialCommandTests
{
    private readonly ICredentialSecurityService _securityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();

    public CredentialCommandTests()
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
        _securityService.GenerateQrPayload()
            .Returns("tahdeera://qr/0123456789abcdef0123456789abcdef");
    }

    [Fact]
    public async Task IssueNfc_WhenActiveCardExists_ShouldRevokePriorCardAndIssueNew()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = _studentId,
            Status = WorkspaceStudentStatus.Active
        });

        var oldTokenHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("card_old"));
        var oldCard = new StudentCredential
        {
            StudentId = _studentId,
            WorkspaceId = _workspaceId,
            CredentialType = CredentialType.Nfc,
            TokenHash = oldTokenHash,
            RevokedAt = null
        };
        context.StudentCredentials.Add(oldCard);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);

        var handler = new IssueNfcCredentialCommandHandler(
            unitOfWork,
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new IssueNfcCredentialCommand(_workspaceId, _studentId, "card_new", "Card #200");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReplacedCredentialId.Should().Be(oldCard.Id);

        var updatedOldCard = await context.StudentCredentials.FindAsync(oldCard.Id);
        updatedOldCard!.RevokedAt.Should().NotBeNull();
        updatedOldCard.RevocationReason.Should().Be("Replaced by new card issuance");

        var newCard = await context.StudentCredentials.FindAsync(result.Value.CredentialId);
        newCard.Should().NotBeNull();
        newCard!.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task IssueNfc_WhenTokenAlreadyRegistered_ShouldReturnConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = _studentId,
            Status = WorkspaceStudentStatus.Active
        });

        var token = "duplicate_token";
        var tokenHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        context.StudentCredentials.Add(new StudentCredential
        {
            StudentId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            CredentialType = CredentialType.Nfc,
            TokenHash = tokenHash
        });
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);

        var handler = new IssueNfcCredentialCommandHandler(
            unitOfWork,
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new IssueNfcCredentialCommand(_workspaceId, _studentId, token, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CREDENTIAL_TOKEN_ALREADY_EXISTS");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task IssueQr_ShouldGenerateRawPayloadAndRevokePriorQr()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        context.WorkspaceStudents.Add(new WorkspaceStudent
        {
            WorkspaceId = _workspaceId,
            StudentId = _studentId,
            Status = WorkspaceStudentStatus.Active
        });

        var oldQrHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("qr_old"));
        var oldQr = new StudentCredential
        {
            StudentId = _studentId,
            WorkspaceId = _workspaceId,
            CredentialType = CredentialType.Qr,
            TokenHash = oldQrHash,
            RevokedAt = null
        };
        context.StudentCredentials.Add(oldQr);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);

        var handler = new IssueQrCredentialCommandHandler(
            unitOfWork,
            _securityService,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new IssueQrCredentialCommand(_workspaceId, _studentId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RawPayload.Should().StartWith("tahdeera://qr/");
        result.Value.ReplacedCredentialId.Should().Be(oldQr.Id);

        var updatedOldQr = await context.StudentCredentials.FindAsync(oldQr.Id);
        updatedOldQr!.RevokedAt.Should().NotBeNull();
        updatedOldQr.RevocationReason.Should().Be("Replaced by new QR code");
    }

    [Fact]
    public async Task RevokeCredential_WhenAlreadyRevoked_ShouldReturnConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var card = new StudentCredential
        {
            StudentId = _studentId,
            WorkspaceId = _workspaceId,
            CredentialType = CredentialType.Nfc,
            RevokedAt = DateTimeOffset.UtcNow.AddHours(-1),
            RevocationReason = "Lost"
        };
        context.StudentCredentials.Add(card);
        await context.SaveChangesAsync();

        var unitOfWork = new UnitOfWork(context);

        var handler = new RevokeCredentialCommandHandler(
            unitOfWork,
            _currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new RevokeCredentialCommand(_workspaceId, card.Id, "Trying again");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CREDENTIAL_ALREADY_REVOKED");
    }

    [Fact]
    public async Task GetStudentCredentials_ShouldReturnAllWorkspaceCredentials()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user = new AppUser { Id = _userId, Username = "staff1", DisplayName = "أحمد الموظف" };
        context.AppUsers.Add(user);

        context.StudentCredentials.AddRange(
            new StudentCredential
            {
                StudentId = _studentId,
                WorkspaceId = _workspaceId,
                CredentialType = CredentialType.Nfc,
                TokenHash = new byte[] { 1, 2, 3 },
                IssuedAt = DateTimeOffset.UtcNow.AddDays(-10),
                RevokedAt = DateTimeOffset.UtcNow.AddDays(-5),
                RevocationReason = "Replaced",
                IssuedByUserId = user.Id
            },
            new StudentCredential
            {
                StudentId = _studentId,
                WorkspaceId = _workspaceId,
                CredentialType = CredentialType.Nfc,
                TokenHash = new byte[] { 4, 5, 6 },
                IssuedAt = DateTimeOffset.UtcNow.AddDays(-5),
                RevokedAt = null,
                IssuedByUserId = user.Id
            }
        );
        await context.SaveChangesAsync();

        var handler = new GetStudentCredentialsQueryHandler(new StudentCredentialRepository(context));

        // Act
        var result = await handler.Handle(new GetStudentCredentialsQuery(_workspaceId, _studentId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].IsActive.Should().BeTrue();
        result.Value[1].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetStudentCredentials_WhenStudentNotEnrolledInWorkspace_ShouldReturnNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var handler = new GetStudentCredentialsQueryHandler(
            new StudentCredentialRepository(context),
            new WorkspaceStudentRepository(context),
            _currentUserService,
            new UnitOfWork(context));

        // Act
        var result = await handler.Handle(new GetStudentCredentialsQuery(_workspaceId, Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("STUDENT_NOT_FOUND_IN_WORKSPACE");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task IssueNfc_WhenUserIsNotWorkspaceMember_ShouldReturnForbidden()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(Guid.NewGuid());
        currentUserService.IsPlatformAdmin.Returns(false);

        var handler = new IssueNfcCredentialCommandHandler(
            new UnitOfWork(context),
            _securityService,
            currentUserService,
            _auditLogService,
            _dateTimeProvider);

        var command = new IssueNfcCredentialCommand(
            WorkspaceId: _workspaceId,
            StudentId: _studentId,
            Token: "some_token",
            CardUidDisplay: "Card 1"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_WORKSPACE_ACCESS");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }
}
