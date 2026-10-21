namespace Tahdeera.Application.Features.WhatsApp.Commands.DisconnectWhatsAppSender;

using MediatR;
using Tahdeera.Domain.Common;

public sealed record DisconnectWhatsAppSenderCommand(Guid WorkspaceId) : IRequest<Result>;
