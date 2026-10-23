using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Attendance.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Attendance.Queries.GetStudentAttendanceHistory;

public sealed class GetStudentAttendanceHistoryQueryHandler : IRequestHandler<GetStudentAttendanceHistoryQuery, Result<StudentAttendanceHistoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetStudentAttendanceHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<StudentAttendanceHistoryDto>> Handle(
        GetStudentAttendanceHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId, cancellationToken);
        if (student is null)
        {
            return DomainErrors.Groups.StudentNotEnrolled;
        }

        var workspaceId = request.WorkspaceId != Guid.Empty
            ? request.WorkspaceId
            : _currentUserService.ActiveWorkspaceId ?? Guid.Empty;

        if (workspaceId != Guid.Empty)
        {
            if (_currentUserService.ActiveWorkspaceId.HasValue &&
                _currentUserService.ActiveWorkspaceId.Value != workspaceId)
            {
                return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
            }

            var workspaceStudent = await _unitOfWork.WorkspaceStudents
                .FirstOrDefaultAsync(ws => ws.WorkspaceId == workspaceId && ws.StudentId == request.StudentId, cancellationToken);

            if (workspaceStudent is null)
            {
                return DomainErrors.Groups.StudentNotEnrolled;
            }
        }

        // Fetch groups
        List<Guid>? workspaceGroupIds = null;
        Dictionary<Guid, string> groupDict;

        if (workspaceId != Guid.Empty)
        {
            var workspaceGroups = await _unitOfWork.Groups
                .FindAsync(g => g.OwnerWorkspaceId == workspaceId, cancellationToken);
            workspaceGroupIds = workspaceGroups.Select(g => g.Id).ToList();

            if (request.GroupId.HasValue)
            {
                if (!workspaceGroupIds.Contains(request.GroupId.Value))
                {
                    return DomainErrors.Groups.NotFound;
                }
                workspaceGroupIds = new List<Guid> { request.GroupId.Value };
            }

            groupDict = workspaceGroups.ToDictionary(g => g.Id, g => g.Name);
        }
        else if (request.GroupId.HasValue)
        {
            workspaceGroupIds = new List<Guid> { request.GroupId.Value };
            var groups = await _unitOfWork.Groups
                .FindAsync(g => g.Id == request.GroupId.Value, cancellationToken);
            groupDict = groups.ToDictionary(g => g.Id, g => g.Name);
        }
        else
        {
            groupDict = new Dictionary<Guid, string>();
        }

        // Fetch records for this student
        var records = await _unitOfWork.AttendanceRecords
            .FindAsync(r => r.StudentId == request.StudentId && r.VoidedAt == null, cancellationToken);

        // Fetch sessions
        var sessionIds = records.Select(r => r.SessionId).Distinct().ToList();
        var sessions = await _unitOfWork.Sessions
            .FindAsync(s => sessionIds.Contains(s.Id), cancellationToken);

        if (workspaceGroupIds != null)
        {
            sessions = sessions.Where(s => workspaceGroupIds.Contains(s.GroupId)).ToList();
            var filteredSessionIds = sessions.Select(s => s.Id).ToHashSet();
            records = records.Where(r => filteredSessionIds.Contains(r.SessionId)).ToList();
        }

        var sessionDict = sessions.ToDictionary(s => s.Id);

        if (workspaceId == Guid.Empty && !request.GroupId.HasValue)
        {
            var groupIds = sessions.Select(s => s.GroupId).Distinct().ToList();
            var groups = await _unitOfWork.Groups
                .FindAsync(g => groupIds.Contains(g.Id), cancellationToken);
            groupDict = groups.ToDictionary(g => g.Id, g => g.Name);
        }

        var attendedCount = records.Count(r => r.Status == AttendanceStatus.Present);
        var lateCount = records.Count(r => r.Status == AttendanceStatus.Late);
        var absentCount = records.Count(r => r.Status == AttendanceStatus.Absent);
        var excusedCount = records.Count(r => r.Status == AttendanceStatus.Excused);
        var totalSessions = records.Count;

        var attendancePercentage = totalSessions > 0
            ? Math.Round(((double)(attendedCount + lateCount) / totalSessions) * 100.0, 1)
            : 0.0;

        var timeline = records
            .OrderByDescending(r => sessionDict.TryGetValue(r.SessionId, out var s) ? s.StartsAt : r.RecordedAt)
            .Select(r =>
            {
                sessionDict.TryGetValue(r.SessionId, out var s);
                var groupName = s != null && groupDict.TryGetValue(s.GroupId, out var gName) ? gName : string.Empty;

                return new StudentAttendanceHistoryItemDto(
                    r.Id,
                    r.SessionId,
                    s?.GroupId ?? Guid.Empty,
                    groupName,
                    s?.StartsAt ?? r.RecordedAt,
                    r.Status.ToString(),
                    r.CheckInAt,
                    r.IsGuestMakeup);
            }).ToList();

        return Result.Success(new StudentAttendanceHistoryDto(
            student.Id,
            student.FullName,
            student.StudentCode,
            totalSessions,
            attendedCount,
            lateCount,
            absentCount,
            excusedCount,
            attendancePercentage,
            timeline));
    }
}
