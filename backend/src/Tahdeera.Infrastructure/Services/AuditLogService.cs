using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AuditLogService(
        ITahdeeraDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        Guid? actorUserId,
        Guid? workspaceId,
        string action,
        string entityType,
        string? entityId,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var jsonMetadata = metadata != null ? JsonSerializer.Serialize(metadata, JsonOptions) : "{}";

        var log = new AuditLog
        {
            ActorUserId = actorUserId,
            WorkspaceId = workspaceId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OccurredAt = _dateTimeProvider.UtcNow,
            IpAddress = ipAddress,
            Metadata = jsonMetadata
        };

        await _dbContext.AuditLogs.AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
