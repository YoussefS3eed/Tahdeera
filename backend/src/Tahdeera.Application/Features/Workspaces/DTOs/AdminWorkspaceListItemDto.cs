namespace Tahdeera.Application.Features.Workspaces.DTOs;

public record AdminWorkspaceListItemDto(
    Guid Id,
    string Name,
    string Kind,
    string? OwnerName,
    string? OwnerEmail,
    string SubscriptionStatus,
    DateTimeOffset? SubscriptionEndsAt,
    bool IsSuspended,
    DateTimeOffset CreatedAt);
