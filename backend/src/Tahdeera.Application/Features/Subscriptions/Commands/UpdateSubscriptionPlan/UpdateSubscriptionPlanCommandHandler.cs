using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;

public class UpdateSubscriptionPlanCommandHandler : IRequestHandler<UpdateSubscriptionPlanCommand, Result<SubscriptionPlanDto>>
{
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateSubscriptionPlanCommandHandler(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
        : this(
            unitOfWork.SubscriptionPlans,
            unitOfWork,
            auditLogService,
            currentUserService)
    {
    }

    public UpdateSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SubscriptionPlanDto>> Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _subscriptionPlanRepository.GetByIdAsync(request.Id, cancellationToken);

        if (plan == null)
        {
            return Error.NotFound("PLAN_NOT_FOUND", $"Subscription plan with ID '{request.Id}' was not found.");
        }

        // Apply in-place update using Mapster
        request.Adapt(plan);
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        _subscriptionPlanRepository.Update(plan);

        await _auditLogService.LogAsync(
            _currentUserService.UserId,
            null,
            "PLAN_UPDATED",
            "SubscriptionPlan",
            plan.Id.ToString(),
            new { plan.Code, plan.DefaultPrice, plan.DurationDays, plan.IsActive },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plan.Adapt<SubscriptionPlanDto>());
    }
}
