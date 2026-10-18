using MediatR;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string UsernameOrEmailOrPhone,
    string Password) : IRequest<Result<AuthResultDto>>;
