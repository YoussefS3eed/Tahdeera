using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;
using System.Text.Json;
using Tahdeera.Application.Common.Interfaces.Messaging;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Common.Models;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Tahdeera.Infrastructure.Configuration;
using Tahdeera.Infrastructure.Messaging.Consumers;
using Tahdeera.Infrastructure.Services;

namespace Tahdeera.Application.UnitTests.Messaging;

public class WhatsAppPipelineUnitTests
{
    [Fact]
    public void AttendanceCheckInPayloadDto_Serializes_Correctly_In_Arabic_And_English()
    {
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // Arabic Check-In
        var arDto = new AttendanceCheckInPayloadDto("أحمد علي", "حاضر", "مجموعة أ", "04:30 م");
        var arJson = JsonSerializer.Serialize(arDto, options);
        arJson.Should().Contain("أحمد علي");
        arJson.Should().Contain("حاضر");

        // English Late
        var enDto = new AttendanceCheckInPayloadDto("John Doe", "Late", "Group B", "04:30 PM");
        var enJson = JsonSerializer.Serialize(enDto, options);
        enJson.Should().Contain("John Doe");
        enJson.Should().Contain("Late");
    }

    [Fact]
    public void AbsenceAlertPayloadDto_Serializes_Correctly()
    {
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var dto = new AbsenceAlertPayloadDto("سارة محمود", "مجموعة الفيزياء", "2026-10-21");
        var json = JsonSerializer.Serialize(dto, options);
        json.Should().Contain("سارة محمود");
        json.Should().Contain("مجموعة الفيزياء");
        json.Should().Contain("2026-10-21");
    }

    [Fact]
    public async Task WhatsAppSenderResolver_CacheHit_DoesNotQueryDatabaseTwice()
    {
        var workspaceId = Guid.NewGuid();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var dataProtection = Substitute.For<IDataProtectionProvider>();
        var platformOptions = Options.Create(new PlatformWhatsAppSettings { IsEnabled = false });
        var logger = Substitute.For<ILogger<WhatsAppSenderResolver>>();

        var config = WorkspaceWhatsAppConfig.Create(workspaceId, WhatsAppSenderMode.Disabled);
        unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var resolver = new WhatsAppSenderResolver(unitOfWork, memoryCache, dataProtection, platformOptions, logger);

        // First call - cache miss
        var descriptor1 = await resolver.ResolveSenderAsync(workspaceId);
        descriptor1.Should().NotBeNull();
        descriptor1!.CanSend.Should().BeFalse();

        // Second call - cache hit
        var descriptor2 = await resolver.ResolveSenderAsync(workspaceId);
        descriptor2.Should().NotBeNull();
        descriptor2!.CanSend.Should().BeFalse();

        // Database should only be queried once
        await unitOfWork.WorkspaceWhatsAppConfigs.Received(1)
            .GetByWorkspaceIdAsync(workspaceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhatsAppSenderResolver_InvalidateCache_ForcesDatabaseQuery()
    {
        var workspaceId = Guid.NewGuid();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var dataProtection = Substitute.For<IDataProtectionProvider>();
        var platformOptions = Options.Create(new PlatformWhatsAppSettings { IsEnabled = false });
        var logger = Substitute.For<ILogger<WhatsAppSenderResolver>>();

        var config = WorkspaceWhatsAppConfig.Create(workspaceId, WhatsAppSenderMode.Disabled);
        unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var resolver = new WhatsAppSenderResolver(unitOfWork, memoryCache, dataProtection, platformOptions, logger);

        await resolver.ResolveSenderAsync(workspaceId);
        resolver.InvalidateSenderCache(workspaceId);
        await resolver.ResolveSenderAsync(workspaceId);

        // Database should be queried twice because cache was evicted
        await unitOfWork.WorkspaceWhatsAppConfigs.Received(2)
            .GetByWorkspaceIdAsync(workspaceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WhatsAppSenderResolver_EnforcesZeroSilentFallback_WhenActiveWabaMissing()
    {
        var workspaceId = Guid.NewGuid();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var dataProtection = Substitute.For<IDataProtectionProvider>();
        var platformOptions = Options.Create(new PlatformWhatsAppSettings
        {
            IsEnabled = true,
            PhoneNumberId = "platform_phone_999",
            AccessToken = "platform_token",
            BusinessDisplayName = "Tahdeera"
        });
        var logger = Substitute.For<ILogger<WhatsAppSenderResolver>>();

        // Workspace configured for WorkspaceOwned
        var connId = Guid.NewGuid();
        var config = WorkspaceWhatsAppConfig.Create(workspaceId, WhatsAppSenderMode.WorkspaceOwned);
        config.UpdateSenderMode(WhatsAppSenderMode.WorkspaceOwned, connId);

        unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        // No active connection found
        unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns((WhatsAppSenderConnection?)null);

        var resolver = new WhatsAppSenderResolver(unitOfWork, memoryCache, dataProtection, platformOptions, logger);
        var descriptor = await resolver.ResolveSenderAsync(workspaceId);

        // MUST NOT fall back to platform sender
        descriptor.Should().NotBeNull();
        descriptor!.CanSend.Should().BeFalse();
        descriptor.SenderMode.Should().Be(WhatsAppSenderMode.WorkspaceOwned);
        descriptor.PhoneNumberId.Should().BeEmpty();
        descriptor.AccessToken.Should().BeEmpty();
        descriptor.RejectionReason.Should().Contain("Workspace has no active, connected WhatsApp Business Account.");
    }

    [Fact]
    public async Task MetaWhatsAppCloudApiClient_Success_ParsesMessageId()
    {
        var jsonResponse = """
        {
          "messaging_product": "whatsapp",
          "contacts": [{ "input": "+201012345678", "wa_id": "201012345678" }],
          "messages": [{ "id": "wamid.HBgLMTIzNDU2Nzg5FQIAERgSRjQ2QjgzQkM2Qzg0RjAxNkM2AA==" }]
        }
        """;

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, jsonResponse);
        var client = new HttpClient(handler);
        var logger = Substitute.For<ILogger<MetaWhatsAppCloudApiClient>>();
        var apiClient = new MetaWhatsAppCloudApiClient(client, logger);

        var sender = WhatsAppSenderDescriptor.Platform(Guid.NewGuid(), "phone_123", "token_abc", "Tahdeera", "Tahdeera");
        var result = await apiClient.SendTemplateMessageAsync(sender, "+201012345678", "attendance_check_in_v1", "ar", "{}");

        result.IsSuccess.Should().BeTrue();
        result.ProviderMessageId.Should().Be("wamid.HBgLMTIzNDU2Nzg5FQIAERgSRjQ2QjgzQkM2Qzg0RjAxNkM2AA==");
    }

    [Fact]
    public async Task MetaWhatsAppCloudApiClient_RateLimit_ReturnsTransientFailure()
    {
        var jsonResponse = """
        {
          "error": {
            "message": "(#130429) Rate limit hit",
            "type": "OAuthException",
            "code": 130429,
            "fbtrace_id": "Abc123Trace"
          }
        }
        """;

        var handler = new MockHttpMessageHandler((HttpStatusCode)429, jsonResponse);
        var client = new HttpClient(handler);
        var logger = Substitute.For<ILogger<MetaWhatsAppCloudApiClient>>();
        var apiClient = new MetaWhatsAppCloudApiClient(client, logger);

        var sender = WhatsAppSenderDescriptor.Platform(Guid.NewGuid(), "phone_123", "token_abc", "Tahdeera", "Tahdeera");
        var result = await apiClient.SendTemplateMessageAsync(sender, "+201012345678", "attendance_check_in_v1", "ar", "{}");

        result.IsSuccess.Should().BeFalse();
        result.IsTransient.Should().BeTrue();
        result.ErrorCode.Should().Be("ERR_WA_RATE_LIMIT_EXCEEDED");
    }

    [Fact]
    public async Task MetaWhatsAppCloudApiClient_RecipientIneligible_ReturnsPermanentFailure()
    {
        var jsonResponse = """
        {
          "error": {
            "message": "(#131026) Receiver does not have a WhatsApp account.",
            "type": "OAuthException",
            "code": 131026,
            "fbtrace_id": "TraceXyz"
          }
        }
        """;

        var handler = new MockHttpMessageHandler(HttpStatusCode.BadRequest, jsonResponse);
        var client = new HttpClient(handler);
        var logger = Substitute.For<ILogger<MetaWhatsAppCloudApiClient>>();
        var apiClient = new MetaWhatsAppCloudApiClient(client, logger);

        var sender = WhatsAppSenderDescriptor.Platform(Guid.NewGuid(), "phone_123", "token_abc", "Tahdeera", "Tahdeera");
        var result = await apiClient.SendTemplateMessageAsync(sender, "+201012345678", "attendance_check_in_v1", "ar", "{}");

        result.IsSuccess.Should().BeFalse();
        result.IsTransient.Should().BeFalse();
        result.ErrorCode.Should().Be("ERR_WA_RECIPIENT_INELIGIBLE");
    }

    [Fact]
    public async Task SendWhatsAppMessageConsumer_IdempotencyGate_SkipsAlreadySentMessage()
    {
        var outboxId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var senderResolver = Substitute.For<IWhatsAppSenderResolver>();
        var metaApiClient = Substitute.For<IMetaWhatsAppCloudApiClient>();
        var logger = Substitute.For<ILogger<SendWhatsAppMessageConsumer>>();

        var message = MessageOutbox.CreateForAttendance(workspaceId, Guid.NewGuid(), Guid.NewGuid(), "+201012345678", "AttendanceCheckIn", "{}", "key_1");
        message.TransitionToSent("wamid.already_sent", 0.0356m, 1.35m, DateTimeOffset.UtcNow);

        unitOfWork.MessageOutbox.GetByIdAsync(outboxId, Arg.Any<CancellationToken>())
            .Returns(message);

        var consumer = new SendWhatsAppMessageConsumer(unitOfWork, senderResolver, metaApiClient, logger);
        var context = Substitute.For<ConsumeContext<SendWhatsAppMessageCommand>>();
        context.Message.Returns(new SendWhatsAppMessageCommand(outboxId, workspaceId, "+201012345678", "AttendanceCheckIn", "attendance_check_in_v1", "ar", "{}", DateTimeOffset.UtcNow));

        await consumer.Consume(context);

        // Meta API Client should NEVER be called
        await metaApiClient.DidNotReceiveWithAnyArgs().SendTemplateMessageAsync(default!, default!, default!, default!, default!, default!);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseContent;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
