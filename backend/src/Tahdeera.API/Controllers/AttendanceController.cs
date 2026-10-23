using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tahdeera.API.Contracts.Requests;
using Tahdeera.API.Security;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Security;
using Tahdeera.Application.Features.Attendance.Commands.CorrectAttendanceRecord;
using Tahdeera.Application.Features.Attendance.Commands.ManualCheckIn;
using Tahdeera.Application.Features.Attendance.Commands.RegisterGuestMakeupAttendance;
using Tahdeera.Application.Features.Attendance.Commands.ScanCardAttendance;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Application.Features.Attendance.Queries.GetSessionAttendanceSheet;
using Tahdeera.Application.Features.Attendance.Queries.GetStudentAttendanceHistory;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Authorize]
public class AttendanceController : ApiControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public AttendanceController(
        ISender sender,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _sender = sender;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/sessions/{sessionId:guid}/attendance/scan")]
    [HasPermission(Permissions.AttendanceRegister)]
    [ProducesResponseType(typeof(AttendanceScanResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScanCardAttendance(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid sessionId,
        [FromBody] ScanCardAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ScanCardAttendanceCommand(
            SessionId: sessionId,
            RawToken: request.RawToken,
            WorkspaceId: workspaceId
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/sessions/{sessionId:guid}/attendance/manual")]
    [HasPermission(Permissions.AttendanceRegister)]
    [ProducesResponseType(typeof(AttendanceScanResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ManualCheckIn(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid sessionId,
        [FromBody] ManualCheckInRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ManualCheckInCommand(
            SessionId: sessionId,
            StudentId: request.StudentId,
            RetroactiveReason: request.RetroactiveReason,
            WorkspaceId: workspaceId
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/sessions/{sessionId:guid}/attendance/guest-makeup")]
    [HasPermission(Permissions.AttendanceRegister)]
    [ProducesResponseType(typeof(GuestMakeupResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterGuestMakeup(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid sessionId,
        [FromBody] RegisterGuestMakeupAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterGuestMakeupAttendanceCommand(
            SessionId: sessionId,
            StudentId: request.StudentId,
            HomeGroupId: request.HomeGroupId,
            HomeSessionId: request.HomeSessionId,
            Reason: request.Reason,
            WorkspaceId: workspaceId
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPatch("api/workspaces/{workspaceId:guid}/sessions/{sessionId:guid}/attendance/{studentId:guid}")]
    [HasPermission(Permissions.AttendanceCorrect)]
    [ProducesResponseType(typeof(AttendanceCorrectedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CorrectAttendanceRecord(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid sessionId,
        [FromRoute] Guid studentId,
        [FromBody] CorrectAttendanceRecordRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Verify session belongs to workspace
        var session = await _unitOfWork.Sessions.GetByIdWithDetailsAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return HandleFailure(Result.Failure(DomainErrors.Sessions.NotFound));
        }

        if (session.Group.OwnerWorkspaceId != workspaceId)
        {
            return HandleFailure(Result.Failure(DomainErrors.Sessions.UnauthorizedWorkspaceAccess));
        }

        // 2. Resolve attendance record by (sessionId, studentId) or by attendanceRecordId
        var record = await _unitOfWork.AttendanceRecords.FirstOrDefaultAsync(
            r => r.SessionId == sessionId && r.StudentId == studentId && r.VoidedAt == null, cancellationToken);

        if (record is null)
        {
            record = await _unitOfWork.AttendanceRecords.FirstOrDefaultAsync(
                r => r.Id == studentId && r.SessionId == sessionId && r.VoidedAt == null, cancellationToken);
        }

        if (record is null)
        {
            return HandleFailure(Result.Failure(DomainErrors.Attendance.NotFound));
        }

        // 3. Check if actor is Workspace Owner for the session's workspace
        bool isOwner = User.IsInRole("PlatformAdmin") || User.IsInRole("SuperAdmin");
        if (!isOwner && _currentUserService.UserId.HasValue)
        {
            var member = await _unitOfWork.WorkspaceMembers.FirstOrDefaultAsync(
                m => m.WorkspaceId == workspaceId && m.UserId == _currentUserService.UserId.Value && m.IsActive, cancellationToken);
            isOwner = member?.Role == "Owner";
        }

        var command = new CorrectAttendanceRecordCommand(
            AttendanceRecordId: record.Id,
            NewStatus: request.NewStatus,
            Reason: request.Reason,
            IsOwner: isOwner,
            WorkspaceId: workspaceId
        );

        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/sessions/{sessionId:guid}/attendance")]
    [HasPermission(Permissions.SessionsView)]
    [ProducesResponseType(typeof(SessionAttendanceSheetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionAttendanceSheet(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var query = new GetSessionAttendanceSheetQuery(workspaceId, sessionId);
        var result = await _sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/students/{studentId:guid}/attendance")]
    [HasPermission(Permissions.StudentsView)]
    [ProducesResponseType(typeof(StudentAttendanceHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentAttendanceHistory(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid studentId,
        [FromQuery] Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetStudentAttendanceHistoryQuery(workspaceId, studentId, groupId);
        var result = await _sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
