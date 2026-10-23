namespace Tahdeera.Application.Features.Finances.DTOs;

public sealed record WorkspaceFinanceStudentItemDto(
    Guid StudentId,
    string StudentName,
    string StudentCode,
    string? GradeName,
    string? GroupName,
    decimal RequiredFee,
    decimal PaidAmount,
    decimal RemainingFee,
    string PaymentStatus, // "Paid", "Partial", "Unpaid", "Settled"
    DateTimeOffset? LastPaymentDate);
