using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Students.Commands.RegisterStudent;

public record RegisterStudentCommand(
    Guid WorkspaceId,
    string FullName,
    short GradeId,
    string? GuardianName,
    string GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    string? LocalNotes,
    string? CardToken,
    string? CardUidDisplay,
    bool AllowSiblingPhone = false
) : IRequest<Result<StudentEnrollmentDto>>, ITransactionalRequest;
