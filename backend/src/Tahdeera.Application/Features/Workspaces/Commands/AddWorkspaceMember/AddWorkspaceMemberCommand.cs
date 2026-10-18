using MediatR;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Commands.AddWorkspaceMember;

public record AddWorkspaceMemberCommand(
    Guid WorkspaceId,
    string Username,
    string Email,
    string Password,
    string DisplayName,
    string? Phone,
    string Role) : IRequest<Result<AddWorkspaceMemberResultDto>>;
