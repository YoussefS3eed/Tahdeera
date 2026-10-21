using FluentAssertions;
using Microsoft.Extensions.Options;
using Tahdeera.Infrastructure.Configuration;
using Tahdeera.Infrastructure.Services;
using Xunit;

namespace Tahdeera.Application.UnitTests.WhatsApp;

public sealed class WhatsAppRateCardServiceTests
{
    private readonly WhatsAppRateCardOptions _options;
    private readonly WhatsAppRateCardService _service;

    public WhatsAppRateCardServiceTests()
    {
        _options = new WhatsAppRateCardOptions
        {
            UsdToEgpExchangeRate = 50.0m,
            DefaultPerMessageUsd = 0.0270m,
            TemplateRatesUsd = new(StringComparer.OrdinalIgnoreCase)
            {
                ["attendance_check_in_v1"] = 0.0270m,
                ["custom_expensive_v1"] = 0.0500m
            }
        };

        _service = new WhatsAppRateCardService(Options.Create(_options));
    }

    [Fact]
    public void GetCostUsd_KnownTemplate_ReturnsConfiguredRate()
    {
        var cost = _service.GetCostUsd("custom_expensive_v1");
        cost.Should().Be(0.0500m);
    }

    [Fact]
    public void GetCostUsd_UnknownTemplate_ReturnsDefaultRate()
    {
        var cost = _service.GetCostUsd("unknown_template_v99");
        cost.Should().Be(0.0270m);
    }

    [Fact]
    public void GetCostEgp_CalculatesUsingExchangeRate()
    {
        // 0.0500 * 50.0 = 2.5000 EGP
        var costEgp = _service.GetCostEgp("custom_expensive_v1");
        costEgp.Should().Be(2.5000m);
    }

    [Fact]
    public void CalculateCost_ReturnsBothUsdAndEgpAccurately()
    {
        var (usd, egp) = _service.CalculateCost("attendance_check_in_v1");
        usd.Should().Be(0.0270m);
        egp.Should().Be(1.3500m); // 0.027 * 50 = 1.35
    }

    [Fact]
    public void DefaultPerMessageEgp_ReflectsExchangeRate()
    {
        _service.DefaultPerMessageEgp.Should().Be(1.3500m);
        _service.ExchangeRateEgpPerUsd.Should().Be(50.0m);
    }
}
