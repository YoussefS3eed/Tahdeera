using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Students.Queries.GetWorkspaceStudents;

public class GetWorkspaceStudentsQueryHandler : IRequestHandler<GetWorkspaceStudentsQuery, Result<PagedList<StudentRosterItemDto>>>
{
    private readonly IWorkspaceStudentRepository _workspaceStudentRepository;
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IStudentCredentialRepository _credentialRepository;

    public GetWorkspaceStudentsQueryHandler(
        IWorkspaceStudentRepository workspaceStudentRepository,
        IAcademicYearRepository academicYearRepository,
        IStudentCredentialRepository credentialRepository)
    {
        _workspaceStudentRepository = workspaceStudentRepository;
        _academicYearRepository = academicYearRepository;
        _credentialRepository = credentialRepository;
    }

    public async Task<Result<PagedList<StudentRosterItemDto>>> Handle(GetWorkspaceStudentsQuery request, CancellationToken cancellationToken)
    {
        var activeYear = await _academicYearRepository.GetCurrentYearAsync(cancellationToken);
        var activeYearId = activeYear?.Id;

        WorkspaceStudentStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<WorkspaceStudentStatus>(request.Status, ignoreCase: true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (pagedRows, totalCount) = await _workspaceStudentRepository.GetWorkspaceRosterAsync(
            request.WorkspaceId,
            request.SearchTerm,
            request.GradeId,
            activeYearId,
            statusFilter,
            pageNumber,
            pageSize,
            request.SortBy ?? "enrolledat",
            request.SortDirection ?? "desc",
            cancellationToken);

        var studentIds = pagedRows.Select(r => r.StudentId).ToList();
        var activeCardStudentIds = await _credentialRepository.GetStudentIdsWithActiveCardsAsync(
            request.WorkspaceId,
            studentIds,
            cancellationToken);

        var items = pagedRows.Select(ws =>
        {
            var academicRecord = (activeYearId != null
                ? ws.Student.AcademicHistories.FirstOrDefault(ah => ah.AcademicYearId == activeYearId)
                : null)
                ?? ws.Student.AcademicHistories.OrderByDescending(ah => ah.RecordedAt).FirstOrDefault();

            return new StudentRosterItemDto(
                StudentId: ws.StudentId,
                StudentCode: ws.Student.StudentCode,
                FullName: ws.Student.FullName,
                GradeId: academicRecord?.GradeId ?? 0,
                GradeNameAr: academicRecord?.Grade?.NameAr ?? string.Empty,
                GradeNameEn: academicRecord?.Grade?.NameEn ?? string.Empty,
                Status: ws.Status.ToString(),
                GuardianPhone: ws.GuardianPhone,
                StudentPhone: ws.StudentPhone,
                WhatsappOptIn: ws.WhatsappOptIn,
                SeatNumber: ws.SeatNumber,
                HasActiveCard: activeCardStudentIds.Contains(ws.StudentId),
                EnrolledAt: ws.EnrolledAt
            );
        }).ToList();

        var pagedList = PagedList<StudentRosterItemDto>.Create(items, totalCount, pageNumber, pageSize);
        return Result.Success(pagedList);
    }
}
