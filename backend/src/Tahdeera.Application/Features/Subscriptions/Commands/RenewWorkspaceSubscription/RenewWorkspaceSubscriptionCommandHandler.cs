using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Subscriptions.Commands.RenewWorkspaceSubscription;

public class RenewWorkspaceSubscriptionCommandHandler : IRequestHandler<RenewWorkspaceSubscriptionCommand, Result<RenewSubscriptionResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    public RenewWorkspaceSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    public async Task<Result<RenewSubscriptionResultDto>> Handle(
        RenewWorkspaceSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Error.Unauthorized("AUTH_UNAUTHORIZED", "User is not authenticated.");
        }

        if (!_currentUserService.IsPlatformAdmin)
        {
            return Error.Forbidden("ROLE_FORBIDDEN", "لا تملك الصلاحية المطلوبة لتنفيذ هذا الإجراء.");
        }

        var subscriptions = await _unitOfWork.WorkspaceSubscriptions
            .FindAsync(s => s.WorkspaceId == request.WorkspaceId, cancellationToken);

        var subscription = subscriptions.OrderByDescending(s => s.EndsAt).FirstOrDefault();

        if (subscription == null)
        {
            return Error.NotFound("WORKSPACE_NOT_FOUND", "مساحة العمل المطلوبة غير موجودة أو لا تمتلك اشتراكاً مسجلاً.");
        }

        var now = _dateTimeProvider.UtcNow;

        try
        {
            subscription.Renew(
                request.DurationDays,
                request.Amount,
                now,
                request.OverrideStartsAt,
                request.OverrideEndsAt);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.ErrorCode, ex.Message);
        }

        if (request.PlanId.HasValue)
        {
            subscription.PlanId = request.PlanId.Value;
        }

        var payment = new SubscriptionPayment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            Amount = request.Amount,
            PaidAt = now,
            Method = request.Method,
            Reference = request.Reference,
            RecordedByUserId = currentUserId.Value,
            Status = "Received",
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _unitOfWork.SubscriptionPayments.AddAsync(payment, cancellationToken);
        _unitOfWork.WorkspaceSubscriptions.Update(subscription);

        await _auditLogService.LogAsync(
            actorUserId: currentUserId.Value,
            workspaceId: request.WorkspaceId,
            action: "SUBSCRIPTION_RENEWED",
            entityType: "WorkspaceSubscription",
            entityId: subscription.Id.ToString(),
            metadata: new
            {
                Amount = request.Amount,
                Method = request.Method,
                NewEndsAt = subscription.EndsAt,
                PaymentId = payment.Id
            },
            cancellationToken: cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var resultDto = subscription.Adapt<RenewSubscriptionResultDto>() with
        {
            PaymentId = payment.Id,
            RenewedAt = now
        };

        return Result<RenewSubscriptionResultDto>.Success(resultDto);
    }
}
