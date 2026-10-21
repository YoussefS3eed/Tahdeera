namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppConfig;

using MediatR;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;

public sealed record GetWorkspaceWhatsAppConfigQuery(Guid WorkspaceId) : IRequest<Result<WorkspaceWhatsAppConfigDto>>;
