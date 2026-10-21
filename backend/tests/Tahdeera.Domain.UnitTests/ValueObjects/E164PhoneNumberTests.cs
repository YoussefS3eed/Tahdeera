using FluentAssertions;
using Tahdeera.Domain.Exceptions;
using Tahdeera.Domain.ValueObjects;

namespace Tahdeera.Domain.UnitTests.ValueObjects;

public class E164PhoneNumberTests
{
    [Theory]
    [InlineData("01019482687", "+201019482687")]
    [InlineData("01155123456", "+201155123456")]
    [InlineData("01288990011", "+201288990011")]
    [InlineData("01511223344", "+201511223344")]
    public void Create_WithValidLocalNumber_NormalizesToE164(string input, string expected)
    {
        var phone = E164PhoneNumber.Create(input);

        phone.Value.Should().Be(expected);
        ((string)phone).Should().Be(expected);
        phone.ToString().Should().Be(expected);
    }

    [Fact]
    public void Create_WithPrefixed20Number_NormalizesToE164()
    {
        var phone = E164PhoneNumber.Create("201019482687");

        phone.Value.Should().Be("+201019482687");
    }

    [Fact]
    public void Create_WithE164Number_PreservesValue()
    {
        var phone = E164PhoneNumber.Create("+201019482687");

        phone.Value.Should().Be("+201019482687");
    }

    [Fact]
    public void Create_WithWhitespaceAndHyphens_StripsAndNormalizes()
    {
        var phone = E164PhoneNumber.Create(" 010-1948-2687 ");

        phone.Value.Should().Be("+201019482687");
    }

    [Theory]
    [InlineData("01312345678")]
    [InlineData("02345678")]
    [InlineData("010123")]
    [InlineData("abcdef")]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidPrefixOrLength_ThrowsDomainValidationException(string input)
    {
        var act = () => E164PhoneNumber.Create(input);

        var exception = act.Should().Throw<DomainValidationException>().Which;
        exception.ErrorCode.Should().Be("INVALID_PHONE_NUMBER");
    }
}
