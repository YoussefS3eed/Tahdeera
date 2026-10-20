using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.BackgroundJobs;
using Tahdeera.Infrastructure.Persistence;

namespace Tahdeera.Application.UnitTests.Features.Attendance;

public class MessageOutboxRelayWorkerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _attendanceRecordId = Guid.NewGuid();

    private MessageOutbox CreateSampleMessage(string idempotencyKey = "key_1", OutboxMessageStatus status = OutboxMessageStatus.Pending)
    {
        var msg = MessageOutbox.CreateForAttendance(
            _workspaceId,
            _studentId,
            _attendanceRecordId,
            "+201012345678",
            "AttendanceCheckIn",
            "{\"studentName\":\"Ali\"}",
            idempotencyKey);

        msg.Status = status;
        return msg;
    }

    [Fact]
    public async Task ProcessOutboxBatchAsync_WhenPendingMessagesExist_ClaimsAndPublishesSuccessfully()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using (var setupContext = TestDbContextFactory.Create(dbName))
        {
            var msg = CreateSampleMessage("test_key_1", OutboxMessageStatus.Pending);
            setupContext.MessageOutboxes.Add(msg);
            await setupContext.SaveChangesAsync();
        }

        var baseTime = DateTimeOffset.UtcNow;
        var mockPublisher = Substitute.For<IMessageOutboxPublisher>();
        mockPublisher.PublishAsync(Arg.Any<MessageOutbox>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(OutboxPublishResult.Succeeded("broker_msg_123")));

        var services = new ServiceCollection();
        services.AddScoped(_ => TestDbContextFactory.Create(dbName));
        services.AddScoped(_ => mockPublisher);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new OutboxWorkerOptions
        {
            PollingIntervalMilliseconds = 1000,
            BatchSize = 10,
            LeaseTimeoutMinutes = 5
        });

        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var logger = Substitute.For<ILogger<MessageOutboxRelayWorker>>();

        var worker = new MessageOutboxRelayWorker(scopeFactory, options, timeProvider, logger);

        // Act
        var processedCount = await worker.ProcessOutboxBatchAsync(CancellationToken.None);

        // Assert
        processedCount.Should().Be(1);

        using var verifyContext = TestDbContextFactory.Create(dbName);
        var msgInDb = await verifyContext.MessageOutboxes.FirstAsync();
        msgInDb.Status.Should().Be(OutboxMessageStatus.PublishedToBroker);
        msgInDb.BrokerMessageId.Should().Be("broker_msg_123");
        msgInDb.Attempts.Should().Be(1);
        msgInDb.LastAttemptAt.Should().Be(baseTime);
    }

    [Fact]
    public async Task ProcessOutboxBatchAsync_WhenPublisherFails_SchedulesRetryWithBackoff()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using (var setupContext = TestDbContextFactory.Create(dbName))
        {
            var msg = CreateSampleMessage("test_fail_1", OutboxMessageStatus.Pending);
            setupContext.MessageOutboxes.Add(msg);
            await setupContext.SaveChangesAsync();
        }

        var baseTime = DateTimeOffset.UtcNow;
        var mockPublisher = Substitute.For<IMessageOutboxPublisher>();
        mockPublisher.PublishAsync(Arg.Any<MessageOutbox>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(OutboxPublishResult.Failed("BROKER_DOWN", "Connection to RabbitMQ refused")));

        var services = new ServiceCollection();
        services.AddScoped(_ => TestDbContextFactory.Create(dbName));
        services.AddScoped(_ => mockPublisher);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new OutboxWorkerOptions
        {
            PollingIntervalMilliseconds = 1000,
            BatchSize = 10,
            LeaseTimeoutMinutes = 5
        });

        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var logger = Substitute.For<ILogger<MessageOutboxRelayWorker>>();

        var worker = new MessageOutboxRelayWorker(scopeFactory, options, timeProvider, logger);

        // Act
        var processedCount = await worker.ProcessOutboxBatchAsync(CancellationToken.None);

        // Assert
        processedCount.Should().Be(1);

        using var verifyContext = TestDbContextFactory.Create(dbName);
        var msgInDb = await verifyContext.MessageOutboxes.FirstAsync();
        msgInDb.Status.Should().Be(OutboxMessageStatus.FailedTransient);
        msgInDb.FailureCategory.Should().Be("Transient");
        msgInDb.ErrorCode.Should().Be("BROKER_DOWN");
        msgInDb.Attempts.Should().Be(1);
        msgInDb.NextAttemptAt.Should().NotBeNull();
        msgInDb.NextAttemptAt.Should().BeAfter(baseTime);
    }

    [Fact]
    public async Task ProcessOutboxBatchAsync_WhenNextAttemptAtIsInFuture_SkipsMessage()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var baseTime = DateTimeOffset.UtcNow;

        using (var setupContext = TestDbContextFactory.Create(dbName))
        {
            var msg = CreateSampleMessage("test_future_1", OutboxMessageStatus.FailedTransient);
            msg.NextAttemptAt = baseTime.AddMinutes(5); // In future
            setupContext.MessageOutboxes.Add(msg);
            await setupContext.SaveChangesAsync();
        }

        var mockPublisher = Substitute.For<IMessageOutboxPublisher>();

        var services = new ServiceCollection();
        services.AddScoped(_ => TestDbContextFactory.Create(dbName));
        services.AddScoped(_ => mockPublisher);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new OutboxWorkerOptions
        {
            PollingIntervalMilliseconds = 1000,
            BatchSize = 10,
            LeaseTimeoutMinutes = 5
        });

        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var logger = Substitute.For<ILogger<MessageOutboxRelayWorker>>();

        var worker = new MessageOutboxRelayWorker(scopeFactory, options, timeProvider, logger);

        // Act
        var processedCount = await worker.ProcessOutboxBatchAsync(CancellationToken.None);

        // Assert
        processedCount.Should().Be(0);
        await mockPublisher.DidNotReceive().PublishAsync(Arg.Any<MessageOutbox>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecoverStaleLeasesAsync_WhenLeaseExpired_ReleasesLeaseToFailedTransient()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var baseTime = DateTimeOffset.UtcNow;

        using (var setupContext = TestDbContextFactory.Create(dbName))
        {
            var msg = CreateSampleMessage("test_stale_1", OutboxMessageStatus.Processing);
            msg.Attempts = 1;
            msg.LastAttemptAt = baseTime.AddMinutes(-10); // Expired (past 5 min)
            setupContext.MessageOutboxes.Add(msg);
            await setupContext.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddScoped(_ => TestDbContextFactory.Create(dbName));
        services.AddScoped(_ => Substitute.For<IMessageOutboxPublisher>());
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new OutboxWorkerOptions
        {
            PollingIntervalMilliseconds = 1000,
            BatchSize = 10,
            LeaseTimeoutMinutes = 5
        });

        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var logger = Substitute.For<ILogger<MessageOutboxRelayWorker>>();

        var worker = new MessageOutboxRelayWorker(scopeFactory, options, timeProvider, logger);

        // Act
        var recoveredCount = await worker.RecoverStaleLeasesAsync(CancellationToken.None);

        // Assert
        recoveredCount.Should().Be(1);

        using var verifyContext = TestDbContextFactory.Create(dbName);
        var msgInDb = await verifyContext.MessageOutboxes.FirstAsync();
        msgInDb.Status.Should().Be(OutboxMessageStatus.FailedTransient);
        msgInDb.ErrorCode.Should().Be("LEASE_TIMEOUT");
        msgInDb.NextAttemptAt.Should().Be(baseTime); // Available for immediate retry
    }

    [Fact]
    public async Task RecoverStaleLeasesAsync_WhenMaxAttemptsExhausted_TransitionsToFailedPermanent()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var baseTime = DateTimeOffset.UtcNow;

        using (var setupContext = TestDbContextFactory.Create(dbName))
        {
            var msg = CreateSampleMessage("test_stale_max_1", OutboxMessageStatus.Processing);
            msg.Attempts = 5;
            msg.MaxAttempts = 5;
            msg.LastAttemptAt = baseTime.AddMinutes(-15);
            setupContext.MessageOutboxes.Add(msg);
            await setupContext.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddScoped(_ => TestDbContextFactory.Create(dbName));
        services.AddScoped(_ => Substitute.For<IMessageOutboxPublisher>());
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var options = Options.Create(new OutboxWorkerOptions
        {
            PollingIntervalMilliseconds = 1000,
            BatchSize = 10,
            LeaseTimeoutMinutes = 5
        });

        var timeProvider = Substitute.For<IDateTimeProvider>();
        timeProvider.UtcNow.Returns(baseTime);
        var logger = Substitute.For<ILogger<MessageOutboxRelayWorker>>();

        var worker = new MessageOutboxRelayWorker(scopeFactory, options, timeProvider, logger);

        // Act
        var recoveredCount = await worker.RecoverStaleLeasesAsync(CancellationToken.None);

        // Assert
        recoveredCount.Should().Be(1);

        using var verifyContext = TestDbContextFactory.Create(dbName);
        var msgInDb = await verifyContext.MessageOutboxes.FirstAsync();
        msgInDb.Status.Should().Be(OutboxMessageStatus.FailedPermanent);
        msgInDb.ErrorCode.Should().Be("LEASE_EXHAUSTED");
        msgInDb.FailureCategory.Should().Be("Permanent");
    }
}
