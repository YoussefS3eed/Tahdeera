namespace Tahdeera.Application.Features.Workspaces.DTOs;

public record CreateWorkspaceResultDto(
    Guid WorkspaceId,
    string Name,
    string Kind,
    Guid OwnerUserId,
    Guid SubscriptionId,
    Guid PaymentId,
    DateTimeOffset CreatedAt);
