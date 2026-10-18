using Microsoft.AspNetCore.Mvc;
using Tahdeera.Domain.Common;

namespace Tahdeera.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleFailure(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot handle failure for a successful result.");
        }

        var statusCode = result.Error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unprocessable => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = result.Error.Code,
            Detail = result.Error.Description,
            Type = $"https://tahdeera.app/errors/{result.Error.Code.ToLowerInvariant().Replace('_', '-')}",
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = result.Error.Code;
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return StatusCode(statusCode, problemDetails);
    }
}
