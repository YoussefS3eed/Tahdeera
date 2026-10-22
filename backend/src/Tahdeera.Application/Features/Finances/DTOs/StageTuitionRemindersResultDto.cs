namespace Tahdeera.Application.Features.Finances.DTOs;

public record StageTuitionRemindersResultDto(
    Guid WorkspaceId,
    Guid? GroupId,
    decimal MinimumBalanceThreshold,
    int TotalDebtorsFound,
    int RemindersStagedCount,
    int SkippedCooldownCount,
    int SkippedOptedOutCount,
    int SkippedInvalidPhoneCount,
    decimal TotalOutstandingArrearsNotified
);
