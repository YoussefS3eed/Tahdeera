using Mapster;
using MediatR;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.ReferenceData.Queries.GetCurrentAcademicYear;

public class GetCurrentAcademicYearQueryHandler : IRequestHandler<GetCurrentAcademicYearQuery, Result<AcademicYearDto>>
{
    private readonly IAcademicYearRepository _academicYearRepository;

    public GetCurrentAcademicYearQueryHandler(IAcademicYearRepository academicYearRepository)
    {
        _academicYearRepository = academicYearRepository;
    }

    public async Task<Result<AcademicYearDto>> Handle(GetCurrentAcademicYearQuery request, CancellationToken cancellationToken)
    {
        var year = await _academicYearRepository.GetCurrentYearAsync(cancellationToken);

        if (year == null)
        {
            return Error.NotFound("ACTIVE_ACADEMIC_YEAR_NOT_CONFIGURED", "لم يتم تهيئة عام دراسي نشط في النظام حالياً. يرجى مراجعة إدارة المنصة.");
        }

        return Result.Success(year.Adapt<AcademicYearDto>());
    }
}
