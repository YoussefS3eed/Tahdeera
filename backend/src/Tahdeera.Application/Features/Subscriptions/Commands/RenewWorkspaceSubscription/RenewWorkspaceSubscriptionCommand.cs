using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Subscriptions.Commands.RenewWorkspaceSubscription;

public record RenewWorkspaceSubscriptionCommand(
    Guid WorkspaceId,
    decimal Amount,
    string Method,
    string? Reference,
    string? Notes,
    Guid? PlanId,
    int DurationDays,
    DateTimeOffset? OverrideStartsAt,
    DateTimeOffset? OverrideEndsAt) : IRequest<Result<RenewSubscriptionResultDto>>, ITransactionalRequest;
