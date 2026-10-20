namespace Tahdeera.Application.Features.Groups.DTOs;

public sealed record GroupDelegationDto(
    Guid Id,
    Guid GroupId,
    Guid AssistantUserId,
    string AssistantName,
    DateTimeOffset AssignedAt,
    DateTimeOffset? RevokedAt);
