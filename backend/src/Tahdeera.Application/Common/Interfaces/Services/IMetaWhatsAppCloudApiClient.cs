namespace Tahdeera.Application.Common.Interfaces.Services;

using Tahdeera.Application.Common.Models;

public interface IMetaWhatsAppCloudApiClient
{
    Task<MetaCloudApiResponse> SendTemplateMessageAsync(
        WhatsAppSenderDescriptor sender,
        string recipientPhoneE164,
        string templateName,
        string languageCode,
        string payloadJson,
        CancellationToken cancellationToken = default);
}
