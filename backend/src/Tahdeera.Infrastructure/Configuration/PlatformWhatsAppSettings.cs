namespace Tahdeera.Infrastructure.Configuration;

public sealed class PlatformWhatsAppSettings
{
    public const string SectionName = "PlatformWhatsApp";

    public string PhoneNumberId { get; set; } = string.Empty;
    public string WabaId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string BusinessDisplayName { get; set; } = "Tahdeera";
    public bool IsEnabled { get; set; } = false;
    public string AppSecret { get; set; } = string.Empty;
    public string WebhookVerifyToken { get; set; } = string.Empty;
}
