using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;

public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result<CreateWorkspaceResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public CreateWorkspaceCommandHandler(
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CreateWorkspaceResultDto>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify subscription plan exists
        var plan = await _unitOfWork.SubscriptionPlans
            .GetByIdAsync(request.Subscription.PlanId, cancellationToken);

        if (plan == null)
        {
            return Error.NotFound("PLAN_NOT_FOUND", $"Subscription plan with ID '{request.Subscription.PlanId}' was not found.");
        }

        // 2. Check if username or email already exists in AppUsers
        var normalizedEmail = request.Owner.Email.Trim().ToUpperInvariant();
        var normalizedUsername = request.Owner.Username.Trim();

        var emailOrUserTaken = await _unitOfWork.AppUsers.AnyAsync(
            u => u.NormalizedEmail == normalizedEmail || u.Username == normalizedUsername,
            cancellationToken);

        if (emailOrUserTaken)
        {
            return Error.Conflict("USER_ALREADY_EXISTS", "A user with this username or email already exists.");
        }

        // 3. Create Identity User via IdentityService
        var identityResult = await _identityService.CreateUserAsync(
            request.Owner.Username.Trim(),
            request.Owner.Email.Trim(),
            request.Owner.Password,
            isPlatformAdmin: false,
            cancellationToken: cancellationToken);

        if (!identityResult.Success)
        {
            return Error.Failure("IDENTITY_CREATION_FAILED", string.Join(", ", identityResult.Errors));
        }

        // 4. Create AppUser
        var user = new AppUser
        {
            IdentitySubject = identityResult.UserId.ToString(),
            Username = request.Owner.Username.Trim(),
            DisplayName = request.Owner.DisplayName.Trim(),
            NormalizedEmail = normalizedEmail,
            Phone = request.Owner.Phone?.Trim(),
            PreferredLanguage = "ar",
            PreferredTheme = "light",
            IsPlatformAdmin = false,
            IsActive = true
        };
        await _unitOfWork.AppUsers.AddAsync(user, cancellationToken);

        // 5. Create Workspace
        var workspace = new Workspace
        {
            Name = request.Name.Trim(),
            Kind = request.Kind.Trim(),
            TimezoneId = string.IsNullOrWhiteSpace(request.TimezoneId) ? "Africa/Cairo" : request.TimezoneId.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            CityAddress = request.CityAddress?.Trim(),
            DefaultLateGraceMinutes = request.DefaultLateGraceMinutes ?? 20,
            WeeklyHolidayDays = new[] { 5 },
            IsActive = true,
            CreatedByUserId = _currentUserService.UserId
        };
        await _unitOfWork.Workspaces.AddAsync(workspace, cancellationToken);

        // 6. Create WorkspaceMember (Owner)
        var member = new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = user.Id,
            Role = "Owner",
            IsActive = true,
            JoinedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _currentUserService.UserId
        };
        await _unitOfWork.WorkspaceMembers.AddAsync(member, cancellationToken);

        // 7. Create WorkspaceSubscription
        var subscription = new WorkspaceSubscription
        {
            WorkspaceId = workspace.Id,
            PlanId = plan.Id,
            StartsAt = request.Subscription.StartsAt,
            EndsAt = request.Subscription.EndsAt,
            Status = "Active",
            AgreedPrice = request.Subscription.AgreedPrice,
            CreatedByUserId = _currentUserService.UserId
        };
        await _unitOfWork.WorkspaceSubscriptions.AddAsync(subscription, cancellationToken);

        // 8. Create SubscriptionPayment
        var payment = new SubscriptionPayment
        {
            SubscriptionId = subscription.Id,
            Amount = request.Subscription.InitialPayment.Amount,
            PaidAt = DateTimeOffset.UtcNow,
            Method = request.Subscription.InitialPayment.Method.Trim(),
            Reference = request.Subscription.InitialPayment.Reference?.Trim(),
            Notes = request.Subscription.InitialPayment.Notes?.Trim(),
            RecordedByUserId = _currentUserService.UserId ?? user.Id,
            Status = "Received"
        };
        await _unitOfWork.SubscriptionPayments.AddAsync(payment, cancellationToken);

        // 9. Record Audit Log
        await _auditLogService.LogAsync(
            _currentUserService.UserId,
            workspace.Id,
            "WORKSPACE_PROVISIONED",
            "Workspace",
            workspace.Id.ToString(),
            new
            {
                WorkspaceName = workspace.Name,
                workspace.Kind,
                OwnerUserId = user.Id,
                PlanCode = plan.Code,
                SubscriptionId = subscription.Id,
                PaymentId = payment.Id
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateWorkspaceResultDto(
            workspace.Id,
            workspace.Name,
            workspace.Kind,
            user.Id,
            subscription.Id,
            payment.Id,
            workspace.CreatedAt));
    }
}
