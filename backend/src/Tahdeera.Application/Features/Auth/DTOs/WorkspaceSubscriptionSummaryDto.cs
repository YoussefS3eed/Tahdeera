namespace Tahdeera.Application.Features.Auth.DTOs;

public record WorkspaceSubscriptionSummaryDto(
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsLockedOut);
