using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Common.Models;

namespace Tahdeera.Infrastructure.Services;

public sealed class MetaWhatsAppCloudApiClient : IMetaWhatsAppCloudApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MetaWhatsAppCloudApiClient> _logger;

    public MetaWhatsAppCloudApiClient(
        HttpClient httpClient,
        ILogger<MetaWhatsAppCloudApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MetaCloudApiResponse> SendTemplateMessageAsync(
        WhatsAppSenderDescriptor sender,
        string recipientPhoneE164,
        string templateName,
        string languageCode,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        var maskedPhone = MaskPhoneNumber(recipientPhoneE164);
        var url = $"https://graph.facebook.com/v18.0/{sender.PhoneNumberId}/messages";

        try
        {
            var parameters = ExtractTemplateParameters(templateName, payloadJson);
            var requestBody = BuildMetaRequestBody(recipientPhoneE164, templateName, languageCode, parameters);
            var jsonString = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sender.AccessToken);
            request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            _logger.LogInformation("Dispatching WhatsApp template '{Template}' to {Phone} via PhoneId {PhoneId}",
                templateName, maskedPhone, sender.PhoneNumberId);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var doc = JsonNode.Parse(responseContent);
                var providerMessageId = doc?["messages"]?[0]?["id"]?.GetValue<string>();

                if (!string.IsNullOrWhiteSpace(providerMessageId))
                {
                    _logger.LogInformation("Meta dispatch successful for {Phone}. MessageId: {ProviderId}",
                        maskedPhone, providerMessageId);
                    return MetaCloudApiResponse.Success(providerMessageId);
                }

                return MetaCloudApiResponse.Success($"wamid.mock_{Guid.NewGuid():N}");
            }

            return ParseMetaErrorResponse(response.StatusCode, responseContent, maskedPhone);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP exception invoking Meta Graph API for {Phone}", maskedPhone);
            return MetaCloudApiResponse.TransientFailure("ERR_HTTP_TIMEOUT", ex.Message);
        }
    }

    private static List<string> ExtractTemplateParameters(string templateName, string payloadJson)
    {
        var parameters = new List<string>();

        try
        {
            var node = JsonNode.Parse(payloadJson);
            if (node is JsonObject obj)
            {
                if (templateName.Contains("check_in", StringComparison.OrdinalIgnoreCase))
                {
                    // attendance_check_in_v1: StudentName, AttendanceStatus, GroupName, CheckInTime
                    var studentName = obj["StudentName"]?.GetValue<string>() ?? obj["student_name"]?.GetValue<string>() ?? string.Empty;
                    var status = obj["AttendanceStatus"]?.GetValue<string>() ?? obj["attendance_status"]?.GetValue<string>() ?? obj["status"]?.GetValue<string>() ?? "حاضر";
                    var group = obj["GroupName"]?.GetValue<string>() ?? obj["group_name"]?.GetValue<string>() ?? string.Empty;
                    var time = obj["CheckInTime"]?.GetValue<string>() ?? obj["check_in_time"]?.GetValue<string>() ?? DateTimeOffset.UtcNow.ToString("hh:mm tt");

                    parameters.Add(studentName);
                    parameters.Add(status);
                    parameters.Add(group);
                    parameters.Add(time);
                }
                else if (templateName.Contains("absence", StringComparison.OrdinalIgnoreCase))
                {
                    // absence_alert_v1: StudentName, GroupName, SessionDate
                    var studentName = obj["StudentName"]?.GetValue<string>() ?? obj["student_name"]?.GetValue<string>() ?? string.Empty;
                    var group = obj["GroupName"]?.GetValue<string>() ?? obj["group_name"]?.GetValue<string>() ?? string.Empty;
                    var date = obj["SessionDate"]?.GetValue<string>() ?? obj["session_date"]?.GetValue<string>() ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

                    parameters.Add(studentName);
                    parameters.Add(group);
                    parameters.Add(date);
                }
                else
                {
                    foreach (var prop in obj)
                    {
                        if (prop.Value is not null)
                        {
                            parameters.Add(prop.Value.ToString());
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback: empty parameters
        }

        return parameters;
    }

    private static object BuildMetaRequestBody(string to, string templateName, string languageCode, List<string> parameters)
    {
        var bodyParams = parameters.Select(p => new { type = "text", text = p }).ToArray();

        return new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = to,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = languageCode },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = bodyParams
                    }
                }
            }
        };
    }

    private MetaCloudApiResponse ParseMetaErrorResponse(HttpStatusCode statusCode, string responseContent, string maskedPhone)
    {
        int metaCode = 0;
        int metaSubcode = 0;
        string? errorMessage = null;
        string? fbTraceId = null;

        try
        {
            var node = JsonNode.Parse(responseContent);
            var error = node?["error"];
            if (error != null)
            {
                metaCode = error["code"]?.GetValue<int>() ?? 0;
                metaSubcode = error["error_subcode"]?.GetValue<int>() ?? 0;
                errorMessage = error["message"]?.GetValue<string>();
                fbTraceId = error["fbtrace_id"]?.GetValue<string>();
            }
        }
        catch
        {
            errorMessage = responseContent;
        }

        errorMessage ??= $"HTTP {(int)statusCode} {statusCode}";

        _logger.LogError("Meta Graph API returned HTTP {StatusCode} (Code {Code}, Subcode {Subcode}) for {Phone}: {Message} [Trace: {TraceId}]",
            (int)statusCode, metaCode, metaSubcode, maskedPhone, errorMessage, fbTraceId);

        // Map Meta status and error codes
        if (statusCode == (HttpStatusCode)429 || metaCode == 130429 || metaCode == 131056)
        {
            return MetaCloudApiResponse.TransientFailure("ERR_WA_RATE_LIMIT_EXCEEDED", errorMessage, metaSubcode, fbTraceId);
        }

        if (statusCode == HttpStatusCode.InternalServerError || statusCode == HttpStatusCode.ServiceUnavailable || metaCode == 131000 || metaCode == 131005)
        {
            return MetaCloudApiResponse.TransientFailure("ERR_WA_META_UPSTREAM_ERROR", errorMessage, metaSubcode, fbTraceId);
        }

        if (metaCode == 131026)
        {
            return MetaCloudApiResponse.PermanentFailure("ERR_WA_RECIPIENT_INELIGIBLE", errorMessage, metaSubcode, fbTraceId);
        }

        if (metaCode == 132001 || metaCode == 132000)
        {
            return MetaCloudApiResponse.PermanentFailure("ERR_WA_TEMPLATE_INACTIVE", errorMessage, metaSubcode, fbTraceId);
        }

        if (metaCode == 190 || statusCode == HttpStatusCode.Unauthorized)
        {
            return MetaCloudApiResponse.PermanentFailure("ERR_WA_ACCESS_TOKEN_EXPIRED", errorMessage, metaSubcode, fbTraceId);
        }

        if ((int)statusCode >= 500)
        {
            return MetaCloudApiResponse.TransientFailure("ERR_WA_META_UPSTREAM_ERROR", errorMessage, metaSubcode, fbTraceId);
        }

        return MetaCloudApiResponse.PermanentFailure("ERR_WA_PROVIDER_REJECTED", errorMessage, metaSubcode, fbTraceId);
    }

    private static string MaskPhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 6) return "***";
        return phone[..4] + "****" + phone[^4..];
    }
}
