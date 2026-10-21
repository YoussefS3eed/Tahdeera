using MediatR;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppUsage;

public sealed record GetWorkspaceWhatsAppUsageQuery(
    Guid WorkspaceId,
    string? YearMonth = null
) : IRequest<Result<WorkspaceWhatsAppUsageDto>>;
