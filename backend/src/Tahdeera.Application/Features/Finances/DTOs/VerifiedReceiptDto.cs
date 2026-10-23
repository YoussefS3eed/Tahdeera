namespace Tahdeera.Application.Features.Finances.DTOs;

public sealed record VerifiedReceiptDto(
    bool IsValid,
    string ReceiptNumber,
    string WorkspaceName,
    string StudentNameMasked,
    decimal AmountEgp,
    DateTimeOffset PaidAtUtc,
    string PaymentMethod,
    string Status,
    DateTimeOffset? ReversedAtUtc);
