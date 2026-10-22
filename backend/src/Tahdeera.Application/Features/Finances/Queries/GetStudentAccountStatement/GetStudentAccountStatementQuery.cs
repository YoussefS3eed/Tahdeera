using MediatR;
using Tahdeera.Application.Features.Finances.DTOs;

namespace Tahdeera.Application.Features.Finances.Queries.GetStudentAccountStatement;

public record GetStudentAccountStatementQuery(
    Guid WorkspaceId,
    Guid StudentId,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate) : IRequest<StudentAccountStatementDto>;
