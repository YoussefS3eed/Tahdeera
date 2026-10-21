namespace Tahdeera.Application.Features.WhatsApp.Commands.RetryWhatsAppMessage;

using MediatR;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;

public sealed record RetryWhatsAppMessageCommand(
    Guid WorkspaceId,
    Guid MessageId) : IRequest<Result<RetryWhatsAppMessageResultDto>>;
