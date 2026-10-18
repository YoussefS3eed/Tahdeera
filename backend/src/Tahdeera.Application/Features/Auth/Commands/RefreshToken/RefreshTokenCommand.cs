using MediatR;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthTokenDto>>;
