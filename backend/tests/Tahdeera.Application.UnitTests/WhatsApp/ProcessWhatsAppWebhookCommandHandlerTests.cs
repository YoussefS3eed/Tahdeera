using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tahdeera.Application.Common.Models.Webhooks;
using Tahdeera.Application.Features.WhatsApp.Commands.ProcessWhatsAppWebhook;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Xunit;

namespace Tahdeera.Application.UnitTests.WhatsApp;

public sealed class ProcessWhatsAppWebhookCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessWhatsAppWebhookCommandHandler> _logger;
    private readonly ProcessWhatsAppWebhookCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public ProcessWhatsAppWebhookCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<ProcessWhatsAppWebhookCommandHandler>>();
        _handler = new ProcessWhatsAppWebhookCommandHandler(_unitOfWork, _logger);
    }

    [Fact]
    public async Task Handle_DeliveredStatus_TransitionsOutboxAndIncrementsDeliveredCount()
    {
        var outbox = MessageOutbox.Create(
            _workspaceId,
            "+201012345678",
            "AttendanceCheckIn",
            "attendance_check_in_v1",
            "ar",
            "{}",
            "idem_key_1");
        outbox.TransitionToSent("wamid.test_123", 1.35m);

        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, DateTimeOffset.UtcNow.ToString("yyyy-MM"));

        _unitOfWork.MessageOutbox.GetByProviderMessageIdAsync("wamid.test_123", Arg.Any<CancellationToken>())
            .Returns(outbox);
        _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(_workspaceId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        var payload = CreatePayload("wamid.test_123", "delivered", "1729500000");

        await _handler.Handle(new ProcessWhatsAppWebhookCommand(payload), CancellationToken.None);

        outbox.Status.Should().Be(OutboxMessageStatus.Delivered);
        outbox.DeliveredAt.Should().NotBeNull();
        ledger.MessagesDeliveredCount.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateDeliveredStatus_IsStrictIdempotentNoOp()
    {
        var outbox = MessageOutbox.Create(
            _workspaceId,
            "+201012345678",
            "AttendanceCheckIn",
            "attendance_check_in_v1",
            "ar",
            "{}",
            "idem_key_2");
        var deliveredTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        outbox.TransitionToSent("wamid.test_dup", 1.35m);
        outbox.TransitionToDelivered(deliveredTime);

        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, DateTimeOffset.UtcNow.ToString("yyyy-MM"));

        _unitOfWork.MessageOutbox.GetByProviderMessageIdAsync("wamid.test_dup", Arg.Any<CancellationToken>())
            .Returns(outbox);
        _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(_workspaceId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        var payload = CreatePayload("wamid.test_dup", "delivered", "1729500300");

        await _handler.Handle(new ProcessWhatsAppWebhookCommand(payload), CancellationToken.None);

        outbox.Status.Should().Be(OutboxMessageStatus.Delivered);
        outbox.DeliveredAt.Should().Be(deliveredTime); // Unchanged!
        ledger.MessagesDeliveredCount.Should().Be(0);   // Not re-incremented!
    }

    [Fact]
    public async Task Handle_ReadStatus_RecordsMetadataWithoutModifyingStatusOrBilling()
    {
        var outbox = MessageOutbox.Create(
            _workspaceId,
            "+201012345678",
            "AttendanceCheckIn",
            "attendance_check_in_v1",
            "ar",
            "{\"foo\":\"bar\"}",
            "idem_key_read");
        outbox.TransitionToSent("wamid.test_read", 1.35m);

        _unitOfWork.MessageOutbox.GetByProviderMessageIdAsync("wamid.test_read", Arg.Any<CancellationToken>())
            .Returns(outbox);

        var payload = CreatePayload("wamid.test_read", "read", "1729500400");

        await _handler.Handle(new ProcessWhatsAppWebhookCommand(payload), CancellationToken.None);

        outbox.Status.Should().Be(OutboxMessageStatus.Sent); // Status unchanged!
        outbox.Payload.Should().Contain("meta_read_at");
        await _unitOfWork.WorkspaceWhatsAppUsageLedgers.DidNotReceiveWithAnyArgs()
            .GetOrCreateCurrentMonthAsync(default, default!, default);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FailedStatus_TransitionsToFailedPermanentAndIncrementsLedger()
    {
        var outbox = MessageOutbox.Create(
            _workspaceId,
            "+201012345678",
            "AttendanceCheckIn",
            "attendance_check_in_v1",
            "ar",
            "{}",
            "idem_key_fail");
        outbox.TransitionToSent("wamid.test_fail", 1.35m);

        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, DateTimeOffset.UtcNow.ToString("yyyy-MM"));

        _unitOfWork.MessageOutbox.GetByProviderMessageIdAsync("wamid.test_fail", Arg.Any<CancellationToken>())
            .Returns(outbox);
        _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(_workspaceId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ledger);

        var payload = new MetaWebhookPayload
        {
            Object = "whatsapp_business_account",
            Entry =
            [
                new MetaWebhookEntry
                {
                    Id = "entry_1",
                    Changes =
                    [
                        new MetaWebhookChange
                        {
                            Field = "messages",
                            Value = new MetaWebhookValue
                            {
                                Statuses =
                                [
                                    new MetaWebhookStatus
                                    {
                                        Id = "wamid.test_fail",
                                        Status = "failed",
                                        Timestamp = "1729500500",
                                        RecipientId = "201012345678",
                                        Errors =
                                        [
                                            new MetaWebhookError
                                            {
                                                Code = 131026,
                                                Title = "Message Undeliverable",
                                                Message = "Receiver is not on WhatsApp"
                                            }
                                        ]
                                    }
                                ]
                            }
                        }
                    ]
                }
            ]
        };

        await _handler.Handle(new ProcessWhatsAppWebhookCommand(payload), CancellationToken.None);

        outbox.Status.Should().Be(OutboxMessageStatus.FailedPermanent);
        outbox.ErrorCode.Should().Be("ERR_META_131026");
        ledger.MessagesFailedCount.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownWamid_GracefulNoOpWithoutSavingChanges()
    {
        _unitOfWork.MessageOutbox.GetByProviderMessageIdAsync("wamid.unknown_999", Arg.Any<CancellationToken>())
            .Returns((MessageOutbox?)null);

        var payload = CreatePayload("wamid.unknown_999", "delivered", "1729500600");

        await _handler.Handle(new ProcessWhatsAppWebhookCommand(payload), CancellationToken.None);

        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static MetaWebhookPayload CreatePayload(string wamid, string status, string timestamp)
    {
        return new MetaWebhookPayload
        {
            Object = "whatsapp_business_account",
            Entry =
            [
                new MetaWebhookEntry
                {
                    Id = "entry_1",
                    Changes =
                    [
                        new MetaWebhookChange
                        {
                            Field = "messages",
                            Value = new MetaWebhookValue
                            {
                                Statuses =
                                [
                                    new MetaWebhookStatus
                                    {
                                        Id = wamid,
                                        Status = status,
                                        Timestamp = timestamp,
                                        RecipientId = "201012345678"
                                    }
                                ]
                            }
                        }
                    ]
                }
            ]
        };
    }
}
