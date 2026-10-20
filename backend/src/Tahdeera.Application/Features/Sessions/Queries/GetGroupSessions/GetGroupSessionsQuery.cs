using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Sessions.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Sessions.Queries.GetGroupSessions;

public sealed record GetGroupSessionsQuery(
    Guid WorkspaceId,
    Guid GroupId,
    SessionStatus? Status = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<Result<PagedList<SessionDto>>>;
