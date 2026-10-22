using MediatR;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceSummary;

public record GetWorkspaceFinanceSummaryQuery(
    Guid WorkspaceId) : IRequest<WorkspaceFinanceSummaryDto>;
