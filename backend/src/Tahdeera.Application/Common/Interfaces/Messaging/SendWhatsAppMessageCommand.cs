namespace Tahdeera.Application.Common.Interfaces.Messaging;

public sealed record SendWhatsAppMessageCommand(
    Guid OutboxMessageId,
    Guid WorkspaceId,
    string RecipientPhoneE164,
    string MessageType,
    string TemplateName,
    string LanguageCode,
    string PayloadJson,
    DateTimeOffset DispatchedAt);
