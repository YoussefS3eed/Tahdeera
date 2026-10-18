namespace Tahdeera.Application.Features.Auth.DTOs;

public record UserWorkspaceMembershipDto(
    Guid WorkspaceId,
    string WorkspaceName,
    string WorkspaceKind,
    string Role,
    WorkspaceSubscriptionSummaryDto? Subscription);
