namespace Tahdeera.API.Contracts.Requests;

public sealed record RecordWorkspacePaymentRequest(
    Guid StudentId,
    decimal Amount,
    string PaymentMethod,
    DateTimeOffset? PaidAt,
    string? TransactionReference,
    string? Notes,
    IReadOnlyList<PaymentAllocationRequestItem>? Allocations);

public sealed record PaymentAllocationRequestItem(
    Guid ChargeId,
    decimal Amount);
