namespace Tahdeera.Application.Features.WhatsApp.Commands.UpdateWorkspaceWhatsAppConfig;

using MediatR;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;

public sealed record UpdateWorkspaceWhatsAppConfigCommand(
    Guid WorkspaceId,
    string SenderMode,
    bool IsNotificationsEnabled) : IRequest<Result<WorkspaceWhatsAppConfigDto>>;
