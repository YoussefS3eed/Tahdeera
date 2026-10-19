using MediatR;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Students.Queries.GetStudentDetails;

public class GetStudentDetailsQueryHandler : IRequestHandler<GetStudentDetailsQuery, Result<StudentDetailsDto>>
{
    private readonly IWorkspaceStudentRepository _workspaceStudentRepository;
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IStudentCredentialRepository _credentialRepository;

    public GetStudentDetailsQueryHandler(
        IWorkspaceStudentRepository workspaceStudentRepository,
        IAcademicYearRepository academicYearRepository,
        IStudentCredentialRepository credentialRepository)
    {
        _workspaceStudentRepository = workspaceStudentRepository;
        _academicYearRepository = academicYearRepository;
        _credentialRepository = credentialRepository;
    }

    public async Task<Result<StudentDetailsDto>> Handle(GetStudentDetailsQuery request, CancellationToken cancellationToken)
    {
        var wsStudent = await _workspaceStudentRepository.GetWithStudentAndAcademicHistoryAsync(
            request.WorkspaceId,
            request.StudentId,
            cancellationToken);

        if (wsStudent is null)
        {
            return Error.NotFound("STUDENT_NOT_FOUND_IN_WORKSPACE", "الطالب غير مسجل في مركز الدروس الحالي.");
        }

        var activeYear = await _academicYearRepository.GetCurrentYearAsync(cancellationToken);

        var academicRecord = (activeYear != null
            ? wsStudent.Student.AcademicHistories.FirstOrDefault(ah => ah.AcademicYearId == activeYear.Id)
            : null)
            ?? wsStudent.Student.AcademicHistories.OrderByDescending(ah => ah.RecordedAt).FirstOrDefault();

        var hasActiveCard = await _credentialRepository.HasActiveCredentialAsync(
            request.WorkspaceId,
            request.StudentId,
            cancellationToken);

        var dto = new StudentDetailsDto(
            StudentId: wsStudent.StudentId,
            StudentCode: wsStudent.Student.StudentCode,
            FullName: wsStudent.Student.FullName,
            GradeId: academicRecord?.GradeId ?? 0,
            GradeNameAr: academicRecord?.Grade?.NameAr ?? string.Empty,
            GradeNameEn: academicRecord?.Grade?.NameEn ?? string.Empty,
            Status: wsStudent.Status.ToString(),
            GuardianName: wsStudent.GuardianName,
            GuardianPhone: wsStudent.GuardianPhone,
            StudentPhone: wsStudent.StudentPhone,
            WhatsappOptIn: wsStudent.WhatsappOptIn,
            SeatNumber: wsStudent.SeatNumber,
            LocalNotes: wsStudent.LocalNotes,
            HasActiveCard: hasActiveCard,
            EnrolledAt: wsStudent.EnrolledAt
        );

        return Result.Success(dto);
    }
}
