using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Sessions.Commands.ScheduleSession;

public sealed record ScheduleSessionCommand(
    Guid WorkspaceId,
    Guid GroupId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? LateGraceMinutes,
    string? RoomName,
    string? Topic,
    Guid ActorUserId) : IRequest<Result<SessionDto>>, ITransactionalRequest;
