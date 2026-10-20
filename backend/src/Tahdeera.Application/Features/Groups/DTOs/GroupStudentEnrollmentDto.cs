namespace Tahdeera.Application.Features.Groups.DTOs;

public sealed record GroupStudentEnrollmentDto(
    Guid GroupStudentId,
    Guid GroupId,
    Guid StudentId,
    string StudentName,
    string StudentCode,
    string Status,
    DateTimeOffset JoinedAt);
