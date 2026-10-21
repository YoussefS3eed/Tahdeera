namespace Tahdeera.Application.Features.WhatsApp.Commands.ConnectWhatsAppSender;

using MediatR;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;

public sealed record ConnectWhatsAppSenderCommand(
    Guid WorkspaceId,
    string Code,
    string WabaId,
    string PhoneNumberId,
    string DisplayPhoneNumber) : IRequest<Result<WhatsAppSenderConnectionDto>>;
