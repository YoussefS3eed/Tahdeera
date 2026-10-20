using Mapster;
using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Queries.GetGroups;

public sealed class GetGroupsQueryHandler : IRequestHandler<GetGroupsQuery, PagedList<GroupDto>>
{
    private readonly IGroupRepository _groupRepository;

    public GetGroupsQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<PagedList<GroupDto>> Handle(GetGroupsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _groupRepository.GetPagedGroupsAsync(
            request.WorkspaceId,
            request.Status,
            request.GradeId,
            request.AcademicYearId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Adapt<List<GroupDto>>();

        return new PagedList<GroupDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
