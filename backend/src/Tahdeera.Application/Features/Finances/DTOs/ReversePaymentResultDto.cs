namespace Tahdeera.Application.Features.Finances.DTOs;

public record ReversePaymentResultDto(
    Guid PaymentId,
    string Status,
    string ReversalReason,
    DateTimeOffset ReversedAt,
    int ReopenedChargesCount,
    decimal RestoredArrearsAmount);
