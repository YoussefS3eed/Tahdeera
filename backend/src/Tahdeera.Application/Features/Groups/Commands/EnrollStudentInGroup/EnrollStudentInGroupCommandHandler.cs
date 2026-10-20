using Mapster;
using MediatR;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Commands.EnrollStudentInGroup;

public sealed class EnrollStudentInGroupCommandHandler : IRequestHandler<EnrollStudentInGroupCommand, Result<GroupStudentEnrollmentDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public EnrollStudentInGroupCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GroupStudentEnrollmentDto>> Handle(EnrollStudentInGroupCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch group
        var group = await _unitOfWork.Groups.GetByIdForWorkspaceAsync(request.WorkspaceId, request.GroupId, cancellationToken);

        if (group is null)
        {
            return DomainErrors.Groups.NotFound;
        }

        // 2. Verify workspace student active status
        var wsStudent = await _unitOfWork.WorkspaceStudents
            .GetByWorkspaceAndStudentIdAsync(request.WorkspaceId, request.StudentId, cancellationToken);

        if (wsStudent is null || wsStudent.Status != WorkspaceStudentStatus.Active)
        {
            return DomainErrors.Groups.StudentWorkspaceStatusNotActive;
        }

        // 3. Verify academic grade matches group grade
        var academicHistory = await _unitOfWork.StudentAcademicHistories
            .FirstOrDefaultAsync(h => h.StudentId == request.StudentId && h.AcademicYearId == group.AcademicYearId, cancellationToken);

        if (academicHistory is null || academicHistory.GradeId != group.GradeId)
        {
            return DomainErrors.Groups.GradeMismatch;
        }

        // 4. Query existing group membership
        var existingEnrollment = await _unitOfWork.GroupStudents
            .FirstOrDefaultAsync(gs => gs.GroupId == request.GroupId && gs.StudentId == request.StudentId, cancellationToken);

        var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId, cancellationToken);
        var studentName = student?.FullName ?? string.Empty;
        var studentCode = student?.StudentCode ?? string.Empty;

        // 5. Handle according to Option C
        if (existingEnrollment is not null)
        {
            if (existingEnrollment.Status == GroupStudentStatus.Active)
            {
                // Idempotent return without duplicate rows or status history entries
                var activeDto = existingEnrollment.Adapt<GroupStudentEnrollmentDto>() with
                {
                    StudentName = studentName,
                    StudentCode = studentCode
                };
                return Result.Success(activeDto);
            }

            if (existingEnrollment.Status == GroupStudentStatus.Suspended)
            {
                // Reject - requires explicit administrative reinstatement
                return DomainErrors.Groups.StudentSuspendedRequiresReinstatement;
            }

            if (existingEnrollment.Status == GroupStudentStatus.Withdrawn)
            {
                // In-place reactivation
                var now = DateTimeOffset.UtcNow;
                existingEnrollment.Status = GroupStudentStatus.Active;
                existingEnrollment.JoinedAt = now;
                existingEnrollment.LeftAt = null;
                existingEnrollment.UpdatedByUserId = request.ActorUserId;
                existingEnrollment.UpdatedAt = now;

                await _unitOfWork.GroupStudentStatusHistories.AddAsync(GroupStudentStatusHistory.Record(
                    existingEnrollment.Id,
                    GroupStudentStatus.Withdrawn,
                    GroupStudentStatus.Active,
                    now,
                    request.ActorUserId,
                    "Re-enrolled in group"), cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var reactivatedDto = existingEnrollment.Adapt<GroupStudentEnrollmentDto>() with
                {
                    StudentName = studentName,
                    StudentCode = studentCode
                };
                return Result.Success(reactivatedDto);
            }
        }

        // 6. New enrollment
        var enrollNow = DateTimeOffset.UtcNow;
        var newEnrollment = GroupStudent.Create(group.Id, request.StudentId, enrollNow);
        await _unitOfWork.GroupStudents.AddAsync(newEnrollment, cancellationToken);

        await _unitOfWork.GroupStudentStatusHistories.AddAsync(GroupStudentStatusHistory.Record(
            newEnrollment.Id,
            null,
            GroupStudentStatus.Active,
            enrollNow,
            request.ActorUserId,
            "Initial group enrollment"), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdDto = newEnrollment.Adapt<GroupStudentEnrollmentDto>() with
        {
            StudentName = studentName,
            StudentCode = studentCode
        };

        return Result.Success(createdDto);
    }
}
