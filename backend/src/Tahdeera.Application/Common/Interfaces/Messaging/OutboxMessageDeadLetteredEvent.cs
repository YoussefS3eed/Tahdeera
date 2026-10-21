namespace Tahdeera.Application.Common.Interfaces.Messaging;

public sealed record OutboxMessageDeadLetteredEvent(
    Guid OutboxMessageId,
    Guid WorkspaceId,
    string ErrorCode,
    string ErrorMessage,
    int TotalAttempts,
    DateTimeOffset FailedAt);
