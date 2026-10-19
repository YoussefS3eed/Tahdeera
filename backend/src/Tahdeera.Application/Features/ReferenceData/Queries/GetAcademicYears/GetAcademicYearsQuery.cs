using MediatR;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.ReferenceData.Queries.GetAcademicYears;

public record GetAcademicYearsQuery() : IRequest<Result<IReadOnlyList<AcademicYearDto>>>;
