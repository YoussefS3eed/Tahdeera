namespace Tahdeera.API.Contracts.Requests;

public record RenewSubscriptionRequest(
    decimal Amount,
    string Method,
    string? Reference,
    string? Notes,
    Guid? PlanId,
    int DurationDays,
    DateTimeOffset? OverrideStartsAt,
    DateTimeOffset? OverrideEndsAt);
