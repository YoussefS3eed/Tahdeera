using FluentAssertions;
using Tahdeera.Application.Common.Utilities;

namespace Tahdeera.Application.UnitTests.Finances;

public class StudentPrivacyMaskerTests
{
    [Theory]
    [InlineData(null, "***")]
    [InlineData("", "***")]
    [InlineData("   ", "***")]
    public void MaskEgyptianName_NullOrEmpty_ReturnsThreeAsterisks(string? input, string expected)
    {
        var result = StudentPrivacyMasker.MaskEgyptianName(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("أحمد", "أح***")]
    [InlineData("مي", "مي***")]
    [InlineData("علي", "عل***")]
    public void MaskEgyptianName_SingleWordName_MasksCorrectly(string input, string expected)
    {
        var result = StudentPrivacyMasker.MaskEgyptianName(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("أحمد محمد", "أحمد م***")]
    [InlineData("أحمد محمد علي", "أحمد م***")]
    [InlineData("محمود عبد الرحمن السيد", "محمود ع***")]
    [InlineData("سارة حسام حسن", "سارة ح***")]
    [InlineData("عبد الرحمن محمد علي", "عبد الرحمن م***")]
    [InlineData("سيف الدين خالد مصطفى", "سيف الدين خ***")]
    public void MaskEgyptianName_MultiWordEgyptianName_PreservesFirstNameAndMasksSecondInitial(string input, string expected)
    {
        var result = StudentPrivacyMasker.MaskEgyptianName(input);
        result.Should().Be(expected);
    }
}
