namespace Tahdeera.API.Contracts.Requests;

public sealed record UpdateWhatsAppSettingsRequest(
    string SenderMode,
    bool IsNotificationsEnabled);

public sealed record ConnectWhatsAppSenderRequest(
    string Code,
    string WabaId,
    string PhoneNumberId,
    string DisplayPhoneNumber);
