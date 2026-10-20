using Mapster;
using MediatR;
using Tahdeera.Application.Features.Groups.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Groups.Commands.CreateGroup;

public sealed class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Result<GroupDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateGroupCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GroupDto>> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify academic year exists
        var academicYear = await _unitOfWork.AcademicYears
            .FirstOrDefaultAsync(a => a.Id == request.AcademicYearId, cancellationToken);

        if (academicYear is null)
        {
            return Error.NotFound("ACADEMIC_YEAR_NOT_FOUND", "العام الدراسي المحدد غير موجود.");
        }

        // 2. Check duplicate name in same workspace & academic year via repository
        var exists = await _unitOfWork.Groups.ExistsByNameAsync(
            request.WorkspaceId,
            request.AcademicYearId,
            request.Name,
            null,
            cancellationToken);

        if (exists)
        {
            return DomainErrors.Groups.NameDuplicate;
        }

        // 3. Create Group entity
        var group = Group.Create(
            ownerWorkspaceId: request.WorkspaceId,
            academicYearId: request.AcademicYearId,
            gradeId: request.GradeId,
            name: request.Name,
            responsibleTeacherUserId: request.ActorUserId,
            subjectId: request.SubjectId,
            roomName: request.RoomName,
            scheduleSummary: request.ScheduleSummary,
            lateGraceMinutes: request.LateGraceMinutes,
            defaultMonthlyFee: request.DefaultMonthlyFee,
            consecutiveAbsenceThreshold: request.ConsecutiveAbsenceThreshold,
            absenceAction: request.AbsenceAction,
            createdByUserId: request.ActorUserId);

        await _unitOfWork.Groups.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Map to DTO via Mapster
        var dto = group.Adapt<GroupDto>();
        return Result.Success(dto);
    }
}
