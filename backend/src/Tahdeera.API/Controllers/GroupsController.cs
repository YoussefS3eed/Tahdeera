using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.Groups.Commands.AssignAttendanceDelegate;
using Tahdeera.Application.Features.Groups.Commands.ChangeGroupStatus;
using Tahdeera.Application.Features.Groups.Commands.ChangeGroupStudentStatus;
using Tahdeera.Application.Features.Groups.Commands.CreateGroup;
using Tahdeera.Application.Features.Groups.Commands.EnrollStudentInGroup;
using Tahdeera.Application.Features.Groups.Commands.GroupWorkspaceAccess;
using Tahdeera.Application.Features.Groups.Commands.RevokeAttendanceDelegate;
using Tahdeera.Application.Features.Groups.Commands.UpdateGroup;
using Tahdeera.Application.Features.Groups.Commands.WithdrawStudentFromGroup;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Application.Features.Groups.Queries.GetGroupDetails;
using Tahdeera.Application.Features.Groups.Queries.GetGroupRoster;
using Tahdeera.Application.Features.Groups.Queries.GetGroups;
using Tahdeera.Domain.Enums;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/workspaces/{workspaceId:guid}/groups")]
public class GroupsController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public GroupsController(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [HasPermission(Permissions.GroupsManage)]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateGroup(
        [FromRoute] Guid workspaceId,
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new CreateGroupCommand(
            WorkspaceId: workspaceId,
            AcademicYearId: request.AcademicYearId,
            GradeId: request.GradeId,
            SubjectId: request.SubjectId,
            Name: request.Name,
            RoomName: request.RoomName,
            ScheduleSummary: request.ScheduleSummary,
            LateGraceMinutes: request.LateGraceMinutes,
            DefaultMonthlyFee: request.DefaultMonthlyFee,
            ConsecutiveAbsenceThreshold: request.ConsecutiveAbsenceThreshold,
            AbsenceAction: request.AbsenceAction,
            ActorUserId: actorId,
            TargetGender: request.TargetGender,
            MaxCapacity: request.MaxCapacity
        );

        var result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet]
    [HasPermission(Permissions.GroupsView)]
    [ProducesResponseType(typeof(PagedList<GroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGroups(
        [FromRoute] Guid workspaceId,
        [FromQuery] GroupStatus? status = null,
        [FromQuery] short? gradeId = null,
        [FromQuery] Guid? academicYearId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetGroupsQuery(
            WorkspaceId: workspaceId,
            Status: status,
            GradeId: gradeId,
            AcademicYearId: academicYearId,
            PageNumber: pageNumber,
            PageSize: pageSize
        );

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.GroupsView)]
    [ProducesResponseType(typeof(GroupDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupDetails(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetGroupDetailsQuery(workspaceId, id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.GroupsManage)]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateGroup(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromBody] UpdateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new UpdateGroupCommand(
            WorkspaceId: workspaceId,
            GroupId: id,
            Name: request.Name,
            RoomName: request.RoomName,
            ScheduleSummary: request.ScheduleSummary,
            LateGraceMinutes: request.LateGraceMinutes,
            DefaultMonthlyFee: request.DefaultMonthlyFee,
            ConsecutiveAbsenceThreshold: request.ConsecutiveAbsenceThreshold,
            AbsenceAction: request.AbsenceAction,
            ActorUserId: actorId,
            TargetGender: request.TargetGender,
            MaxCapacity: request.MaxCapacity,
            RecalculateNow: request.RecalculateNow
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/status")]
    [HasPermission(Permissions.GroupsManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeGroupStatus(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromBody] ChangeGroupStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new ChangeGroupStatusCommand(workspaceId, id, request.Status, actorId);
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }

    [HttpGet("{id:guid}/roster")]
    [HasPermission(Permissions.GroupsView)]
    [ProducesResponseType(typeof(PagedList<GroupStudentRosterItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupRoster(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromQuery] GroupStudentStatus? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetGroupRosterQuery(workspaceId, id, status, pageNumber, pageSize);
        var result = await _sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{id:guid}/roster")]
    [HasPermission(Permissions.GroupsManage)]
    [ProducesResponseType(typeof(GroupStudentEnrollmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EnrollStudent(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromBody] EnrollStudentRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new EnrollStudentInGroupCommand(workspaceId, id, request.StudentId, actorId);
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : HandleFailure(result);
    }

    [HttpDelete("{id:guid}/roster/{studentId:guid}")]
    [HasPermission(Permissions.GroupsManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawStudent(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromRoute] Guid studentId,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new WithdrawStudentFromGroupCommand(workspaceId, id, studentId, reason, actorId);
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }

    [HttpPost("{id:guid}/roster/{studentId:guid}/status")]
    [HasPermission(Permissions.GroupsManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeGroupStudentStatus(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromRoute] Guid studentId,
        [FromBody] ChangeGroupStudentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new ChangeGroupStudentStatusCommand(workspaceId, id, studentId, request.NewStatus, request.Reason, actorId);
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }

    [HttpPost("{id:guid}/delegations")]
    [HasPermission(Permissions.GroupsManage)]
    [ProducesResponseType(typeof(GroupDelegationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignAttendanceDelegate(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromBody] AssignAttendanceDelegateRequest request,
        CancellationToken cancellationToken)
    {
        var teacherUserId = _currentUserService.UserId ?? Guid.Empty;
        var command = new AssignAttendanceDelegateCommand(workspaceId, id, request.AssistantUserId, teacherUserId);
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("{id:guid}/delegations/{delegationId:guid}")]
    [HasPermission(Permissions.GroupsManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAttendanceDelegate(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromRoute] Guid delegationId,
        [FromQuery] string reason = "Revoked by teacher",
        CancellationToken cancellationToken = default)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new RevokeAttendanceDelegateCommand(workspaceId, id, delegationId, reason, actorId);
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }

    [HttpPost("{id:guid}/access")]
    [HasPermission(Permissions.GroupsManage)]
    [ProducesResponseType(typeof(GroupWorkspaceAccessDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GrantWorkspaceAccess(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromBody] GrantGroupWorkspaceAccessRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new GrantGroupWorkspaceAccessCommand(
            WorkspaceId: workspaceId,
            GroupId: id,
            GranteeWorkspaceId: request.GranteeWorkspaceId,
            CanViewRoster: request.CanViewRoster,
            CanRegisterAttendance: request.CanRegisterAttendance,
            CanEditAttendance: request.CanEditAttendance,
            ExpiresAt: request.ExpiresAt,
            ActorUserId: actorId,
            Notes: request.Notes
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : HandleFailure(result);
    }

    [HttpDelete("{id:guid}/access/{accessId:guid}")]
    [HasPermission(Permissions.GroupsManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeWorkspaceAccess(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid id,
        [FromRoute] Guid accessId,
        CancellationToken cancellationToken)
    {
        var actorId = _currentUserService.UserId ?? Guid.Empty;
        var command = new RevokeGroupWorkspaceAccessCommand(workspaceId, accessId, actorId);
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }
}
