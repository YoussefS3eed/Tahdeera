using MediatR;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Commands.BatchCreateGroupCharges;

public record BatchCreateGroupChargesCommand(
    Guid WorkspaceId,
    Guid GroupId,
    string BillingPeriodLabel,
    string Description,
    decimal Amount,
    DateTimeOffset? DueAt = null,
    string? Notes = null
) : IRequest<BatchCreateGroupChargesResultDto>;
