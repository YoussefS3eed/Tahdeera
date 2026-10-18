using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;

public class CreateSubscriptionPlanCommandHandler : IRequestHandler<CreateSubscriptionPlanCommand, Result<SubscriptionPlanDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;

    public CreateSubscriptionPlanCommandHandler(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SubscriptionPlanDto>> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var codeNormalized = request.Code.Trim().ToUpperInvariant();
        var exists = await _unitOfWork.SubscriptionPlans.ExistsByCodeAsync(codeNormalized, cancellationToken);

        if (exists)
        {
            return Error.Conflict("PLAN_CODE_EXISTS", $"Subscription plan with code '{codeNormalized}' already exists.");
        }

        var plan = request.Adapt<SubscriptionPlan>();

        await _unitOfWork.SubscriptionPlans.AddAsync(plan, cancellationToken);

        await _auditLogService.LogAsync(
            _currentUserService.UserId,
            null,
            "PLAN_CREATED",
            "SubscriptionPlan",
            plan.Id.ToString(),
            new { plan.Code, plan.DefaultPrice, plan.DurationDays },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(plan.Adapt<SubscriptionPlanDto>());
    }
}
