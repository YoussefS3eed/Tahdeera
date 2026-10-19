using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using Tahdeera.Application.Resources;
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

        var localizer = HttpContext.RequestServices.GetService<IStringLocalizer<DomainMessages>>();

        string title = result.Error.Code;
        string detail = result.Error.Description;

        if (localizer != null)
        {
            var localizedTitle = localizer[$"{result.Error.Code}_Title"];
            if (!localizedTitle.ResourceNotFound && !string.IsNullOrWhiteSpace(localizedTitle.Value))
            {
                title = localizedTitle.Value;
            }

            var localizedDetail = localizer[$"{result.Error.Code}_Detail"];
            if (!localizedDetail.ResourceNotFound && !string.IsNullOrWhiteSpace(localizedDetail.Value))
            {
                detail = localizedDetail.Value;
            }
            else
            {
                var localizedGeneral = localizer[result.Error.Code];
                if (!localizedGeneral.ResourceNotFound && !string.IsNullOrWhiteSpace(localizedGeneral.Value))
                {
                    detail = localizedGeneral.Value;
                }
            }
        }

        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://tahdeera.app/errors/{result.Error.Code.ToLowerInvariant().Replace('_', '-')}",
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = result.Error.Code;
        problemDetails.Extensions["traceId"] = traceId;

        return StatusCode(statusCode, problemDetails);
    }
}
