using Mapster;
using MediatR;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Queries.GetGroupDetails;

public sealed class GetGroupDetailsQueryHandler : IRequestHandler<GetGroupDetailsQuery, Result<GroupDetailDto>>
{
    private readonly IGroupRepository _groupRepository;

    public GetGroupDetailsQueryHandler(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<Result<GroupDetailDto>> Handle(GetGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetByIdWithDetailsAsync(request.GroupId, cancellationToken);

        if (group is null || group.OwnerWorkspaceId != request.WorkspaceId)
        {
            return DomainErrors.Groups.NotFound;
        }

        var dto = group.Adapt<GroupDetailDto>();
        return Result.Success(dto);
    }
}
