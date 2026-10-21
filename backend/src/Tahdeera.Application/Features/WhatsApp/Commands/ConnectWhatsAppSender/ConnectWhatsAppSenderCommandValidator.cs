namespace Tahdeera.Application.Features.WhatsApp.Commands.ConnectWhatsAppSender;

using FluentValidation;

public sealed class ConnectWhatsAppSenderCommandValidator : AbstractValidator<ConnectWhatsAppSenderCommand>
{
    public ConnectWhatsAppSenderCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().WithMessage("Authorization code is required from Embedded Signup.");
        RuleFor(x => x.WabaId).NotEmpty().WithMessage("WabaId is required.");
        RuleFor(x => x.PhoneNumberId).NotEmpty().WithMessage("PhoneNumberId is required.");
        RuleFor(x => x.DisplayPhoneNumber).NotEmpty().WithMessage("DisplayPhoneNumber is required.");
    }
}
