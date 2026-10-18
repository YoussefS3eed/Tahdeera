using FluentAssertions;
using NSubstitute;
using System.Linq.Expressions;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Workspaces.Commands.DeactivateWorkspaceMember;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Workspaces;

public class DeactivateWorkspaceMemberCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly DeactivateWorkspaceMemberCommandHandler _handler;

    private readonly Guid _ownerUserId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    public DeactivateWorkspaceMemberCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _auditLogService = Substitute.For<IAuditLogService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(_fixedTime);
        _currentUserService.UserId.Returns(_ownerUserId);
        _currentUserService.IsPlatformAdmin.Returns(false);

        _handler = new DeactivateWorkspaceMemberCommandHandler(
            _unitOfWork,
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

        var command = new DeactivateWorkspaceMemberCommand(_workspaceId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ROLE_FORBIDDEN");
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_MemberNotFound_ShouldReturnNotFound()
    {
        // Arrange: caller is owner
        _unitOfWork.WorkspaceMembers.AnyAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        _unitOfWork.WorkspaceMembers.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns((WorkspaceMember?)null);

        var command = new DeactivateWorkspaceMemberCommand(_workspaceId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MEMBER_NOT_FOUND");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_AttemptToDeactivateOwner_ShouldReturnValidationFailure()
    {
        // Arrange: caller is owner
        _unitOfWork.WorkspaceMembers.AnyAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var ownerMember = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            UserId = _ownerUserId,
            Role = "Owner",
            IsActive = true
        };

        _unitOfWork.WorkspaceMembers.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(ownerMember);

        var command = new DeactivateWorkspaceMemberCommand(_workspaceId, ownerMember.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CANNOT_DEACTIVATE_OWNER");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_DeactivateStaff_ShouldMarkInactiveAndRevokeActiveRefreshTokens()
    {
        // Arrange: caller is owner
        _unitOfWork.WorkspaceMembers.AnyAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var staffUserId = Guid.NewGuid();
        var staffMember = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            UserId = staffUserId,
            Role = "Teacher",
            IsActive = true
        };

        _unitOfWork.WorkspaceMembers.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<WorkspaceMember, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(staffMember);

        // Active refresh tokens exist for this user
        var activeToken = RefreshToken.Create(staffUserId, [1, 2, 3], _fixedTime.AddDays(7));
        _unitOfWork.RefreshTokens.FindAsync(
            Arg.Any<Expression<Func<RefreshToken, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken> { activeToken });

        var command = new DeactivateWorkspaceMemberCommand(_workspaceId, staffMember.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
        result.Value.LeftAt.Should().Be(_fixedTime);

        staffMember.IsActive.Should().BeFalse();
        staffMember.LeftAt.Should().Be(_fixedTime);

        activeToken.RevokedAt.Should().Be(_fixedTime);
        _unitOfWork.RefreshTokens.Received(1).Update(activeToken);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
