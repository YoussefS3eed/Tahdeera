using MediatR;
using Tahdeera.Application.Common.Models.Webhooks;

namespace Tahdeera.Application.Features.WhatsApp.Commands.ProcessWhatsAppWebhook;

public sealed record ProcessWhatsAppWebhookCommand(MetaWebhookPayload Payload) : IRequest;
