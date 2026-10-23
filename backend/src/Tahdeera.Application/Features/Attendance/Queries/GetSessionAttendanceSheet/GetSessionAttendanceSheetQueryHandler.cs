using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Attendance.Queries.GetSessionAttendanceSheet;

public sealed class GetSessionAttendanceSheetQueryHandler : IRequestHandler<GetSessionAttendanceSheetQuery, Result<SessionAttendanceSheetDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetSessionAttendanceSheetQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<SessionAttendanceSheetDto>> Handle(
        GetSessionAttendanceSheetQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Sessions
            .GetByIdWithDetailsAsync(request.SessionId, cancellationToken);

        if (session is null)
        {
            return DomainErrors.Sessions.NotFound;
        }

        var workspaceId = session.Group.OwnerWorkspaceId;
        if (request.WorkspaceId != Guid.Empty && workspaceId != request.WorkspaceId)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        if (_currentUserService.ActiveWorkspaceId.HasValue &&
            _currentUserService.ActiveWorkspaceId.Value != workspaceId)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        // Active enrolled students count
        var totalEnrolled = await _unitOfWork.GroupStudents
            .CountAsync(gs => gs.GroupId == session.GroupId && gs.Status == GroupStudentStatus.Active, cancellationToken);

        // Attendance records for this session
        var records = await _unitOfWork.AttendanceRecords
            .FindAsync(r => r.SessionId == session.Id && r.VoidedAt == null, cancellationToken);

        var presentCount = records.Count(r => r.Status == AttendanceStatus.Present);
        var lateCount = records.Count(r => r.Status == AttendanceStatus.Late);
        var absentCount = records.Count(r => r.Status == AttendanceStatus.Absent);
        var excusedCount = records.Count(r => r.Status == AttendanceStatus.Excused);
        var guestCount = records.Count(r => r.IsGuestMakeup);

        // Fetch students to populate details
        var studentIds = records.Select(r => r.StudentId).Distinct().ToList();
        var students = await _unitOfWork.Students
            .FindAsync(s => studentIds.Contains(s.Id), cancellationToken);
        var studentDict = students.ToDictionary(s => s.Id);

        // Fetch home groups for guest makeups
        var homeGroupIds = records.Where(r => r.HomeGroupId.HasValue).Select(r => r.HomeGroupId!.Value).Distinct().ToList();
        var homeGroups = await _unitOfWork.Groups
            .FindAsync(g => homeGroupIds.Contains(g.Id), cancellationToken);
        var homeGroupDict = homeGroups.ToDictionary(g => g.Id, g => g.Name);

        var items = records.Select(r =>
        {
            studentDict.TryGetValue(r.StudentId, out var stu);
            string? homeGroupName = null;
            if (r.HomeGroupId.HasValue)
            {
                homeGroupDict.TryGetValue(r.HomeGroupId.Value, out homeGroupName);
            }

            return new AttendanceSheetItemDto(
                r.Id,
                r.StudentId,
                stu?.FullName ?? string.Empty,
                stu?.StudentCode ?? string.Empty,
                r.Status.ToString(),
                r.CheckInAt,
                r.AutoEvaluationLocked,
                r.IsGuestMakeup,
                r.HomeGroupId,
                homeGroupName,
                r.RetroactiveReason);
        }).ToList();

        return Result.Success(new SessionAttendanceSheetDto(
            session.Id,
            session.GroupId,
            session.Group.Name,
            session.StartsAt,
            session.EndsAt,
            totalEnrolled,
            presentCount,
            lateCount,
            absentCount,
            excusedCount,
            guestCount,
            items));
    }
}
