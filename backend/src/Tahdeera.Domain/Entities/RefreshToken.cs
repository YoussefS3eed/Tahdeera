using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public byte[] TokenHash { get; private set; } = [];
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public byte[]? ReplacedByTokenHash { get; private set; }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;

    // Navigation property
    public AppUser? User { get; private set; }

    public void Revoke(DateTimeOffset now, byte[]? replacedByTokenHash = null)
    {
        RevokedAt = now;
        ReplacedByTokenHash = replacedByTokenHash;
        UpdatedAt = now;
    }
}
