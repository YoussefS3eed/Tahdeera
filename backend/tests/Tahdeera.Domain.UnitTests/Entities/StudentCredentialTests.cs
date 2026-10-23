using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.UnitTests.Entities;

public class StudentCredentialTests
{
    [Fact]
    public void StudentCredential_DefaultInitialization_SetsDefaults()
    {
        // Act
        var cred = new StudentCredential();

        // Assert
        cred.Id.Should().NotBeEmpty();
        cred.TokenHash.Should().BeEmpty();
        cred.CardUidDisplay.Should().BeNull();
        cred.Label.Should().BeNull();
        cred.IssuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        cred.ExpiresAt.Should().BeNull();
        cred.RevokedAt.Should().BeNull();
        cred.RevokedByUserId.Should().BeNull();
        cred.RevocationReason.Should().BeNull();
    }

    [Fact]
    public void StudentCredential_PropertyAssignment_PreservesValues()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var issuerId = Guid.NewGuid();
        var hash = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var expires = DateTimeOffset.UtcNow.AddYears(1);

        // Act
        var cred = new StudentCredential
        {
            StudentId = studentId,
            WorkspaceId = workspaceId,
            CredentialType = CredentialType.Nfc,
            TokenHash = hash,
            CardUidDisplay = "04:A1:B2:C3",
            Label = "Student Main Card",
            IssuedByUserId = issuerId,
            ExpiresAt = expires
        };

        // Assert
        cred.StudentId.Should().Be(studentId);
        cred.WorkspaceId.Should().Be(workspaceId);
        cred.CredentialType.Should().Be(CredentialType.Nfc);
        cred.TokenHash.Should().Equal(hash);
        cred.CardUidDisplay.Should().Be("04:A1:B2:C3");
        cred.Label.Should().Be("Student Main Card");
        cred.IssuedByUserId.Should().Be(issuerId);
        cred.ExpiresAt.Should().Be(expires);
    }

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
