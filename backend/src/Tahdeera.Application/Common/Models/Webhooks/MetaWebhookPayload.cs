using System.Text.Json.Serialization;

namespace Tahdeera.Application.Common.Models.Webhooks;

public sealed record MetaWebhookPayload
{
    [JsonPropertyName("object")]
    public string Object { get; init; } = string.Empty;

    [JsonPropertyName("entry")]
    public List<MetaWebhookEntry> Entry { get; init; } = [];
}

public sealed record MetaWebhookEntry
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("changes")]
    public List<MetaWebhookChange> Changes { get; init; } = [];
}

public sealed record MetaWebhookChange
{
    [JsonPropertyName("field")]
    public string Field { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public MetaWebhookValue Value { get; init; } = new();
}

public sealed record MetaWebhookValue
{
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; init; } = string.Empty;

    [JsonPropertyName("metadata")]
    public MetaWebhookMetadata? Metadata { get; init; }

    [JsonPropertyName("statuses")]
    public List<MetaWebhookStatus>? Statuses { get; init; }
}

public sealed record MetaWebhookMetadata
{
    [JsonPropertyName("display_phone_number")]
    public string DisplayPhoneNumber { get; init; } = string.Empty;

    [JsonPropertyName("phone_number_id")]
    public string PhoneNumberId { get; init; } = string.Empty;
}

public sealed record MetaWebhookStatus
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty; // wamid.HBg...

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty; // "delivered", "read", "failed", "sent"

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty; // Unix timestamp in seconds

    [JsonPropertyName("recipient_id")]
    public string RecipientId { get; init; } = string.Empty;

    [JsonPropertyName("conversation")]
    public MetaWebhookConversation? Conversation { get; init; }

    [JsonPropertyName("pricing")]
    public MetaWebhookPricing? Pricing { get; init; }

    [JsonPropertyName("errors")]
    public List<MetaWebhookError>? Errors { get; init; }
}

public sealed record MetaWebhookConversation
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("origin")]
    public MetaWebhookConversationOrigin? Origin { get; init; }
}

public sealed record MetaWebhookConversationOrigin
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}

public sealed record MetaWebhookPricing
{
    [JsonPropertyName("billable")]
    public bool Billable { get; init; }

    [JsonPropertyName("pricing_model")]
    public string PricingModel { get; init; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;
}

public sealed record MetaWebhookError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("error_data")]
    public MetaWebhookErrorData? ErrorData { get; init; }
}

public sealed record MetaWebhookErrorData
{
    [JsonPropertyName("details")]
    public string? Details { get; init; }
}
