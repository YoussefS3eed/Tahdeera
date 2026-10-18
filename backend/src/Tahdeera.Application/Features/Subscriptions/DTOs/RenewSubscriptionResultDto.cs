namespace Tahdeera.Application.Features.Subscriptions.DTOs;

public record RenewSubscriptionResultDto(
    Guid SubscriptionId,
    Guid PaymentId,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal AgreedPrice,
    DateTimeOffset RenewedAt);
