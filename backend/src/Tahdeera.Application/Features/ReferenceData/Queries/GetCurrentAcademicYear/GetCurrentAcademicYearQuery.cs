using MediatR;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.ReferenceData.Queries.GetCurrentAcademicYear;

public record GetCurrentAcademicYearQuery() : IRequest<Result<AcademicYearDto>>;
