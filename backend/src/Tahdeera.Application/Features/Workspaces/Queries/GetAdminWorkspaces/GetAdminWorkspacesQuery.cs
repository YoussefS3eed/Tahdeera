using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Workspaces.Queries.GetAdminWorkspaces;

public record GetAdminWorkspacesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null) : IRequest<Result<PaginatedList<AdminWorkspaceListItemDto>>>;
