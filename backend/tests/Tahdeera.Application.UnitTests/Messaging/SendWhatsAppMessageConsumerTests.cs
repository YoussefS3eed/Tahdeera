using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tahdeera.Application.Common.Interfaces.Messaging;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Common.Models;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Tahdeera.Infrastructure.Messaging.Consumers;

namespace Tahdeera.Application.UnitTests.Messaging;

public class SendWhatsAppMessageConsumerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppSenderResolver _senderResolver;
    private readonly IMetaWhatsAppCloudApiClient _metaApiClient;
    private readonly IWhatsAppRateCardService _rateCardService;
    private readonly ILogger<SendWhatsAppMessageConsumer> _logger;
    private readonly SendWhatsAppMessageConsumer _consumer;

    private readonly Guid _outboxId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();

    public SendWhatsAppMessageConsumerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _senderResolver = Substitute.For<IWhatsAppSenderResolver>();
        _metaApiClient = Substitute.For<IMetaWhatsAppCloudApiClient>();
        _rateCardService = Substitute.For<IWhatsAppRateCardService>();
        _logger = Substitute.For<ILogger<SendWhatsAppMessageConsumer>>();

        _rateCardService.CalculateCost(Arg.Any<string>()).Returns((0.0356m, 1.75m));

        _consumer = new SendWhatsAppMessageConsumer(
            _unitOfWork,
            _senderResolver,
            _metaApiClient,
            _rateCardService,
            _logger);
    }

    private ConsumeContext<SendWhatsAppMessageCommand> CreateConsumeContext()
    {
        var context = Substitute.For<ConsumeContext<SendWhatsAppMessageCommand>>();
        context.Message.Returns(new SendWhatsAppMessageCommand(
            _outboxId,
            _workspaceId,
            "+201012345678",
            "AttendanceCheckIn",
            "attendance_check_in_v1",
            "ar",
            "{}",
            DateTimeOffset.UtcNow));
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_OutboxNotFound_SkipsDispatch()
    {
        var context = CreateConsumeContext();
        _unitOfWork.MessageOutbox.GetByIdAsync(_outboxId, Arg.Any<CancellationToken>())
            .Returns((MessageOutbox?)null);

        await _consumer.Consume(context);

        await _metaApiClient.DidNotReceiveWithAnyArgs().SendTemplateMessageAsync(default!, default!, default!, default!, default!, default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_FailedPermanentStatus_SkipsDispatch()
    {
        var context = CreateConsumeContext();
        var message = MessageOutbox.CreateForAttendance(_workspaceId, _sessionId, _studentId, "+201012345678", "AttendanceCheckIn", "{}", "key_1");
        message.TransitionToFailedPermanent("PREVIOUS_ERROR", "Previous permanent failure");

        _unitOfWork.MessageOutbox.GetByIdAsync(_outboxId, Arg.Any<CancellationToken>())
            .Returns(message);

        await _consumer.Consume(context);

        await _metaApiClient.DidNotReceiveWithAnyArgs().SendTemplateMessageAsync(default!, default!, default!, default!, default!, default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_MonthlyQuotaExceeded_MarksFailedPermanentAndRecordsFailed()
    {
        var context = CreateConsumeContext();
        var message = MessageOutbox.CreateForAttendance(_workspaceId, _sessionId, _studentId, "+201012345678", "AttendanceCheckIn", "{}", "key_1");

        _unitOfWork.MessageOutbox.GetByIdAsync(_outboxId, Arg.Any<CancellationToken>())
            .Returns(message);

        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged, monthlyQuota: 100);
        for (int i = 0; i < 100; i++)
        {
            config.IncrementUsage();
        }

        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, DateTimeOffset.UtcNow.ToString("yyyy-MM"));
        for (int i = 0; i < 100; i++)
        {
            ledger.RecordDispatch(0, 0);
        }

        _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(_workspaceId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        await _consumer.Consume(context);

        message.Status.Should().Be(OutboxMessageStatus.FailedPermanent);
        message.ErrorCode.Should().Be("WORKSPACE_QUOTA_EXCEEDED");
        ledger.MessagesFailedCount.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _metaApiClient.DidNotReceiveWithAnyArgs().SendTemplateMessageAsync(default!, default!, default!, default!, default!, default!);
    }

    [Fact]
    public async Task Consume_SenderResolutionFails_MarksFailedPermanentWithCONF07()
    {
        var context = CreateConsumeContext();
        var message = MessageOutbox.CreateForAttendance(_workspaceId, _sessionId, _studentId, "+201012345678", "AttendanceCheckIn", "{}", "key_1");

        _unitOfWork.MessageOutbox.GetByIdAsync(_outboxId, Arg.Any<CancellationToken>())
            .Returns(message);

        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.WorkspaceOwned, monthlyQuota: 500);
        config.IncrementUsage();
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var disabledSender = WhatsAppSenderDescriptor.Unavailable(_workspaceId, WhatsAppSenderMode.WorkspaceOwned, "WABA disconnected");
        _senderResolver.ResolveSenderAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(disabledSender);

        await _consumer.Consume(context);

        message.Status.Should().Be(OutboxMessageStatus.FailedPermanent);
        message.ErrorCode.Should().Be("ERR_WA_NO_SENDER_CONFIGURED");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _metaApiClient.DidNotReceiveWithAnyArgs().SendTemplateMessageAsync(default!, default!, default!, default!, default!, default!);
    }

    [Fact]
    public async Task Consume_SuccessfulDispatch_TransitionsToSentAndIncrementsConfig()
    {
        var context = CreateConsumeContext();
        var message = MessageOutbox.CreateForAttendance(_workspaceId, _sessionId, _studentId, "+201012345678", "AttendanceCheckIn", "{}", "key_1");

        _unitOfWork.MessageOutbox.GetByIdAsync(_outboxId, Arg.Any<CancellationToken>())
            .Returns(message);

        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged, monthlyQuota: 500);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, DateTimeOffset.UtcNow.ToString("yyyy-MM"));
        _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(_workspaceId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        var activeSender = WhatsAppSenderDescriptor.Platform(_workspaceId, "phone_123", "token_abc", "+201012345678", "Tahdeera");
        _senderResolver.ResolveSenderAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(activeSender);

        _metaApiClient.SendTemplateMessageAsync(
                activeSender, "+201012345678", "attendance_check_in_v1", "ar", "{}", Arg.Any<CancellationToken>())
            .Returns(MetaCloudApiResponse.Success("wamid.SUCCESS_12345"));

        await _consumer.Consume(context);

        message.Status.Should().Be(OutboxMessageStatus.Sent);
        message.ProviderMessageId.Should().Be("wamid.SUCCESS_12345");
        config.MessagesUsedThisMonth.Should().Be(1);
        ledger.MessagesDispatchedCount.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_TransientFailure_Under5Attempts_SchedulesRetry()
    {
        var context = CreateConsumeContext();
        var message = MessageOutbox.CreateForAttendance(_workspaceId, _sessionId, _studentId, "+201012345678", "AttendanceCheckIn", "{}", "key_1");

        _unitOfWork.MessageOutbox.GetByIdAsync(_outboxId, Arg.Any<CancellationToken>())
            .Returns(message);

        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var activeSender = WhatsAppSenderDescriptor.Platform(_workspaceId, "phone_123", "token_abc", "+201012345678", "Tahdeera");
        _senderResolver.ResolveSenderAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(activeSender);

        _metaApiClient.SendTemplateMessageAsync(
                activeSender, "+201012345678", "attendance_check_in_v1", "ar", "{}", Arg.Any<CancellationToken>())
            .Returns(MetaCloudApiResponse.TransientFailure("ERR_RATE_LIMIT", "Rate limit hit"));

        await _consumer.Consume(context);

        message.Status.Should().Be(OutboxMessageStatus.FailedTransient);
        message.AttemptCount.Should().Be(2); // 1 from RecordAttempt + 1 from ScheduleRetryAt
        message.NextAttemptAt.Should().NotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_TransientFailure_MaxAttemptsReached_MarksFailedPermanent()
    {
        var context = CreateConsumeContext();
        var message = MessageOutbox.CreateForAttendance(_workspaceId, _sessionId, _studentId, "+201012345678", "AttendanceCheckIn", "{}", "key_1");
        message.Attempts = 5; // Max retry count already reached!

        _unitOfWork.MessageOutbox.GetByIdAsync(_outboxId, Arg.Any<CancellationToken>())
            .Returns(message);

        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var activeSender = WhatsAppSenderDescriptor.Platform(_workspaceId, "phone_123", "token_abc", "+201012345678", "Tahdeera");
        _senderResolver.ResolveSenderAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(activeSender);

        _metaApiClient.SendTemplateMessageAsync(
                activeSender, "+201012345678", "attendance_check_in_v1", "ar", "{}", Arg.Any<CancellationToken>())
            .Returns(MetaCloudApiResponse.TransientFailure("ERR_RATE_LIMIT", "Rate limit hit"));

        await _consumer.Consume(context);

        message.Status.Should().Be(OutboxMessageStatus.FailedPermanent);
        message.ErrorCode.Should().Be("ERR_MAX_ATTEMPTS_EXCEEDED");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_PermanentFailure_MarksFailedPermanent()
    {
        var context = CreateConsumeContext();
        var message = MessageOutbox.CreateForAttendance(_workspaceId, _sessionId, _studentId, "+201012345678", "AttendanceCheckIn", "{}", "key_1");

        _unitOfWork.MessageOutbox.GetByIdAsync(_outboxId, Arg.Any<CancellationToken>())
            .Returns(message);

        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var activeSender = WhatsAppSenderDescriptor.Platform(_workspaceId, "phone_123", "token_abc", "+201012345678", "Tahdeera");
        _senderResolver.ResolveSenderAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(activeSender);

        _metaApiClient.SendTemplateMessageAsync(
                activeSender, "+201012345678", "attendance_check_in_v1", "ar", "{}", Arg.Any<CancellationToken>())
            .Returns(MetaCloudApiResponse.PermanentFailure("ERR_WA_RECIPIENT_INELIGIBLE", "Number not on WhatsApp"));

        await _consumer.Consume(context);

        message.Status.Should().Be(OutboxMessageStatus.FailedPermanent);
        message.ErrorCode.Should().Be("ERR_WA_RECIPIENT_INELIGIBLE");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_UnexpectedException_Under5Attempts_SchedulesRetry()
    {
        var context = CreateConsumeContext();
        var message = MessageOutbox.CreateForAttendance(_workspaceId, _sessionId, _studentId, "+201012345678", "AttendanceCheckIn", "{}", "key_1");

        _unitOfWork.MessageOutbox.GetByIdAsync(_outboxId, Arg.Any<CancellationToken>())
            .Returns(message);

        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(config);

        var activeSender = WhatsAppSenderDescriptor.Platform(_workspaceId, "phone_123", "token_abc", "+201012345678", "Tahdeera");
        _senderResolver.ResolveSenderAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(activeSender);

        _metaApiClient.SendTemplateMessageAsync(
                activeSender, "+201012345678", "attendance_check_in_v1", "ar", "{}", Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException("Connection reset by peer"));

        await _consumer.Consume(context);

        message.Status.Should().Be(OutboxMessageStatus.FailedTransient);
        message.ErrorCode.Should().Be("ERR_UNHANDLED_EXCEPTION");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
