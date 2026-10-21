namespace Tahdeera.Infrastructure.Configuration;

public sealed class WhatsAppRateCardOptions
{
    public const string SectionName = "WhatsAppRateCard";

    /// <summary>
    /// Conversion rate from USD to EGP. Default: 50.00 EGP per USD.
    /// </summary>
    public decimal UsdToEgpExchangeRate { get; set; } = 50.0m;

    /// <summary>
    /// Default utility template rate in USD for Egypt market ($0.0270).
    /// </summary>
    public decimal DefaultPerMessageUsd { get; set; } = 0.0270m;

    /// <summary>
    /// Specific per-template rates in USD.
    /// </summary>
    public Dictionary<string, decimal> TemplateRatesUsd { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["attendance_check_in_v1"] = 0.0270m,
        ["absence_alert_v1"] = 0.0270m,
        ["attendance_correction_v1"] = 0.0270m
    };
}
