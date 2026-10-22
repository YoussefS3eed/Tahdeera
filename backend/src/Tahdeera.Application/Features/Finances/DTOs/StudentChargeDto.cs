namespace Tahdeera.Application.Features.Finances.DTOs;

public record StudentChargeDto(
    Guid Id,
    Guid StudentAccountId,
    Guid? GroupId,
    string? GroupName,
    string? BillingPeriodLabel,
    string Description,
    decimal Amount,
    decimal SettledAmount,
    decimal RemainingAmount,
    DateTimeOffset? DueAt,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt);
