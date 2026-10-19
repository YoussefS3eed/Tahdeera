using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Students.Commands.LinkStudentByCode;

public record LinkStudentByCodeCommand(
    Guid WorkspaceId,
    string StudentCode,
    string VerificationFactor,
    short? GradeId,
    string? GuardianName,
    string GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    string? LocalNotes,
    bool AllowSiblingPhone = false
) : IRequest<Result<StudentEnrollmentDto>>, ITransactionalRequest;
