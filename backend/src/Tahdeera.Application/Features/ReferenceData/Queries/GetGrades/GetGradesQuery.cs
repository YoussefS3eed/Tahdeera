using MediatR;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.ReferenceData.Queries.GetGrades;

public record GetGradesQuery() : IRequest<Result<IReadOnlyList<GradeDto>>>;
