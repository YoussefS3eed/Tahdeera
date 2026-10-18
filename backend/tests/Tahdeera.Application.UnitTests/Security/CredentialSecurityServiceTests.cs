using System.Diagnostics;
using System.Text.RegularExpressions;
using FluentAssertions;
using Tahdeera.Infrastructure.Services;

namespace Tahdeera.Application.UnitTests.Security;

public class CredentialSecurityServiceTests
{
    private readonly CredentialSecurityService _sut = new();

    [Fact]
    public void HashToken_ProducesDeterministic32ByteDigest()
    {
        // Arrange
        const string rawToken = "test_nfc_card_raw_secret_12345";

        // Act
        var hash1 = _sut.HashToken(rawToken);
        var hash2 = _sut.HashToken(rawToken);

        // Assert
        hash1.Should().NotBeNull();
        hash1.Should().HaveCount(32);
        hash2.Should().Equal(hash1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void HashToken_WithNullOrWhitespace_ThrowsArgumentException(string? invalidToken)
    {
        // Act
        var act = () => _sut.HashToken(invalidToken!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateStudentCode_MatchesRegexAndValidCharset()
    {
        // Arrange
        var codeRegex = new Regex(@"^STU-2026-\d{5}$");
        var generatedCodes = new HashSet<string>();

        // Act & Assert (100 iterations)
        for (int i = 0; i < 100; i++)
        {
            var code = _sut.GenerateStudentCode(2026);
            code.Should().HaveLength(14);
            codeRegex.IsMatch(code).Should().BeTrue($"Code '{code}' should match regex format and alphabet");
            generatedCodes.Add(code);
        }

        // Verify high entropy / distribution
        generatedCodes.Count.Should().BeGreaterThan(90);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void GenerateStudentCode_WithOutOfRangeYear_ThrowsArgumentOutOfRangeException(int invalidYear)
    {
        // Act
        var act = () => _sut.GenerateStudentCode(invalidYear);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GenerateQrPayload_MatchesStandardUriFormat()
    {
        // Arrange
        var qrRegex = new Regex(@"^tahdeera://qr/[0-9a-f]{32}$");
        var generatedPayloads = new HashSet<string>();

        // Act & Assert (100 iterations)
        for (int i = 0; i < 100; i++)
        {
            var payload = _sut.GenerateQrPayload();
            payload.Should().HaveLength(46);
            qrRegex.IsMatch(payload).Should().BeTrue($"Payload '{payload}' should match standard URI format");
            generatedPayloads.Add(payload).Should().BeTrue();
        }
    }

    [Fact]
    public async Task ExecuteDummyVerificationDelayAsync_CompletesWithinExpectedTimingRange()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _sut.ExecuteDummyVerificationDelayAsync();
        stopwatch.Stop();

        // Assert: 35ms - 65ms nominal delay. Allowing 25ms floor and 300ms ceiling for CI/OS scheduling jitter.
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(25);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(300);
    }
}
