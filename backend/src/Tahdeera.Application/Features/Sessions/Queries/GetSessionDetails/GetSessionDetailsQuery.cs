using MediatR;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Sessions.Queries.GetSessionDetails;

public sealed record GetSessionDetailsQuery(
    Guid WorkspaceId,
    Guid SessionId) : IRequest<Result<SessionDetailsDto>>;
