using MediatR;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Commands.DeactivateWorkspaceMember;

public record DeactivateWorkspaceMemberCommand(
    Guid WorkspaceId,
    Guid MemberId) : IRequest<Result<DeactivateWorkspaceMemberResultDto>>;
