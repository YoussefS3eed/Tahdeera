using FluentAssertions;
using NSubstitute;
using System.Linq.Expressions;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Workspaces.Commands.ReactivateWorkspace;
using Tahdeera.Application.Features.Workspaces.Commands.SuspendWorkspace;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Workspaces;

public class WorkspaceSuspensionCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IGenericRepository<WorkspaceSubscription> _workspaceSubscriptionRepository;

    private readonly SuspendWorkspaceCommandHandler _suspendHandler;
    private readonly ReactivateWorkspaceCommandHandler _reactivateHandler;

    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly DateTimeOffset _fixedNow = new(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);

    public WorkspaceSuspensionCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _auditLogService = Substitute.For<IAuditLogService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _workspaceSubscriptionRepository = Substitute.For<IGenericRepository<WorkspaceSubscription>>();

        _unitOfWork.WorkspaceSubscriptions.Returns(_workspaceSubscriptionRepository);
        _dateTimeProvider.UtcNow.Returns(_fixedNow);

        _suspendHandler = new SuspendWorkspaceCommandHandler(
            _unitOfWork,
            _auditLogService,
            _currentUserService,
            _dateTimeProvider);

        _reactivateHandler = new ReactivateWorkspaceCommandHandler(
            _unitOfWork,
            _auditLogService,
            _currentUserService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Suspend_UnauthenticatedUser_ReturnsUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var cmd = new SuspendWorkspaceCommand(_workspaceId, "Non-payment");
        var result = await _suspendHandler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_UNAUTHORIZED");
    }

    [Fact]
    public async Task Suspend_NonPlatformAdmin_ReturnsForbidden()
    {
        _currentUserService.UserId.Returns(_adminUserId);
        _currentUserService.IsPlatformAdmin.Returns(false);

        var cmd = new SuspendWorkspaceCommand(_workspaceId, "Non-payment");
        var result = await _suspendHandler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ROLE_FORBIDDEN");
    }

    [Fact]
    public async Task Suspend_NoSubscriptionFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns(_adminUserId);
        _currentUserService.IsPlatformAdmin.Returns(true);
        _workspaceSubscriptionRepository.FindAsync(
                Arg.Any<Expression<Func<WorkspaceSubscription, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceSubscription>());

        var cmd = new SuspendWorkspaceCommand(_workspaceId, "Non-payment");
        var result = await _suspendHandler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WORKSPACE_NOT_FOUND");
    }

    [Fact]
    public async Task Suspend_ValidAdmin_SuspendsSubscriptionAndSaves()
    {
        _currentUserService.UserId.Returns(_adminUserId);
        _currentUserService.IsPlatformAdmin.Returns(true);

        var sub = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            StartsAt = _fixedNow.AddMonths(-1),
            EndsAt = _fixedNow.AddMonths(11),
            Status = "Active"
        };
        _workspaceSubscriptionRepository.FindAsync(
                Arg.Any<Expression<Func<WorkspaceSubscription, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceSubscription> { sub });

        var cmd = new SuspendWorkspaceCommand(_workspaceId, "Violated fair usage policy");
        var result = await _suspendHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sub.ManualSuspendedAt.Should().Be(_fixedNow);
        sub.ManualSuspendedByUserId.Should().Be(_adminUserId);
        sub.SuspensionReason.Should().Be("Violated fair usage policy");

        _workspaceSubscriptionRepository.Received(1).Update(sub);
        await _auditLogService.Received(1).LogAsync(
            _adminUserId,
            _workspaceId,
            "WORKSPACE_SUSPENDED",
            "WorkspaceSubscription",
            sub.Id.ToString(),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reactivate_UnauthenticatedUser_ReturnsUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var cmd = new ReactivateWorkspaceCommand(_workspaceId);
        var result = await _reactivateHandler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_UNAUTHORIZED");
    }

    [Fact]
    public async Task Reactivate_NonPlatformAdmin_ReturnsForbidden()
    {
        _currentUserService.UserId.Returns(_adminUserId);
        _currentUserService.IsPlatformAdmin.Returns(false);

        var cmd = new ReactivateWorkspaceCommand(_workspaceId);
        var result = await _reactivateHandler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ROLE_FORBIDDEN");
    }

    [Fact]
    public async Task Reactivate_NoSubscriptionFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns(_adminUserId);
        _currentUserService.IsPlatformAdmin.Returns(true);
        _workspaceSubscriptionRepository.FindAsync(
                Arg.Any<Expression<Func<WorkspaceSubscription, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceSubscription>());

        var cmd = new ReactivateWorkspaceCommand(_workspaceId);
        var result = await _reactivateHandler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WORKSPACE_NOT_FOUND");
    }

    [Fact]
    public async Task Reactivate_ValidAdmin_ReactivatesSubscriptionAndSaves()
    {
        _currentUserService.UserId.Returns(_adminUserId);
        _currentUserService.IsPlatformAdmin.Returns(true);

        var sub = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            StartsAt = _fixedNow.AddMonths(-1),
            EndsAt = _fixedNow.AddMonths(11),
            Status = "Active",
            ManualSuspendedAt = _fixedNow.AddDays(-2),
            ManualSuspendedByUserId = _adminUserId,
            SuspensionReason = "Previous violation"
        };
        _workspaceSubscriptionRepository.FindAsync(
                Arg.Any<Expression<Func<WorkspaceSubscription, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceSubscription> { sub });

        var cmd = new ReactivateWorkspaceCommand(_workspaceId);
        var result = await _reactivateHandler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sub.ManualSuspendedAt.Should().BeNull();
        sub.ManualSuspendedByUserId.Should().BeNull();
        sub.SuspensionReason.Should().BeNull();
        sub.Status.Should().Be("Active");

        _workspaceSubscriptionRepository.Received(1).Update(sub);
        await _auditLogService.Received(1).LogAsync(
            _adminUserId,
            _workspaceId,
            "WORKSPACE_REACTIVATED",
            "WorkspaceSubscription",
            sub.Id.ToString(),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
