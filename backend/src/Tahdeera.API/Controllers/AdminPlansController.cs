using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.Application.Features.Subscriptions.Commands.CreateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.Commands.UpdateSubscriptionPlan;
using Tahdeera.Application.Features.Subscriptions.DTOs;
using Tahdeera.Application.Features.Subscriptions.Queries.GetSubscriptionPlans;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[Route("api/admin/plans")]
[Authorize(Policy = "PlatformAdmin")]
public class AdminPlansController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AdminPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans([FromQuery] bool activeOnly = false)
    {
        var result = await _mediator.Send(new GetSubscriptionPlansQuery(activeOnly));
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePlan([FromBody] CreateSubscriptionPlanCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPlans), new { id = result.Value.Id }, result.Value)
            : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanRequest request)
    {
        var command = new UpdateSubscriptionPlanCommand(
            id,
            request.NameAr,
            request.NameEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.DefaultPrice,
            request.Currency,
            request.DurationDays,
            request.MaxStudents,
            request.MaxStaff,
            request.MaxBranches,
            request.Features,
            request.IsActive);

        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
