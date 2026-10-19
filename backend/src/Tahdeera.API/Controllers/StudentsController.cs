using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.Students.Commands.ChangeStudentStatus;
using Tahdeera.Application.Features.Students.Commands.LinkStudentByCard;
using Tahdeera.Application.Features.Students.Commands.LinkStudentByCode;
using Tahdeera.Application.Features.Students.Commands.RegisterStudent;
using Tahdeera.Application.Features.Students.Commands.UpdateStudentContact;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Application.Features.Students.Queries.GetStudent360Profile;
using Tahdeera.Application.Features.Students.Queries.GetStudentDetails;
using Tahdeera.Application.Features.Students.Queries.GetWorkspaceStudents;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/students")]
public class StudentsController : ApiControllerBase
{
    private readonly ISender _sender;

    public StudentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [HasPermission(Permissions.StudentsManage)]
    [ProducesResponseType(typeof(StudentEnrollmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterStudent(
        [FromRoute] Guid workspaceId,
        [FromBody] RegisterStudentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterStudentCommand(
            WorkspaceId: workspaceId,
            FullName: request.FullName,
            GradeId: request.GradeId,
            GuardianName: request.GuardianName,
            GuardianPhone: request.GuardianPhone,
            StudentPhone: request.StudentPhone,
            WhatsappOptIn: request.WhatsappOptIn,
            SeatNumber: request.SeatNumber,
            LocalNotes: request.LocalNotes,
            CardToken: request.CardToken,
            CardUidDisplay: request.CardUidDisplay,
            AllowSiblingPhone: request.AllowSiblingPhone
        );

        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("link-by-card")]
    [HasPermission(Permissions.StudentsManage)]
    [ProducesResponseType(typeof(StudentEnrollmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LinkByCard(
        [FromRoute] Guid workspaceId,
        [FromBody] LinkStudentByCardRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LinkStudentByCardCommand(
            WorkspaceId: workspaceId,
            ScannedToken: request.ScannedToken,
            GradeId: request.GradeId,
            GuardianName: request.GuardianName,
            GuardianPhone: request.GuardianPhone,
            StudentPhone: request.StudentPhone,
            WhatsappOptIn: request.WhatsappOptIn,
            SeatNumber: request.SeatNumber,
            LocalNotes: request.LocalNotes,
            AllowSiblingPhone: request.AllowSiblingPhone
        );

        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("link-by-code")]
    [HasPermission(Permissions.StudentsManage)]
    [EnableRateLimiting("StudentsAntiEnumerationRateLimitPolicy")]
    [ProducesResponseType(typeof(StudentEnrollmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LinkByCode(
        [FromRoute] Guid workspaceId,
        [FromBody] LinkStudentByCodeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LinkStudentByCodeCommand(
            WorkspaceId: workspaceId,
            StudentCode: request.StudentCode,
            VerificationFactor: request.RegisteredGuardianPhone,
            GradeId: request.GradeId,
            GuardianName: request.GuardianName,
            GuardianPhone: request.GuardianPhone ?? request.RegisteredGuardianPhone,
            StudentPhone: request.StudentPhone,
            WhatsappOptIn: request.WhatsappOptIn,
            SeatNumber: request.SeatNumber,
            LocalNotes: request.LocalNotes,
            AllowSiblingPhone: request.AllowSiblingPhone
        );

        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet]
    [HasPermission(Permissions.StudentsView)]
    [ProducesResponseType(typeof(PagedList<StudentRosterItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudents(
        [FromRoute] Guid workspaceId,
        [FromQuery] string? search = null,
        [FromQuery] short? gradeId = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "enrolled_at",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var query = new GetWorkspaceStudentsQuery(
            WorkspaceId: workspaceId,
            SearchTerm: search,
            GradeId: gradeId,
            Status: status,
            PageNumber: pageNumber,
            PageSize: pageSize,
            SortBy: sortBy,
            SortDirection: sortDirection
        );

        var result = await _sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{studentId:guid}")]
    [HasPermission(Permissions.StudentsView)]
    [ProducesResponseType(typeof(StudentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentDetails(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStudentDetailsQuery(workspaceId, studentId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("{studentId:guid}/profile")]
    [HttpGet("{studentId:guid}/360")]
    [HasPermission(Permissions.StudentsView)]
    [ProducesResponseType(typeof(Student360ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentProfile(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStudent360ProfileQuery(workspaceId, studentId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPatch("{studentId:guid}/status")]
    [HasPermission(Permissions.StudentsManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStudentStatus(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromBody] ChangeStudentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeStudentStatusCommand(
            WorkspaceId: workspaceId,
            StudentId: studentId,
            Status: request.Status,
            Reason: request.Reason
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }

    [HttpPut("{studentId:guid}/contact")]
    [HttpPut("{studentId:guid}")]
    [HasPermission(Permissions.StudentsManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStudentContact(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromBody] UpdateStudentContactRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStudentContactCommand(
            WorkspaceId: workspaceId,
            StudentId: studentId,
            FullName: request.FullName,
            GuardianName: request.GuardianName,
            GuardianPhone: request.GuardianPhone,
            StudentPhone: request.StudentPhone,
            WhatsappOptIn: request.WhatsappOptIn,
            SeatNumber: request.SeatNumber,
            LocalNotes: request.LocalNotes,
            AllowSiblingPhone: request.AllowSiblingPhone
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }
}
