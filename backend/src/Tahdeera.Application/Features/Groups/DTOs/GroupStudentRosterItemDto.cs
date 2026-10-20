namespace Tahdeera.Application.Features.Groups.DTOs;

public sealed record GroupStudentRosterItemDto(
    Guid GroupStudentId,
    Guid StudentId,
    string StudentName,
    string StudentCode,
    string GuardianPhone,
    string Status,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LeftAt,
    int TotalAttendedSessions,
    int TotalAbsentSessions);
