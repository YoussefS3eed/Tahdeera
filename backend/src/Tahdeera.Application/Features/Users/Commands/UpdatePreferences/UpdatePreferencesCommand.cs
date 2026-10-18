using MediatR;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.Features.Users.Commands.UpdatePreferences;

public record UpdatePreferencesCommand(
    Guid UserId,
    string? PreferredLanguage,
    string? PreferredTheme) : IRequest<Result<UserProfileDto>>;
