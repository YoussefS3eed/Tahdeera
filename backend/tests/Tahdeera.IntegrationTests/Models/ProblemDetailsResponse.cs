namespace Tahdeera.IntegrationTests.Models;

/// <summary>
/// Strongly-typed deserialization DTO for RFC 9457 ProblemDetails responses,
/// including Tahdeera custom error codes and Egyptian support contact extensions.
/// </summary>
public class ProblemDetailsResponse
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
    public string? ErrorCode { get; set; }
    public string? TraceId { get; set; }
    public string? SupportPhone1 { get; set; }
    public string? SupportPhone2 { get; set; }
    public string? SuspensionReason { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}
