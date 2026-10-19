using Mapster;
using MediatR;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.ReferenceData.Queries.GetGrades;

public class GetGradesQueryHandler : IRequestHandler<GetGradesQuery, Result<IReadOnlyList<GradeDto>>>
{
    private readonly IGradeRepository _gradeRepository;

    public GetGradesQueryHandler(IGradeRepository gradeRepository)
    {
        _gradeRepository = gradeRepository;
    }

    public async Task<Result<IReadOnlyList<GradeDto>>> Handle(GetGradesQuery request, CancellationToken cancellationToken)
    {
        var allGrades = await _gradeRepository.GetActiveGradesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<GradeDto>>(allGrades.Adapt<List<GradeDto>>());
    }
}
