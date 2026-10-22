using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Domain.Enums;

namespace Tahdeera.API.Contracts.Requests;

public record CreateStudentChargeRequest(
    Guid? GroupId,
    string? BillingPeriodLabel,
    string Description,
    decimal Amount,
    DateTimeOffset? DueAt,
    string? Notes);

public record CancelStudentChargeRequest(
    string Reason);

public record WriteOffStudentChargeRequest(
    string Reason);

public record RecordStudentPaymentRequest(
    decimal Amount,
    PaymentMethod Method,
    DateTimeOffset? PaidAt,
    string? Reference,
    string? Notes,
    IReadOnlyList<PaymentAllocationRequestDto>? Allocations);

public record ReverseStudentPaymentRequest(
    string Reason);

public record BatchCreateGroupChargesRequest(
    string BillingPeriodLabel,
    string Description,
    decimal Amount,
    DateTimeOffset? DueAt = null,
    string? Notes = null);

public record StageTuitionRemindersRequest(
    Guid? GroupId = null,
    decimal MinimumBalanceThreshold = 50.00m);
