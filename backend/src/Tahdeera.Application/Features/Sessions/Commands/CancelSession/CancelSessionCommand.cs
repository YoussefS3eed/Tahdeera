using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Sessions.Commands.CancelSession;

public sealed record CancelSessionCommand(
    Guid WorkspaceId,
    Guid SessionId,
    string CancellationReason,
    Guid ActorUserId) : IRequest<Result<SessionCancelledDto>>, ITransactionalRequest;
