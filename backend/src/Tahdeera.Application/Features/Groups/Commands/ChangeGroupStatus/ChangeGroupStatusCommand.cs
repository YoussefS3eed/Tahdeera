using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Groups.Commands.ChangeGroupStatus;

public sealed record ChangeGroupStatusCommand(
    Guid WorkspaceId,
    Guid GroupId,
    GroupStatus NewStatus,
    Guid ActorUserId) : IRequest<Result>, ITransactionalRequest;
