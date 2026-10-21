using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Exceptions;

namespace Tahdeera.Domain.Entities;

public class WorkspaceWhatsAppConfig : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public WhatsAppSenderMode SenderMode { get; private set; }
    public bool IsNotificationsEnabled { get; private set; }
    public int MonthlyMessageQuota { get; private set; }
    public int MessagesUsedThisMonth { get; private set; }
    public Guid? ActiveSenderConnectionId { get; private set; }

    // Navigation properties
    public virtual Workspace? Workspace { get; private set; }
    public virtual WhatsAppSenderConnection? ActiveSenderConnection { get; private set; }

    private WorkspaceWhatsAppConfig() { } // EF Core

    public static WorkspaceWhatsAppConfig Create(
        Guid workspaceId,
        WhatsAppSenderMode senderMode = WhatsAppSenderMode.PlatformManaged,
        int monthlyQuota = 1000)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorkspaceWhatsAppConfig
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            SenderMode = senderMode,
            IsNotificationsEnabled = true,
            MonthlyMessageQuota = monthlyQuota,
            MessagesUsedThisMonth = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateSenderMode(WhatsAppSenderMode newMode, Guid? activeConnectionId = null)
    {
        if (newMode == WhatsAppSenderMode.WorkspaceOwned && !activeConnectionId.HasValue && !ActiveSenderConnectionId.HasValue)
            throw new DomainValidationException("SENDER_NOT_CONFIGURED", "Workspace-owned sender requires an active connection ID.");

        SenderMode = newMode;
        if (activeConnectionId.HasValue)
            ActiveSenderConnectionId = activeConnectionId;
        else if (newMode != WhatsAppSenderMode.WorkspaceOwned)
            ActiveSenderConnectionId = null;

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ToggleNotifications(bool isEnabled)
    {
        IsNotificationsEnabled = isEnabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetMonthlyQuota(int newQuota)
    {
        if (newQuota < 0)
            throw new DomainValidationException("INVALID_QUOTA", "Monthly message quota cannot be negative.");

        MonthlyMessageQuota = newQuota;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void IncrementUsage()
    {
        MessagesUsedThisMonth++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ResetMonthlyUsage()
    {
        MessagesUsedThisMonth = 0;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AssignActiveConnection(Guid connectionId)
    {
        ActiveSenderConnectionId = connectionId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearActiveConnection()
    {
        ActiveSenderConnectionId = null;
        if (SenderMode == WhatsAppSenderMode.WorkspaceOwned)
            SenderMode = WhatsAppSenderMode.Disabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
