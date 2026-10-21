using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.Entities;

public class WhatsAppSenderConnection : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public WhatsAppOwnershipType OwnershipType { get; private set; }
    public string WabaId { get; private set; } = string.Empty;
    public string PhoneNumberId { get; private set; } = string.Empty;
    public string DisplayPhoneNumber { get; private set; } = string.Empty;
    public string? VerifiedBusinessName { get; private set; }
    public string EncryptedAccessToken { get; private set; } = string.Empty;
    public SenderConnectionStatus Status { get; private set; }
    public DateTimeOffset? ConnectedAt { get; private set; }
    public DateTimeOffset? DisconnectedAt { get; private set; }
    public string? RevocationReason { get; private set; }
    public string? MetadataJson { get; private set; }

    public virtual Workspace? Workspace { get; private set; }

    private WhatsAppSenderConnection() { } // EF Core

    public static WhatsAppSenderConnection CreateWorkspaceConnection(
        Guid workspaceId,
        string wabaId,
        string phoneNumberId,
        string displayPhoneNumber,
        string encryptedAccessToken,
        string? verifiedBusinessName,
        string? metadataJson = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new WhatsAppSenderConnection
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            OwnershipType = WhatsAppOwnershipType.Workspace,
            WabaId = wabaId,
            PhoneNumberId = phoneNumberId,
            DisplayPhoneNumber = displayPhoneNumber,
            EncryptedAccessToken = encryptedAccessToken,
            VerifiedBusinessName = verifiedBusinessName,
            Status = SenderConnectionStatus.Connected,
            ConnectedAt = now,
            MetadataJson = metadataJson,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Disconnect(string reason)
    {
        Status = SenderConnectionStatus.Disconnected;
        DisconnectedAt = DateTimeOffset.UtcNow;
        RevocationReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRestricted(string reason)
    {
        Status = SenderConnectionStatus.Restricted;
        RevocationReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateBusinessInfo(string displayPhoneNumber, string? verifiedName, string? metadataJson)
    {
        DisplayPhoneNumber = displayPhoneNumber;
        VerifiedBusinessName = verifiedName;
        if (!string.IsNullOrWhiteSpace(metadataJson))
            MetadataJson = metadataJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
