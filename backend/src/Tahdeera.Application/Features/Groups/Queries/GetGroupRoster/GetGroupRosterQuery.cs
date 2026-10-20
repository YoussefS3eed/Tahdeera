using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Groups.Queries.GetGroupRoster;

public sealed record GetGroupRosterQuery(
    Guid WorkspaceId,
    Guid GroupId,
    GroupStudentStatus? StatusFilter = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<Result<PagedList<GroupStudentRosterItemDto>>>;
