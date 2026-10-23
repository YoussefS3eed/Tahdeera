namespace Tahdeera.Application.Features.Finances.DTOs;

public sealed record WorkspaceFinanceLedgerItemDto(
    string Id,
    Guid StudentId,
    string StudentName,
    string StudentCode,
    decimal Amount,
    string Type,
    string? PaymentMethod,
    DateTimeOffset RecordedAtUtc,
    string RecordedByUserName,
    string Status,
    string? ReversalReason,
    string? CancellationReason,
    string? Notes);
