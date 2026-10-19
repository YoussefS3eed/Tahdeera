namespace Tahdeera.Application.Features.Students.DTOs;

public record StudentGroupMembershipDto(
    Guid GroupId,
    string GroupName,
    string SubjectName,
    string TeacherName,
    string Status
);
