using MediatR;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Commands.ReactivateWorkspace;

public record ReactivateWorkspaceCommand(Guid WorkspaceId) : IRequest<Result>;
