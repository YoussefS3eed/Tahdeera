using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.IntegrationTests.Models;

namespace Tahdeera.IntegrationTests.Helpers;

/// <summary>
/// Fluent assertion extensions for verifying RFC 9457 ProblemDetails responses
/// and custom Tahdeera error payloads.
/// </summary>
public static class ProblemDetailsAssertionExtensions
{
    /// <summary>
    /// Asserts status code, content-type (application/problem+json), error code, and trace ID.
    /// </summary>
    public static async Task<ProblemDetailsResponse> ShouldHaveProblemDetails(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedErrorCode)
    {
        response.StatusCode.Should().Be(expectedStatus);

        // Content-Type should be application/problem+json
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonSerializationHelper.Options);
        problem.Should().NotBeNull("Expected response body to be a valid RFC 9457 ProblemDetails JSON object.");
        problem!.Status.Should().Be((int)expectedStatus);
        problem.ErrorCode.Should().Be(expectedErrorCode);
        problem.TraceId.Should().NotBeNullOrWhiteSpace("Every RFC 9457 response MUST contain a valid non-empty traceId.");

        return problem;
    }

    /// <summary>
    /// Asserts presence of Egyptian customer support phone numbers for subscription lockout scenarios.
    /// </summary>
    public static void ShouldHaveSupportHotlines(this ProblemDetailsResponse problem)
    {
        problem.SupportPhone1.Should().Be("01272785618", "Expected Egyptian customer support hotline 1.");
        problem.SupportPhone2.Should().Be("01019482687", "Expected Egyptian customer support hotline 2.");
    }
}
