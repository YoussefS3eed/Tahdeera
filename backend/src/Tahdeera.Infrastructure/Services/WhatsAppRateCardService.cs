using Microsoft.Extensions.Options;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Infrastructure.Configuration;

namespace Tahdeera.Infrastructure.Services;

public sealed class WhatsAppRateCardService : IWhatsAppRateCardService
{
    private readonly WhatsAppRateCardOptions _options;

    public WhatsAppRateCardService(IOptions<WhatsAppRateCardOptions> options)
    {
        _options = options.Value;
    }

    public decimal ExchangeRateEgpPerUsd => _options.UsdToEgpExchangeRate;

    public decimal DefaultPerMessageUsd => _options.DefaultPerMessageUsd;

    public decimal DefaultPerMessageEgp => decimal.Round(_options.DefaultPerMessageUsd * _options.UsdToEgpExchangeRate, 4);

    public decimal GetCostUsd(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            return _options.DefaultPerMessageUsd;
        }

        if (_options.TemplateRatesUsd.TryGetValue(templateName, out var rate))
        {
            return rate;
        }

        return _options.DefaultPerMessageUsd;
    }

    public decimal GetCostEgp(string templateName)
    {
        var usd = GetCostUsd(templateName);
        return decimal.Round(usd * _options.UsdToEgpExchangeRate, 4);
    }

    public (decimal CostUsd, decimal CostEgp) CalculateCost(string templateName)
    {
        var usd = GetCostUsd(templateName);
        var egp = decimal.Round(usd * _options.UsdToEgpExchangeRate, 4);
        return (usd, egp);
    }
}
