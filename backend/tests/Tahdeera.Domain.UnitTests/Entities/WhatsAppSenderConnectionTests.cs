using FluentAssertions;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.UnitTests.Entities;

public class WhatsAppSenderConnectionTests
{
    [Fact]
    public void CreateWorkspaceConnection_FactoryMethod_InitializesPropertiesCorrectly()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var wabaId = "waba_123456";
        var phoneNumberId = "phone_id_789";
        var displayPhoneNumber = "+201000000001";
        var encryptedToken = "enc_token_xyz";
        var verifiedName = "Tahdeera Center Official";
        var metadata = "{\"tier\":\"Tier 1\"}";

        // Act
        var conn = WhatsAppSenderConnection.CreateWorkspaceConnection(
            workspaceId,
            wabaId,
            phoneNumberId,
            displayPhoneNumber,
            encryptedToken,
            verifiedName,
            metadata);

        // Assert
        conn.Id.Should().NotBeEmpty();
        conn.WorkspaceId.Should().Be(workspaceId);
        conn.OwnershipType.Should().Be(WhatsAppOwnershipType.Workspace);
        conn.WabaId.Should().Be(wabaId);
        conn.PhoneNumberId.Should().Be(phoneNumberId);
        conn.DisplayPhoneNumber.Should().Be(displayPhoneNumber);
        conn.EncryptedAccessToken.Should().Be(encryptedToken);
        conn.VerifiedBusinessName.Should().Be(verifiedName);
        conn.BusinessDisplayName.Should().Be(verifiedName);
        conn.Status.Should().Be(SenderConnectionStatus.Connected);
        conn.ConnectedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        conn.DisconnectedAt.Should().BeNull();
        conn.RevocationReason.Should().BeNull();
        conn.MetadataJson.Should().Be(metadata);
        conn.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        conn.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Disconnect_SetsStatusDisconnectedAndRecordsReason()
    {
        // Arrange
        var conn = WhatsAppSenderConnection.CreateWorkspaceConnection(
            Guid.NewGuid(), "waba_1", "phone_1", "+201011111111", "token", "Name");

        // Act
        conn.Disconnect("Owner revoked Meta WABA access");

        // Assert
        conn.Status.Should().Be(SenderConnectionStatus.Disconnected);
        conn.DisconnectedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        conn.RevocationReason.Should().Be("Owner revoked Meta WABA access");
    }

    [Fact]
    public void MarkRestricted_SetsStatusRestrictedAndRecordsReason()
    {
        // Arrange
        var conn = WhatsAppSenderConnection.CreateWorkspaceConnection(
            Guid.NewGuid(), "waba_1", "phone_1", "+201011111111", "token", "Name");

        // Act
        conn.MarkRestricted("Meta Policy Warning 102: Spam Reports Exceeded");

        // Assert
        conn.Status.Should().Be(SenderConnectionStatus.Restricted);
        conn.RevocationReason.Should().Be("Meta Policy Warning 102: Spam Reports Exceeded");
    }

    [Fact]
    public void UpdateBusinessInfo_UpdatesDisplayNumberAndVerifiedName()
    {
        // Arrange
        var conn = WhatsAppSenderConnection.CreateWorkspaceConnection(
            Guid.NewGuid(), "waba_1", "phone_1", "+201011111111", "token", "Old Name");

        // Act
        conn.UpdateBusinessInfo("+201022222222", "New Verified Center", "{\"quality\":\"GREEN\"}");

        // Assert
        conn.DisplayPhoneNumber.Should().Be("+201022222222");
        conn.VerifiedBusinessName.Should().Be("New Verified Center");
        conn.MetadataJson.Should().Be("{\"quality\":\"GREEN\"}");
    }
}
