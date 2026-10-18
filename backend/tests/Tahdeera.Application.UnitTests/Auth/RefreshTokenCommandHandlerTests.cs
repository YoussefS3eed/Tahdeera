using FluentAssertions;
using NSubstitute;
using System.Linq.Expressions;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Auth.Commands.RefreshToken;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.UnitTests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;
    private readonly RefreshTokenCommandHandler _handler;

    private readonly DateTimeOffset _fixedTime = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    public RefreshTokenCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _auditLogService = Substitute.For<IAuditLogService>();

        _dateTimeProvider.UtcNow.Returns(_fixedTime);

        _handler = new RefreshTokenCommandHandler(
            _unitOfWork,
            _jwtTokenService,
            _dateTimeProvider,
            _auditLogService);
    }

    [Fact]
    public async Task Handle_ValidTokenRotation_ShouldReturnNewTokenPair()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Username = "teacher_ahmed",
            IsActive = true
        };

        var oldHash = new byte[] { 1, 1, 1 };
        var tokenRecord = RefreshToken.Create(userId, oldHash, _fixedTime.AddDays(7));

        _jwtTokenService.HashToken("old-plaintext-token").Returns(oldHash);

        _unitOfWork.RefreshTokens
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<RefreshToken, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(tokenRecord);

        _unitOfWork.AppUsers
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _jwtTokenService
            .GenerateAccessToken(user)
            .Returns(("new-access-token", _fixedTime.AddMinutes(15)));

        _jwtTokenService
            .GenerateRefreshToken()
            .Returns("new-plaintext-token");

        var newHash = new byte[] { 2, 2, 2 };
        _jwtTokenService.HashToken("new-plaintext-token").Returns(newHash);

        var command = new RefreshTokenCommand("old-plaintext-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-plaintext-token");
        tokenRecord.RevokedAt.Should().Be(_fixedTime);
        tokenRecord.ReplacedByTokenHash.Should().BeEquivalentTo(newHash);
        await _unitOfWork.RefreshTokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenNotFound_ShouldReturnInvalid()
    {
        // Arrange
        _jwtTokenService.HashToken("unknown-token").Returns(new byte[] { 9, 9, 9 });

        _unitOfWork.RefreshTokens
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<RefreshToken, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var command = new RefreshTokenCommand("unknown-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_REFRESH_TOKEN_INVALID");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ReplayAttackOnRevokedToken_ShouldRevokeAllUserTokensAndReturnRevoked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldHash = new byte[] { 1, 1, 1 };
        var revokedToken = RefreshToken.Create(userId, oldHash, _fixedTime.AddDays(7));
        revokedToken.Revoke(_fixedTime.AddHours(-1)); // already revoked

        var otherActiveToken = RefreshToken.Create(userId, new byte[] { 3, 3, 3 }, _fixedTime.AddDays(7));

        _jwtTokenService.HashToken("replayed-token").Returns(oldHash);

        _unitOfWork.RefreshTokens
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<RefreshToken, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(revokedToken);

        _unitOfWork.RefreshTokens
            .FindAsync(Arg.Any<Expression<Func<RefreshToken, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken> { otherActiveToken });

        var command = new RefreshTokenCommand("replayed-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_REFRESH_TOKEN_REVOKED");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        otherActiveToken.RevokedAt.Should().Be(_fixedTime);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditLogService.Received(1).LogAsync(
            userId,
            null,
            "AUTH_TOKEN_BREACH_CONTAINED",
            "RefreshToken",
            revokedToken.Id.ToString(),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ShouldReturnExpired()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var hash = new byte[] { 1, 1, 1 };
        var expiredToken = RefreshToken.Create(userId, hash, _fixedTime.AddMinutes(-5)); // expired 5 minutes ago

        _jwtTokenService.HashToken("expired-token").Returns(hash);

        _unitOfWork.RefreshTokens
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<RefreshToken, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(expiredToken);

        var command = new RefreshTokenCommand("expired-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_REFRESH_TOKEN_EXPIRED");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ShouldReturnUserDeactivated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new AppUser
        {
            Id = userId,
            Username = "deactivated_user",
            IsActive = false
        };

        var hash = new byte[] { 1, 1, 1 };
        var token = RefreshToken.Create(userId, hash, _fixedTime.AddDays(7));

        _jwtTokenService.HashToken("valid-token").Returns(hash);

        _unitOfWork.RefreshTokens
            .FirstOrDefaultAsync(Arg.Any<Expression<Func<RefreshToken, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(token);

        _unitOfWork.AppUsers
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new RefreshTokenCommand("valid-token");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_USER_DEACTIVATED");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }
}
