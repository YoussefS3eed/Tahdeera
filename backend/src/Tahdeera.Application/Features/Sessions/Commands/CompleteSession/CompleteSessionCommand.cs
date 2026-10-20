using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Sessions.Commands.CompleteSession;

public sealed record CompleteSessionCommand(
    Guid WorkspaceId,
    Guid SessionId,
    Guid ActorUserId) : IRequest<Result<SessionCompletedSummaryDto>>, ITransactionalRequest;
