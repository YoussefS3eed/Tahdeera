using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.UnitTests.Entities;

public class StudentCredentialTests
{
    [Fact]
    public void Revoke_WhenActive_SetsRevocationProperties()
    {
        // Arrange
        var cred = new StudentCredential
        {
            CredentialType = CredentialType.Nfc,
            TokenHash = new byte[] { 1, 2, 3 }
        };
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        cred.Revoke(userId, "Card lost by student", now);

        // Assert
        cred.RevokedAt.Should().Be(now);
        cred.RevokedByUserId.Should().Be(userId);
        cred.RevocationReason.Should().Be("Card lost by student");
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ThrowsInvalidOperationException()
    {
        // Arrange
        var cred = new StudentCredential();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        cred.Revoke(userId, "Lost", now);

        // Act
        var act = () => cred.Revoke(userId, "Stolen", now.AddMinutes(5));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Credential is already revoked.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Revoke_WithEmptyReason_ThrowsArgumentException(string? invalidReason)
    {
        // Arrange
        var cred = new StudentCredential();

        // Act
        var act = () => cred.Revoke(Guid.NewGuid(), invalidReason!, DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
