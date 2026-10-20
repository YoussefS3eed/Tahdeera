using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Sessions.Commands.UpdateSessionSchedule;

public sealed record UpdateSessionScheduleCommand(
    Guid WorkspaceId,
    Guid SessionId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int LateGraceMinutes,
    Guid ActorUserId) : IRequest<Result<SessionScheduleUpdatedDto>>, ITransactionalRequest;
