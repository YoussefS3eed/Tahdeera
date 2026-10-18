namespace Tahdeera.Application.Features.Workspaces.DTOs;

public record CreateInitialSubscriptionDto(
    Guid PlanId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal AgreedPrice,
    CreateInitialPaymentDto InitialPayment);
