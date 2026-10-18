using FluentAssertions;
using NSubstitute;
using System.Linq.Expressions;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Workspaces.Commands.AddWorkspaceMember;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Workspaces;

public class AddWorkspaceMemberCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly AddWorkspaceMemberCommandHandler _handler;

    private readonly Guid _ownerUserId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    public AddWorkspaceMemberCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _identityService = Substitute.For<IIdentityService>();
        _auditLogService = Substitute.For<IAuditLogService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(_fixedTime);
        _currentUserService.UserId.Returns(_ownerUserId);
        _currentUserService.IsPlatformAdmin.Returns(false);

        _handler = new AddWorkspaceMemberCommandHandler(
            _unitOfWork,
            _identityService,
            _auditLogService,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_NonOwnerCaller_ShouldReturnForbidden()
    {
        // Arrange
        _unitOfWork.WorkspaceMembers.AnyAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new AddWorkspaceMemberCommand(
            _workspaceId,
            "teacher1",
            "teacher1@example.com",
            "Password123!",
            "Teacher One",
            "01012345678",
            "Teacher");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ROLE_FORBIDDEN");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_QuotaReached_ShouldReturnUnprocessableEntity()
    {
        // Arrange: caller is owner
        _unitOfWork.WorkspaceMembers.AnyAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Subscription has MaxStaffOverride = 2
        var subscription = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            MaxStaffOverride = 2,
            EndsAt = _fixedTime.AddDays(30),
            Status = "Active"
        };
        _unitOfWork.WorkspaceSubscriptions.FindAsync(
            Arg.Any<Expression<Func<WorkspaceSubscription, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceSubscription> { subscription });

        // Active staff count is already 2
        _unitOfWork.WorkspaceMembers.CountAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(2);

        var command = new AddWorkspaceMemberCommand(
            _workspaceId,
            "teacher3",
            "teacher3@example.com",
            "Password123!",
            "Teacher Three",
            "01012345678",
            "Teacher");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("QUOTA_MAX_STAFF_REACHED");
        result.Error.Type.Should().Be(ErrorType.Unprocessable);
    }

    [Fact]
    public async Task Handle_NewUserWithinQuota_ShouldCreateUserAndMember()
    {
        // Arrange
        _unitOfWork.WorkspaceMembers.AnyAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var subscription = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            MaxStaffOverride = 5,
            EndsAt = _fixedTime.AddDays(30),
            Status = "Active"
        };
        _unitOfWork.WorkspaceSubscriptions.FindAsync(
            Arg.Any<Expression<Func<WorkspaceSubscription, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceSubscription> { subscription });

        _unitOfWork.WorkspaceMembers.CountAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(1);

        // User does not exist
        _unitOfWork.AppUsers.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<AppUser, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var newUserId = Guid.NewGuid();
        _identityService.CreateUserAsync(
            "new_teacher",
            "new.teacher@example.com",
            "Password123!",
            false,
            Arg.Any<CancellationToken>())
            .Returns((true, newUserId, Array.Empty<string>()));

        var command = new AddWorkspaceMemberCommand(
            _workspaceId,
            "new_teacher",
            "new.teacher@example.com",
            "Password123!",
            "New Teacher",
            "01012345678",
            "Teacher");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("New Teacher");
        result.Value.Role.Should().Be("Teacher");
        result.Value.IsActive.Should().BeTrue();

        await _unitOfWork.AppUsers.Received(1).AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        await _unitOfWork.WorkspaceMembers.Received(1).AddAsync(Arg.Any<WorkspaceMember>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingUserNotInWorkspace_ShouldLinkMemberWithoutIdentityCall()
    {
        // Arrange
        _unitOfWork.WorkspaceMembers.AnyAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var subscription = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            MaxStaffOverride = 5,
            EndsAt = _fixedTime.AddDays(30),
            Status = "Active"
        };
        _unitOfWork.WorkspaceSubscriptions.FindAsync(
            Arg.Any<Expression<Func<WorkspaceSubscription, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceSubscription> { subscription });

        _unitOfWork.WorkspaceMembers.CountAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(0);

        var existingUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "existing_user",
            DisplayName = "Existing User",
            NormalizedEmail = "EXISTING@EXAMPLE.COM"
        };

        _unitOfWork.AppUsers.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<AppUser, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(existingUser);

        // Not yet member of workspace
        _unitOfWork.WorkspaceMembers.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns((WorkspaceMember?)null);

        var command = new AddWorkspaceMemberCommand(
            _workspaceId,
            "existing_user",
            "existing@example.com",
            "AnyPassword123!",
            "Existing User",
            null,
            "Assistant");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(existingUser.Id);
        result.Value.Role.Should().Be("Assistant");

        // Must NOT create identity user or update password
        await _identityService.DidNotReceiveWithAnyArgs().CreateUserAsync(default!, default!, default!, default, default);
        await _unitOfWork.WorkspaceMembers.Received(1).AddAsync(Arg.Any<WorkspaceMember>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
