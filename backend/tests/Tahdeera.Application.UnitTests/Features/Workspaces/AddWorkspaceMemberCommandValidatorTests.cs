using FluentAssertions;
using Tahdeera.Application.Features.Workspaces.Commands.AddWorkspaceMember;

namespace Tahdeera.Application.UnitTests.Features.Workspaces;

public class AddWorkspaceMemberCommandValidatorTests
{
    private readonly AddWorkspaceMemberCommandValidator _validator;

    public AddWorkspaceMemberCommandValidatorTests()
    {
        _validator = new AddWorkspaceMemberCommandValidator();
    }

    [Theory]
    [InlineData("Teacher")]
    [InlineData("Assistant")]
    [InlineData("Reception")]
    [InlineData("Accountant")]
    public async Task Validate_ValidCommand_ShouldPassValidation(string role)
    {
        // Arrange
        var command = new AddWorkspaceMemberCommand(
            WorkspaceId: Guid.NewGuid(),
            Username: "valid_user_1",
            Email: "valid.user@example.com",
            Password: "SecurePassword123!",
            DisplayName: "مستخدم جديد",
            Phone: "01012345678",
            Role: role);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("SuperAdmin")]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("")]
    public async Task Validate_InvalidRole_ShouldFailValidation(string invalidRole)
    {
        // Arrange
        var command = new AddWorkspaceMemberCommand(
            WorkspaceId: Guid.NewGuid(),
            Username: "valid_user_1",
            Email: "valid.user@example.com",
            Password: "SecurePassword123!",
            DisplayName: "مستخدم جديد",
            Phone: "01012345678",
            Role: invalidRole);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("nouppercase123!")]
    [InlineData("NoSpecialChar123")]
    [InlineData("NoDigitChar!!!")]
    public async Task Validate_WeakPassword_ShouldFailValidation(string weakPassword)
    {
        // Arrange
        var command = new AddWorkspaceMemberCommand(
            WorkspaceId: Guid.NewGuid(),
            Username: "valid_user_1",
            Email: "valid.user@example.com",
            Password: weakPassword,
            DisplayName: "مستخدم جديد",
            Phone: "01012345678",
            Role: "Teacher");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_EmptyWorkspaceId_ShouldFailValidation()
    {
        // Arrange
        var command = new AddWorkspaceMemberCommand(
            WorkspaceId: Guid.Empty,
            Username: "valid_user_1",
            Email: "valid.user@example.com",
            Password: "SecurePassword123!",
            DisplayName: "مستخدم جديد",
            Phone: "01012345678",
            Role: "Teacher");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WorkspaceId");
    }
}
