using FluentAssertions;
using Tahdeera.Infrastructure.Services;

namespace Tahdeera.Application.UnitTests.Services;

public class WhatsAppRetryPolicyTests
{
    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 5)]
    [InlineData(4, 10)]
    [InlineData(5, 30)]
    [InlineData(6, 30)]
    [InlineData(10, 30)]
    public void GetRetryDelay_ReturnsExpectedInterval(int attempt, int expectedMinutes)
    {
        var delay = WhatsAppRetryPolicy.GetRetryDelay(attempt);
        delay.Should().Be(TimeSpan.FromMinutes(expectedMinutes));
    }
}
