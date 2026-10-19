using Mapster;
using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.Users.Commands.UpdatePreferences;

public class UpdatePreferencesCommandHandler : IRequestHandler<UpdatePreferencesCommand, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdatePreferencesCommandHandler(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<UserProfileDto>> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.AppUsers.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            return Error.NotFound("USER_NOT_FOUND", "User not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.PreferredLanguage))
        {
            user.PreferredLanguage = request.PreferredLanguage.Trim().ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(request.PreferredTheme))
        {
            user.PreferredTheme = request.PreferredTheme.Trim().ToLowerInvariant();
        }

        user.UpdatedAt = _dateTimeProvider.UtcNow;
        _unitOfWork.AppUsers.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(user.Adapt<UserProfileDto>());
    }
}
