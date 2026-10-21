namespace Tahdeera.Application.Features.WhatsApp.DTOs;

public sealed record WorkspaceWhatsAppConfigDto(
    Guid WorkspaceId,
    string SenderMode,
    bool IsNotificationsEnabled,
    int MonthlyMessageQuota,
    int MessagesUsedThisMonth,
    WhatsAppSenderConnectionDto? ActiveConnection);
