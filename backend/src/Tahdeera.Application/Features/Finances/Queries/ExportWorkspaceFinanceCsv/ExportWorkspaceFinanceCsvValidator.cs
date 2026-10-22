using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Queries.ExportWorkspaceFinanceCsv;

public class ExportWorkspaceFinanceCsvValidator : AbstractValidator<ExportWorkspaceFinanceCsvQuery>
{
    public ExportWorkspaceFinanceCsvValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).NotEmpty().GreaterThanOrEqualTo(x => x.FromDate).WithMessage("INVALID_DATE_RANGE");
        RuleFor(x => x)
            .Must(q => (q.ToDate - q.FromDate).TotalDays <= 90)
            .WithMessage("EXPORT_DATE_SPAN_EXCEEDED");
    }
}
