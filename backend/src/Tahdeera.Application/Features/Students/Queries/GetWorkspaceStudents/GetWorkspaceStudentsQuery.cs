using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Students.Queries.GetWorkspaceStudents;

public record GetWorkspaceStudentsQuery(
    Guid WorkspaceId,
    string? SearchTerm = null,
    short? GradeId = null,
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 20,
    string SortBy = "enrolled_at",
    string SortDirection = "desc"
) : IRequest<Result<PagedList<StudentRosterItemDto>>>;
