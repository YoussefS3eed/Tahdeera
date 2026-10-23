using FluentAssertions;
using NSubstitute;
using System.Linq.Expressions;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Features.Workspaces;

public class CreateWorkspaceCommandHandlerTests
{
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGenericRepository<AppUser> _appUserRepository;
    private readonly IGenericRepository<WorkspaceMember> _workspaceMemberRepository;
    private readonly IGenericRepository<WorkspaceSubscription> _workspaceSubscriptionRepository;
    private readonly IGenericRepository<SubscriptionPayment> _subscriptionPaymentRepository;

    private readonly CreateWorkspaceCommandHandler _handler;
    private readonly Guid _adminUserId = Guid.NewGuid();

    public CreateWorkspaceCommandHandlerTests()
    {
        _subscriptionPlanRepository = Substitute.For<ISubscriptionPlanRepository>();
        _workspaceRepository = Substitute.For<IWorkspaceRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _identityService = Substitute.For<IIdentityService>();
        _auditLogService = Substitute.For<IAuditLogService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _appUserRepository = Substitute.For<IGenericRepository<AppUser>>();
        _workspaceMemberRepository = Substitute.For<IGenericRepository<WorkspaceMember>>();
        _workspaceSubscriptionRepository = Substitute.For<IGenericRepository<WorkspaceSubscription>>();
        _subscriptionPaymentRepository = Substitute.For<IGenericRepository<SubscriptionPayment>>();

        _unitOfWork.AppUsers.Returns(_appUserRepository);
        _unitOfWork.Workspaces.Returns(_workspaceRepository);
        _unitOfWork.WorkspaceMembers.Returns(_workspaceMemberRepository);
        _unitOfWork.WorkspaceSubscriptions.Returns(_workspaceSubscriptionRepository);
        _unitOfWork.SubscriptionPayments.Returns(_subscriptionPaymentRepository);

        _currentUserService.UserId.Returns(_adminUserId);
        _currentUserService.IsPlatformAdmin.Returns(true);

        _handler = new CreateWorkspaceCommandHandler(
            _subscriptionPlanRepository,
            _workspaceRepository,
            _unitOfWork,
            _identityService,
            _auditLogService,
            _currentUserService);
    }

    private static CreateWorkspaceCommand CreateValidCommand(Guid planId)
    {
        return new CreateWorkspaceCommand(
            Name: "سنتر التفوق",
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: "01012345678",
            CityAddress: "Alexandria",
            DefaultLateGraceMinutes: 15,
            Owner: new CreateOwnerDto(
                Username: "owner_hassan",
                Email: "hassan@tahdeera.com",
                Password: "SecurePassword123!",
                DisplayName: "أستاذ حسن",
                Phone: "01012345678"),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: planId,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddMonths(12),
                AgreedPrice: 2000m,
                InitialPayment: new CreateInitialPaymentDto(
                    Amount: 2000m,
                    Method: "Cash",
                    Reference: "DEP-01",
                    Notes: "Paid in full")));
    }

    [Fact]
    public async Task Handle_PlanNotFound_ReturnsNotFoundError()
    {
        var planId = Guid.NewGuid();
        var cmd = CreateValidCommand(planId);
        _subscriptionPlanRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>())
            .Returns((SubscriptionPlan?)null);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PLAN_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_UserOrEmailAlreadyExists_ReturnsConflictError()
    {
        var planId = Guid.NewGuid();
        var cmd = CreateValidCommand(planId);
        var plan = new SubscriptionPlan { Id = planId, Code = "STANDARD" };

        _subscriptionPlanRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>())
            .Returns(plan);
        _appUserRepository.AnyAsync(Arg.Any<Expression<Func<AppUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("USER_ALREADY_EXISTS");
    }

    [Fact]
    public async Task Handle_IdentityCreationFailed_ReturnsFailureError()
    {
        var planId = Guid.NewGuid();
        var cmd = CreateValidCommand(planId);
        var plan = new SubscriptionPlan { Id = planId, Code = "STANDARD" };

        _subscriptionPlanRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>())
            .Returns(plan);
        _appUserRepository.AnyAsync(Arg.Any<Expression<Func<AppUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _identityService.CreateUserAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((Success: false, UserId: Guid.Empty, Errors: new[] { "Password does not meet requirements" }));

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IDENTITY_CREATION_FAILED");
    }

    [Fact]
    public async Task Handle_ValidRequest_ProvisionsWorkspaceAndReturnsSuccess()
    {
        var planId = Guid.NewGuid();
        var cmd = CreateValidCommand(planId);
        var plan = new SubscriptionPlan { Id = planId, Code = "PRO_CENTER" };
        var newIdentityUserId = Guid.NewGuid();

        _subscriptionPlanRepository.GetByIdAsync(planId, Arg.Any<CancellationToken>())
            .Returns(plan);
        _appUserRepository.AnyAsync(Arg.Any<Expression<Func<AppUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _identityService.CreateUserAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((Success: true, UserId: newIdentityUserId, Errors: Array.Empty<string>()));

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("سنتر التفوق");
        result.Value.Kind.Should().Be("Center");

        await _appUserRepository.Received(1).AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        await _workspaceRepository.Received(1).AddAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>());
        await _workspaceMemberRepository.Received(1).AddAsync(Arg.Any<WorkspaceMember>(), Arg.Any<CancellationToken>());
        await _workspaceSubscriptionRepository.Received(1).AddAsync(Arg.Any<WorkspaceSubscription>(), Arg.Any<CancellationToken>());
        await _subscriptionPaymentRepository.Received(1).AddAsync(Arg.Any<SubscriptionPayment>(), Arg.Any<CancellationToken>());
        await _auditLogService.Received(1).LogAsync(
            _adminUserId,
            Arg.Any<Guid>(),
            "WORKSPACE_PROVISIONED",
            "Workspace",
            Arg.Any<string>(),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
