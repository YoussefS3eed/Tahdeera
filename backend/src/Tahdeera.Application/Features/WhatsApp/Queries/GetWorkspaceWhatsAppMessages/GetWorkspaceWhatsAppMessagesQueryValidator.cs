namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppMessages;

using FluentValidation;

public sealed class GetWorkspaceWhatsAppMessagesQueryValidator : AbstractValidator<GetWorkspaceWhatsAppMessagesQuery>
{
    public GetWorkspaceWhatsAppMessagesQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
