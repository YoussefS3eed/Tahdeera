using FluentValidation;

namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppUsage;

public sealed class GetWorkspaceWhatsAppUsageQueryValidator : AbstractValidator<GetWorkspaceWhatsAppUsageQuery>
{
    public GetWorkspaceWhatsAppUsageQueryValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("WorkspaceId is required.");

        When(x => !string.IsNullOrWhiteSpace(x.YearMonth), () =>
        {
            RuleFor(x => x.YearMonth)
                .Matches(@"^\d{4}-(0[1-9]|1[0-2])$")
                .WithMessage("YearMonth must follow 'YYYY-MM' format (e.g., 2026-10).");
        });
    }
}
