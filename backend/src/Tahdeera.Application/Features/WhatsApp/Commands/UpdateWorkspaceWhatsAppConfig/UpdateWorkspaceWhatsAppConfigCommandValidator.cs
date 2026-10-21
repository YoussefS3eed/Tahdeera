namespace Tahdeera.Application.Features.WhatsApp.Commands.UpdateWorkspaceWhatsAppConfig;

using FluentValidation;

public sealed class UpdateWorkspaceWhatsAppConfigCommandValidator : AbstractValidator<UpdateWorkspaceWhatsAppConfigCommand>
{
    private static readonly string[] AllowedModes = ["PlatformManaged", "WorkspaceOwned", "Disabled"];

    public UpdateWorkspaceWhatsAppConfigCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SenderMode)
            .NotEmpty()
            .Must(m => AllowedModes.Contains(m))
            .WithMessage("SenderMode must be PlatformManaged, WorkspaceOwned, or Disabled.");
    }
}
