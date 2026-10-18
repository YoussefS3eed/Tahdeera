using Tahdeera.Domain.Enums;

namespace Tahdeera.Domain.Entities;

public class StudentCredential
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid WorkspaceId { get; set; }
    public CredentialType CredentialType { get; set; }
    public byte[] TokenHash { get; set; } = Array.Empty<byte>();
    public string? CardUidDisplay { get; set; }
    public string? Label { get; set; }
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public string? RevocationReason { get; set; }
    public Guid IssuedByUserId { get; set; }

    // Navigation properties
    public Student Student { get; set; } = null!;
    public Workspace Workspace { get; set; } = null!;
    public AppUser IssuedByUser { get; set; } = null!;
    public AppUser? RevokedByUser { get; set; }

    public void Revoke(Guid revokedByUserId, string reason, DateTimeOffset utcNow)
    {
        if (RevokedAt != null)
        {
            throw new InvalidOperationException("Credential is already revoked.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        RevokedAt = utcNow;
        RevokedByUserId = revokedByUserId;
        RevocationReason = reason.Trim();
    }
}
