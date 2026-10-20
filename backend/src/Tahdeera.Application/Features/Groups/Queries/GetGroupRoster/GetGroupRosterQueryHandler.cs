using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Queries.GetGroupRoster;

public sealed class GetGroupRosterQueryHandler : IRequestHandler<GetGroupRosterQuery, Result<PagedList<GroupStudentRosterItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGroupRosterQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedList<GroupStudentRosterItemDto>>> Handle(GetGroupRosterQuery request, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId, cancellationToken);

        if (group is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        var hasAccess = group.OwnerWorkspaceId == request.WorkspaceId
            || await _unitOfWork.GroupWorkspaceAccesses
                .AnyAsync(a => a.GroupId == request.GroupId
                            && a.WorkspaceId == request.WorkspaceId
                            && a.RevokedAt == null
                            && a.CanViewRoster, cancellationToken);

        if (!hasAccess)
        {
            return DomainErrors.Sessions.UnauthorizedWorkspaceAccess;
        }

        var allEnrollments = await _unitOfWork.GroupStudents
            .FindAsync(gs => gs.GroupId == request.GroupId, cancellationToken);

        if (request.StatusFilter.HasValue)
        {
            allEnrollments = allEnrollments.Where(gs => gs.Status == request.StatusFilter.Value).ToList();
        }

        var totalCount = allEnrollments.Count;

        var pagedEnrollments = allEnrollments
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var studentIds = pagedEnrollments.Select(x => x.StudentId).ToList();

        var wsStudents = await _unitOfWork.WorkspaceStudents
            .FindAsync(ws => ws.WorkspaceId == group.OwnerWorkspaceId && studentIds.Contains(ws.StudentId), cancellationToken);
        var contactDict = wsStudents.ToDictionary(ws => ws.StudentId, ws => ws.GuardianPhone ?? string.Empty);

        var groupSessions = await _unitOfWork.Sessions.FindAsync(s => s.GroupId == request.GroupId, cancellationToken);
        var groupSessionIds = groupSessions.Select(s => s.Id).ToHashSet();

        var attendanceRecords = await _unitOfWork.AttendanceRecords
            .FindAsync(ar => studentIds.Contains(ar.StudentId)
                          && groupSessionIds.Contains(ar.SessionId)
                          && ar.VoidedAt == null, cancellationToken);

        var students = await _unitOfWork.Students.FindAsync(s => studentIds.Contains(s.Id), cancellationToken);
        var studentDict = students.ToDictionary(s => s.Id);

        var items = pagedEnrollments.Select(item =>
        {
            studentDict.TryGetValue(item.StudentId, out var student);
            var attendedCount = attendanceRecords.Count(ar =>
                ar.StudentId == item.StudentId && (ar.Status == AttendanceStatus.Present || ar.Status == AttendanceStatus.Late));

            var absentCount = attendanceRecords.Count(ar =>
                ar.StudentId == item.StudentId && ar.Status == AttendanceStatus.Absent);

            contactDict.TryGetValue(item.StudentId, out var guardianPhone);

            return new GroupStudentRosterItemDto(
                GroupStudentId: item.Id,
                StudentId: item.StudentId,
                StudentName: student?.FullName ?? string.Empty,
                StudentCode: student?.StudentCode ?? string.Empty,
                GuardianPhone: guardianPhone ?? string.Empty,
                Status: item.Status.ToString(),
                JoinedAt: item.JoinedAt,
                LeftAt: item.LeftAt,
                TotalAttendedSessions: attendedCount,
                TotalAbsentSessions: absentCount);
        }).ToList();

        var result = PagedList<GroupStudentRosterItemDto>.Create(items, totalCount, request.PageNumber, request.PageSize);
        return Result.Success(result);
    }
}
