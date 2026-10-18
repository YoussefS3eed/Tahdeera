using MediatR;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Queries.GetWorkspaceMembers;

public record GetWorkspaceMembersQuery(Guid WorkspaceId) : IRequest<Result<List<WorkspaceMemberDto>>>;
