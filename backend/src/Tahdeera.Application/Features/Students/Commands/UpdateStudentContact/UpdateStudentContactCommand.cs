using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Students.Commands.UpdateStudentContact;

public record UpdateStudentContactCommand(
    Guid WorkspaceId,
    Guid StudentId,
    string? FullName,
    string? GuardianName,
    string? GuardianPhone,
    string? StudentPhone,
    bool WhatsappOptIn,
    string? SeatNumber,
    string? LocalNotes,
    bool AllowSiblingPhone = false
) : MediatR.IRequest<Result>, ITransactionalRequest;
