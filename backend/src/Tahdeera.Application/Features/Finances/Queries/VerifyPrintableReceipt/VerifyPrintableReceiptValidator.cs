using FluentValidation;

namespace Tahdeera.Application.Features.Finances.Queries.VerifyPrintableReceipt;

public class VerifyPrintableReceiptValidator : AbstractValidator<VerifyPrintableReceiptQuery>
{
    public VerifyPrintableReceiptValidator()
    {
        RuleFor(x => x.ReceiptNumber)
            .NotEmpty();

        RuleFor(x => x.Hash)
            .NotEmpty()
            .Matches("^[a-fA-F0-9]{64}$")
            .WithMessage("INVALID_RECEIPT_VERIFICATION_TOKEN");
    }
}
