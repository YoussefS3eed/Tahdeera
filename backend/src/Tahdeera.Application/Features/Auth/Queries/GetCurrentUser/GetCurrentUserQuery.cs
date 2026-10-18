using MediatR;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<Result<CurrentUserDto>>;
