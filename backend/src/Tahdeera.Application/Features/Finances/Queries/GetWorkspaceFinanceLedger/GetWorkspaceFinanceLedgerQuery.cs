using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceLedger;

public sealed record GetWorkspaceFinanceLedgerQuery(
    Guid WorkspaceId,
    int PageIndex = 0,
    int PageSize = 20,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    string? Status = null,
    string? TransactionType = null) : IRequest<PagedResult<WorkspaceFinanceLedgerItemDto>>;
