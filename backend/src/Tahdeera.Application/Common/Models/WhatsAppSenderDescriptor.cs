using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.Common.Models;

public sealed record WhatsAppSenderDescriptor
{
    public required Guid WorkspaceId { get; init; }
    public required WhatsAppSenderMode SenderMode { get; init; }
    public required string PhoneNumberId { get; init; }
    public required string AccessToken { get; init; }
    public required string DisplayPhoneNumber { get; init; }
    public string? VerifiedBusinessName { get; init; }
    public required bool IsActive { get; init; }
    public string? InactiveReason { get; init; }

    public static WhatsAppSenderDescriptor Platform(
        Guid workspaceId,
        string phoneNumberId,
        string accessToken,
        string displayPhone,
        string? businessName) => new()
    {
        WorkspaceId = workspaceId,
        SenderMode = WhatsAppSenderMode.PlatformManaged,
        PhoneNumberId = phoneNumberId,
        AccessToken = accessToken,
        DisplayPhoneNumber = displayPhone,
        VerifiedBusinessName = businessName,
        IsActive = true
    };

    public static WhatsAppSenderDescriptor Workspace(
        Guid workspaceId,
        string phoneNumberId,
        string decryptedAccessToken,
        string displayPhone,
        string? businessName) => new()
    {
        WorkspaceId = workspaceId,
        SenderMode = WhatsAppSenderMode.WorkspaceOwned,
        PhoneNumberId = phoneNumberId,
        AccessToken = decryptedAccessToken,
        DisplayPhoneNumber = displayPhone,
        VerifiedBusinessName = businessName,
        IsActive = true
    };

    public static WhatsAppSenderDescriptor Disabled(Guid workspaceId) => new()
    {
        WorkspaceId = workspaceId,
        SenderMode = WhatsAppSenderMode.Disabled,
        PhoneNumberId = string.Empty,
        AccessToken = string.Empty,
        DisplayPhoneNumber = string.Empty,
        IsActive = false,
        InactiveReason = "WORKSPACE_NOTIFICATIONS_DISABLED"
    };

    public static WhatsAppSenderDescriptor Unavailable(
        Guid workspaceId,
        WhatsAppSenderMode mode,
        string reason) => new()
    {
        WorkspaceId = workspaceId,
        SenderMode = mode,
        PhoneNumberId = string.Empty,
        AccessToken = string.Empty,
        DisplayPhoneNumber = string.Empty,
        IsActive = false,
        InactiveReason = reason
    };
}
