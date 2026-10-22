namespace Tahdeera.Application.Features.Finances.DTOs;

public record AccountTransactionDto(
    Guid Id,
    string Type, // "Charge" | "Payment"
    DateTimeOffset Date,
    string? ReferenceOrReceipt,
    string Description,
    decimal Amount,
    string Status,
    decimal? SettledAmount,
    decimal? RemainingAmount,
    string? PaymentMethod,
    IReadOnlyList<AllocationItemDto>? Allocations);
