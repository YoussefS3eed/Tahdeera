namespace Tahdeera.Application.Features.Finances.DTOs;

public record StudentPaymentDto(
    Guid Id,
    Guid StudentAccountId,
    string ReceiptNumber,
    decimal Amount,
    DateTimeOffset PaidAt,
    string Method,
    string? Reference,
    string? Notes,
    string Status,
    DateTimeOffset? ReversedAt,
    string? ReversalReason,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    IReadOnlyList<PaymentAllocationDetailDto> Allocations);
