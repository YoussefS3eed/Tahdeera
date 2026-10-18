using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Users.Commands.UpdatePreferences;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Users;

public class UpdatePreferencesCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly UpdatePreferencesCommandHandler _handler;

    private readonly DateTimeOffset _fixedTime = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    public UpdatePreferencesCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(_fixedTime);

        _handler = new UpdatePreferencesCommandHandler(
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_UpdateBothPreferences_ShouldUpdateAndReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Username = "teacher_ahmed",
            DisplayName = "أحمد محمود",
            PreferredLanguage = "ar",
            PreferredTheme = "light"
        };

        _unitOfWork.AppUsers.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new UpdatePreferencesCommand(userId, "en", "dark");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PreferredLanguage.Should().Be("en");
        result.Value.PreferredTheme.Should().Be("dark");
        user.PreferredLanguage.Should().Be("en");
        user.PreferredTheme.Should().Be("dark");
        user.UpdatedAt.Should().Be(_fixedTime);
        _unitOfWork.AppUsers.Received(1).Update(user);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateLanguageOnly_ShouldPreserveTheme()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Username = "teacher_ahmed",
            DisplayName = "أحمد محمود",
            PreferredLanguage = "ar",
            PreferredTheme = "light"
        };

        _unitOfWork.AppUsers.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new UpdatePreferencesCommand(userId, "en", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PreferredLanguage.Should().Be("en");
        result.Value.PreferredTheme.Should().Be("light");
        user.PreferredLanguage.Should().Be("en");
        user.PreferredTheme.Should().Be("light");
    }

    [Fact]
    public async Task Handle_UpdateThemeOnly_ShouldPreserveLanguage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Username = "teacher_ahmed",
            DisplayName = "أحمد محمود",
            PreferredLanguage = "ar",
            PreferredTheme = "light"
        };

        _unitOfWork.AppUsers.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new UpdatePreferencesCommand(userId, null, "dark");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PreferredLanguage.Should().Be("ar");
        result.Value.PreferredTheme.Should().Be("dark");
        user.PreferredLanguage.Should().Be("ar");
        user.PreferredTheme.Should().Be("dark");
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _unitOfWork.AppUsers.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var command = new UpdatePreferencesCommand(userId, "en", "dark");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("USER_NOT_FOUND");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
