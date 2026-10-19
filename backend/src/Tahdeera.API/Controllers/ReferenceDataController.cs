using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.Application.Features.ReferenceData.DTOs;
using Tahdeera.Application.Features.ReferenceData.Queries.GetAcademicYears;
using Tahdeera.Application.Features.ReferenceData.Queries.GetCurrentAcademicYear;
using Tahdeera.Application.Features.ReferenceData.Queries.GetGrades;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/reference")]
public class ReferenceDataController : ApiControllerBase
{
    private readonly ISender _sender;

    public ReferenceDataController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("grades")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GradeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGrades(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetGradesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("academic-years")]
    [Authorize]
    [ProducesResponseType(typeof(List<AcademicYearDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAcademicYears(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAcademicYearsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("academic-years/current")]
    [Authorize]
    [ProducesResponseType(typeof(AcademicYearDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentAcademicYear(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCurrentAcademicYearQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
