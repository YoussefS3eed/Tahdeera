namespace Tahdeera.Application.Features.WhatsApp.Commands.RetryWhatsAppMessage;

using FluentValidation;

public sealed class RetryWhatsAppMessageCommandValidator : AbstractValidator<RetryWhatsAppMessageCommand>
{
    public RetryWhatsAppMessageCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
    }
}
