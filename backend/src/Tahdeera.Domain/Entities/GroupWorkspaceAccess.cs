namespace Tahdeera.Domain.Entities;

public class GroupWorkspaceAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid WorkspaceId { get; set; }
    public bool CanViewRoster { get; set; } = false;
    public bool CanRegisterAttendance { get; set; } = false;
    public bool CanEditAttendance { get; set; } = false;
    public Guid GrantedByUserId { get; set; }
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Group Group { get; set; } = null!;
    public Workspace Workspace { get; set; } = null!;
    public AppUser GrantedByUser { get; set; } = null!;
    public AppUser? RevokedByUser { get; set; }

    public static GroupWorkspaceAccess Grant(
        Guid groupId,
        Guid workspaceId,
        Guid grantedByUserId,
        bool canViewRoster = false,
        bool canRegisterAttendance = false,
        bool canEditAttendance = false,
        string? notes = null)
    {
        return new GroupWorkspaceAccess
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            WorkspaceId = workspaceId,
            GrantedByUserId = grantedByUserId,
            CanViewRoster = canViewRoster,
            CanRegisterAttendance = canRegisterAttendance,
            CanEditAttendance = canEditAttendance,
            Notes = notes?.Trim(),
            GrantedAt = DateTimeOffset.UtcNow
        };
    }

    public void Revoke(Guid revokedByUserId, DateTimeOffset revokedAt)
    {
        if (RevokedAt != null)
        {
            throw new InvalidOperationException("Group workspace access is already revoked.");
        }

        if (revokedAt < GrantedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(revokedAt), "Revocation time cannot be earlier than grant time.");
        }

        RevokedAt = revokedAt;
        RevokedByUserId = revokedByUserId;
    }

    public void UpdatePermissions(
        bool canViewRoster,
        bool canRegisterAttendance,
        bool canEditAttendance)
    {
        if (RevokedAt != null)
        {
            throw new InvalidOperationException("Cannot update permissions on revoked workspace access.");
        }

        CanViewRoster = canViewRoster;
        CanRegisterAttendance = canRegisterAttendance;
        CanEditAttendance = canEditAttendance;
    }
}
