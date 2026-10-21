namespace Tahdeera.Application.Features.WhatsApp.DTOs;

public sealed record WorkspaceWhatsAppUsageDto(
    Guid WorkspaceId,
    string YearMonth,
    int MonthlyMessageQuota,
    int MessagesDispatchedCount,
    int MessagesDeliveredCount,
    int MessagesFailedCount,
    int MessagesRemainingThisMonth,
    bool IsQuotaWarning,
    bool IsQuotaExceeded,
    double DeliverySuccessRatePercentage,
    decimal EstimatedCostUsd,
    decimal EstimatedCostEgp,
    decimal RatePerMessageUsd,
    decimal RatePerMessageEgp,
    decimal ExchangeRateEgpPerUsd
);
