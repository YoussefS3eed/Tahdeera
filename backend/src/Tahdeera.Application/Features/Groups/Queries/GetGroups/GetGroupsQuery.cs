using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Features.Groups.Queries.GetGroups;

public sealed record GetGroupsQuery(
    Guid WorkspaceId,
    GroupStatus? Status = null,
    short? GradeId = null,
    Guid? AcademicYearId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedList<GroupDto>>;
