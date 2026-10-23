using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Net;
using System.Text.Json;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Infrastructure.Services;

namespace Tahdeera.Application.UnitTests.Services;

public class AuditLogServiceTests
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 21, 14, 30, 0, TimeSpan.Zero);

    public AuditLogServiceTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _dateTimeProvider.UtcNow.Returns(_fixedTime);
    }

    [Fact]
    public async Task LogAsync_WithMetadataAndRemoteIp_PersistsAuditLog()
    {
        using var dbContext = TestDbContextFactory.Create();

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.55");
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var service = new AuditLogService(dbContext, _dateTimeProvider, _httpContextAccessor);

        var actorId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var metadata = new { ActionDetail = "Price updated", OldPrice = 100, NewPrice = 150 };

        await service.LogAsync(
            actorId,
            workspaceId,
            "PRICE_CHANGE",
            "Group",
            "grp_123",
            metadata,
            CancellationToken.None);

        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.ActorUserId.Should().Be(actorId);
        log.WorkspaceId.Should().Be(workspaceId);
        log.Action.Should().Be("PRICE_CHANGE");
        log.EntityType.Should().Be("Group");
        log.EntityId.Should().Be("grp_123");
        log.OccurredAt.Should().Be(_fixedTime);
        log.IpAddress.Should().Be("192.168.1.55");

        var doc = JsonDocument.Parse(log.Metadata);
        doc.RootElement.GetProperty("actionDetail").GetString().Should().Be("Price updated");
        doc.RootElement.GetProperty("newPrice").GetInt32().Should().Be(150);
    }

    [Fact]
    public async Task LogAsync_WithNullMetadata_PersistsDefaultEmptyJsonObject()
    {
        using var dbContext = TestDbContextFactory.Create();
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var service = new AuditLogService(dbContext, _dateTimeProvider, _httpContextAccessor);

        await service.LogAsync(
            null,
            null,
            "SYSTEM_STARTUP",
            "System",
            null,
            null,
            CancellationToken.None);

        var log = await dbContext.AuditLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Metadata.Should().Be("{}");
        log.IpAddress.Should().BeNull();
    }
}
