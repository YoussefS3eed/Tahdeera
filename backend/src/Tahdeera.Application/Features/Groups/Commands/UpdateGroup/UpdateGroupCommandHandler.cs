using Mapster;
using MediatR;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Commands.UpdateGroup;

public sealed class UpdateGroupCommandHandler : IRequestHandler<UpdateGroupCommand, Result<GroupDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGroupCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GroupDto>> Handle(UpdateGroupCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch group via repository
        var group = await _unitOfWork.Groups.GetByIdForWorkspaceAsync(request.WorkspaceId, request.GroupId, cancellationToken);

        if (group is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        // 2. Validate name uniqueness if modified
        if (!string.Equals(group.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var duplicateExists = await _unitOfWork.Groups.ExistsByNameAsync(
                request.WorkspaceId,
                group.AcademicYearId,
                request.Name,
                group.Id,
                cancellationToken);

            if (duplicateExists)
            {
                return DomainErrors.Groups.NameDuplicate;
            }
        }

        // 3. Mutate group properties
        group.UpdateDetails(request.Name, request.RoomName, request.ScheduleSummary, group.SubjectId, request.ActorUserId);
        group.UpdateSchedule(request.LateGraceMinutes, request.ActorUserId);
        group.UpdateAbsencePolicy(request.ConsecutiveAbsenceThreshold, request.AbsenceAction, request.ActorUserId);
        group.DefaultMonthlyFee = request.DefaultMonthlyFee;

        // 4. Retroactive recalculation if requested
        if (request.RecalculateNow && group.ConsecutiveAbsenceThreshold.HasValue && group.AbsenceAction != AbsenceAction.NotifyOnly)
        {
            var activeStudents = await _unitOfWork.GroupStudents
                .FindAsync(gs => gs.GroupId == group.Id && gs.Status == GroupStudentStatus.Active, cancellationToken);

            if (activeStudents.Count > 0)
            {
                var completedSessions = await _unitOfWork.Sessions
                    .GetCompletedSessionsInPeriodAsync(group.Id, null, cancellationToken);

                if (completedSessions.Count > 0)
                {
                    var sessionIds = completedSessions.Select(s => s.Id).ToHashSet();
                    var attendanceRecords = await _unitOfWork.AttendanceRecords
                        .FindAsync(ar => sessionIds.Contains(ar.SessionId) && ar.VoidedAt == null, cancellationToken);

                    var now = DateTimeOffset.UtcNow;

                    foreach (var studentEnrollment in activeStudents)
                    {
                        var streak = 0;
                        foreach (var session in completedSessions)
                        {
                            var record = attendanceRecords.FirstOrDefault(ar =>
                                ar.SessionId == session.Id && ar.StudentId == studentEnrollment.StudentId);

                            if (record is not null && record.Status == AttendanceStatus.Absent)
                            {
                                streak++;
                            }
                            else
                            {
                                streak = 0;
                                break;
                            }
                        }

                        if (streak >= group.ConsecutiveAbsenceThreshold.Value)
                        {
                            if (group.AbsenceAction == AbsenceAction.MarkSuspended)
                            {
                                studentEnrollment.Suspend(request.ActorUserId);
                                await _unitOfWork.GroupStudentStatusHistories.AddAsync(GroupStudentStatusHistory.Record(
                                    studentEnrollment.Id,
                                    GroupStudentStatus.Active,
                                    GroupStudentStatus.Suspended,
                                    now,
                                    request.ActorUserId,
                                    "Retroactive policy recalculation"), cancellationToken);
                            }
                            else if (group.AbsenceAction == AbsenceAction.Archive)
                            {
                                studentEnrollment.Withdraw(now, request.ActorUserId);
                                await _unitOfWork.GroupStudentStatusHistories.AddAsync(GroupStudentStatusHistory.Record(
                                    studentEnrollment.Id,
                                    GroupStudentStatus.Active,
                                    GroupStudentStatus.Withdrawn,
                                    now,
                                    request.ActorUserId,
                                    "Retroactive policy recalculation"), cancellationToken);
                            }
                        }
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var activeCount = await _unitOfWork.GroupStudents
            .CountAsync(gs => gs.GroupId == group.Id && gs.Status == GroupStudentStatus.Active, cancellationToken);

        var dto = group.Adapt<GroupDto>() with
        {
            TargetGender = request.TargetGender ?? "CoEd",
            MaxCapacity = request.MaxCapacity,
            ActiveStudentCount = activeCount
        };

        return Result.Success(dto);
    }
}
