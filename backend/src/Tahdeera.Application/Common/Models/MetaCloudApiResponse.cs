namespace Tahdeera.Application.Common.Models;

public sealed class MetaCloudApiResponse
{
    public bool IsSuccess { get; private init; }
    public bool IsTransient { get; private init; }
    public string? ProviderMessageId { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }
    public int? MetaSubcode { get; private init; }
    public string? FbTraceId { get; private init; }

    public static MetaCloudApiResponse Success(string providerMessageId) => new()
    {
        IsSuccess = true,
        IsTransient = false,
        ProviderMessageId = providerMessageId
    };

    public static MetaCloudApiResponse TransientFailure(string errorCode, string errorMessage, int? subcode = null, string? fbTraceId = null) => new()
    {
        IsSuccess = false,
        IsTransient = true,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        MetaSubcode = subcode,
        FbTraceId = fbTraceId
    };

    public static MetaCloudApiResponse PermanentFailure(string errorCode, string errorMessage, int? subcode = null, string? fbTraceId = null) => new()
    {
        IsSuccess = false,
        IsTransient = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        MetaSubcode = subcode,
        FbTraceId = fbTraceId
    };
}
