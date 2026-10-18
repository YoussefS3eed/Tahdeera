using MediatR;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Queries.GetWorkspaceSubscriptionStatus;

public record GetWorkspaceSubscriptionStatusQuery(Guid WorkspaceId) : IRequest<Result<WorkspaceSubscriptionStatusDto>>;
