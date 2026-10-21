namespace Tahdeera.Application.Common.Interfaces.Services;

public interface IWhatsAppRateCardService
{
    decimal ExchangeRateEgpPerUsd { get; }
    decimal DefaultPerMessageUsd { get; }
    decimal DefaultPerMessageEgp { get; }

    decimal GetCostUsd(string templateName);
    decimal GetCostEgp(string templateName);
    (decimal CostUsd, decimal CostEgp) CalculateCost(string templateName);
}
