using Mapster;
using MediatR;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.ReferenceData.Queries.GetAcademicYears;

public class GetAcademicYearsQueryHandler : IRequestHandler<GetAcademicYearsQuery, Result<IReadOnlyList<AcademicYearDto>>>
{
    private readonly IAcademicYearRepository _academicYearRepository;

    public GetAcademicYearsQueryHandler(IAcademicYearRepository academicYearRepository)
    {
        _academicYearRepository = academicYearRepository;
    }

    public async Task<Result<IReadOnlyList<AcademicYearDto>>> Handle(GetAcademicYearsQuery request, CancellationToken cancellationToken)
    {
        var allYears = await _academicYearRepository.GetAllOrderedAsync(cancellationToken);
        return Result.Success<IReadOnlyList<AcademicYearDto>>(allYears.Adapt<List<AcademicYearDto>>());
    }
}
