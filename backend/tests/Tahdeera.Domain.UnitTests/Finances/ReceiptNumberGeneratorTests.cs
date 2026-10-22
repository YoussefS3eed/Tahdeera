using FluentAssertions;
using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.UnitTests.Finances;

public class ReceiptNumberGeneratorTests
{
    [Fact]
    public void Generate_ShouldProduceCanonicalReceiptNumberFormat()
    {
        // Arrange
        const int yearMonth = 202610;
        var workspaceId = Guid.Parse("7b4f5261-0000-0000-0000-000000000000");
        const int sequence = 42;

        // Act
        var receiptNumber = ReceiptNumberGenerator.Generate(yearMonth, workspaceId, sequence);

        // Assert
        receiptNumber.Should().Be("REC-202610-7B4F52-00042");
    }

    [Theory]
    [InlineData(1, "00001")]
    [InlineData(99, "00099")]
    [InlineData(1000, "01000")]
    [InlineData(99999, "99999")]
    public void Generate_ShouldPadSequenceToFiveDigits(int sequence, string expectedPadding)
    {
        // Arrange
        const int yearMonth = 202610;
        var workspaceId = Guid.Parse("abcdef12-3456-7890-abcd-ef1234567890");

        // Act
        var receiptNumber = ReceiptNumberGenerator.Generate(yearMonth, workspaceId, sequence);

        // Assert
        receiptNumber.Should().Be($"REC-202610-ABCDEF-{expectedPadding}");
    }
}
