using MediatR;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Features.Students.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Students.Commands.LinkStudentByCard;

public record LinkStudentByCardCommand(
    Guid WorkspaceId,
    string ScannedToken,
    short? GradeId,
    string? GuardianName,
    string GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    string? LocalNotes,
    bool AllowSiblingPhone = false
) : IRequest<Result<StudentEnrollmentDto>>, ITransactionalRequest;
