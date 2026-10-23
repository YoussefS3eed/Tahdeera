using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceStudents;

public sealed record GetWorkspaceFinanceStudentsQuery(
    Guid WorkspaceId,
    int PageIndex = 0,
    int PageSize = 20,
    string? Status = null,
    string? Search = null,
    Guid? GroupId = null) : IRequest<PagedResult<WorkspaceFinanceStudentItemDto>>;
