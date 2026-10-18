using MediatR;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Commands.SuspendWorkspace;

public record SuspendWorkspaceCommand(Guid WorkspaceId, string Reason) : IRequest<Result>;
