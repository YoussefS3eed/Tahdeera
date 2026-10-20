namespace Tahdeera.Application.Features.Sessions.DTOs;

public sealed record SessionCompletedSummaryDto(
    Guid SessionId,
    DateTimeOffset CompletedAt,
    int TotalActiveRoster,
    int ExistingCheckInsCount,
    int GeneratedAbsencesCount,
    int StagedOutboxMessagesCount,
    int StudentsSuspendedCount);
