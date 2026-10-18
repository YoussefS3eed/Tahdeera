using FluentAssertions;
using NSubstitute;
using System.Linq.Expressions;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Auth.Commands.Login;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Auth;

public class LoginCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly LoginCommandHandler _handler;

    private readonly DateTimeOffset _fixedTime = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    public LoginCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _identityService = Substitute.For<IIdentityService>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(_fixedTime);

        _handler = new LoginCommandHandler(
            _unitOfWork,
            _identityService,
            _jwtTokenService,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_ValidEmailLogin_ShouldReturnSuccess()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "teacher_ahmed",
            DisplayName = "أحمد محمود",
            NormalizedEmail = "TEACHER@TAHDEERA.APP",
            Phone = "+201012345678",
            IsActive = true,
            PreferredLanguage = "ar",
            PreferredTheme = "light"
        };

        _unitOfWork.AppUsers
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<AppUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _identityService
            .CheckPasswordAsync(user.Username, "StrongPassword123!", Arg.Any<CancellationToken>())
            .Returns((true, user.Id));

        _jwtTokenService
            .GenerateAccessToken(user)
            .Returns(("access-token-123", _fixedTime.AddMinutes(15)));

        _jwtTokenService
            .GenerateRefreshToken()
            .Returns("plaintext-refresh-token");

        _jwtTokenService
            .HashToken("plaintext-refresh-token")
            .Returns(new byte[] { 1, 2, 3 });

        _unitOfWork.WorkspaceMembers
            .FindAsync(Arg.Any<Expression<Func<WorkspaceMember, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceMember>());

        var command = new LoginCommand("teacher@tahdeera.app", "StrongPassword123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token-123");
        result.Value.RefreshToken.Should().Be("plaintext-refresh-token");
        result.Value.User.Username.Should().Be("teacher_ahmed");
        await _unitOfWork.RefreshTokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidEgyptianPhoneLogin_ShouldNormalizeAndSucceed()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "teacher_ahmed",
            DisplayName = "أحمد محمود",
            Phone = "+201012345678",
            IsActive = true
        };

        _unitOfWork.AppUsers
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<AppUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _identityService
            .CheckPasswordAsync(user.Username, "StrongPassword123!", Arg.Any<CancellationToken>())
            .Returns((true, user.Id));

        _jwtTokenService
            .GenerateAccessToken(user)
            .Returns(("access-token-123", _fixedTime.AddMinutes(15)));

        _jwtTokenService
            .GenerateRefreshToken()
            .Returns("plaintext-refresh-token");

        _jwtTokenService
            .HashToken("plaintext-refresh-token")
            .Returns(new byte[] { 1, 2, 3 });

        _unitOfWork.WorkspaceMembers
            .FindAsync(Arg.Any<Expression<Func<WorkspaceMember, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkspaceMember>());

        // User enters '01012345678'
        var command = new LoginCommand("01012345678", "StrongPassword123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token-123");
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnInvalidCredentials()
    {
        // Arrange
        _unitOfWork.AppUsers
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<AppUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var command = new LoginCommand("unknown@tahdeera.app", "Password123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_INVALID_CREDENTIALS");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        await _identityService.Received(1).CheckPasswordAsync("unknown@tahdeera.app", "Password123!", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidPassword_ShouldReturnInvalidCredentials()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "teacher_ahmed",
            IsActive = true
        };

        _unitOfWork.AppUsers
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<AppUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _identityService
            .CheckPasswordAsync(user.Username, "WrongPassword!", Arg.Any<CancellationToken>())
            .Returns((false, Guid.Empty));

        var command = new LoginCommand("teacher_ahmed", "WrongPassword!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_INVALID_CREDENTIALS");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ShouldReturnUserDeactivated()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "deactivated_user",
            IsActive = false
        };

        _unitOfWork.AppUsers
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<AppUser, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new LoginCommand("deactivated_user", "Password123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_USER_DEACTIVATED");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }
}
