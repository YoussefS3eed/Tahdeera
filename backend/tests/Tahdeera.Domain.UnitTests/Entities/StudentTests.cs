using FluentAssertions;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Entities;

public class StudentTests
{
    [Fact]
    public void UpdateName_WithValidName_UpdatesFullNameAndTimestamp()
    {
        // Arrange
        var initialUpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var student = new Student
        {
            FullName = "Old Name",
            UpdatedAt = initialUpdatedAt
        };

        // Act
        student.UpdateName("  Ahmed Mohamed Ali  ");

        // Assert
        student.FullName.Should().Be("Ahmed Mohamed Ali");
        student.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        student.UpdatedAt.Should().BeAfter(initialUpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateName_WithEmptyString_ThrowsArgumentException(string? invalidName)
    {
        // Arrange
        var student = new Student { FullName = "Existing Student" };

        // Act
        var act = () => student.UpdateName(invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateName_Exceeding200Chars_ThrowsArgumentException()
    {
        // Arrange
        var student = new Student { FullName = "Existing Student" };
        var excessiveName = new string('x', 201);

        // Act
        var act = () => student.UpdateName(excessiveName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Full name cannot exceed 200 characters.*");
    }
}
