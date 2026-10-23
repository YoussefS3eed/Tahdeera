using FluentAssertions;
using Tahdeera.Application.Common.Utilities;

namespace Tahdeera.Application.UnitTests.Finances;

public class TafqeetTests
{
    [Fact]
    public void Convert_ZeroAmount_ReturnsZeroCurrencyString()
    {
        // Act
        var result = Tafqeet.Convert(0.00m);

        // Assert
        result.Should().Be("فقط صفر جنيهاً مصرياً لا غير");
    }

    [Fact]
    public void Convert_FourHundred_ReturnsCorrectWords()
    {
        // Act
        var result = Tafqeet.Convert(400.00m);

        // Assert
        result.Should().Be("فقط أربعمائة جنيهاً مصرياً لا غير");
    }

    [Fact]
    public void Convert_FourHundredFifty_ReturnsCorrectWords()
    {
        // Act
        var result = Tafqeet.Convert(450.00m);

        // Assert
        result.Should().Be("فقط أربعمائة وخمسون جنيهاً مصرياً لا غير");
    }

    [Fact]
    public void Convert_OneThousandTwoHundredFiftyAndFiftyPiastres_ReturnsCorrectWords()
    {
        // Act
        var result = Tafqeet.Convert(1250.50m);

        // Assert
        result.Should().Be("فقط ألف ومائتان وخمسون جنيهاً مصرياً وخمسون قرشاً لا غير");
    }

    [Fact]
    public void Convert_SeventyFiveAndTwentyFivePiastres_ReturnsCorrectWords()
    {
        // Act
        var result = Tafqeet.Convert(75.25m);

        // Assert
        result.Should().Be("فقط خمسة وسبعون جنيهاً مصرياً وخمسة وعشرون قرشاً لا غير");
    }

    [Fact]
    public void Convert_NegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Act
        var act = () => Tafqeet.Convert(-10m);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
