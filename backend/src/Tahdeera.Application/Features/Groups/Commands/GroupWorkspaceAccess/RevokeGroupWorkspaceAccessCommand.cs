using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Groups.Commands.GroupWorkspaceAccess;

public sealed record RevokeGroupWorkspaceAccessCommand(
    Guid WorkspaceId,
    Guid AccessId,
    Guid ActorUserId) : IRequest<Result>, ITransactionalRequest;
