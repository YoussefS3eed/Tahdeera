using MediatR;
using System.Text.RegularExpressions;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;
using DomainRefreshToken = Tahdeera.Domain.Entities.RefreshToken;

namespace Tahdeera.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var trimmedInput = request.UsernameOrEmailOrPhone.Trim();

        // Egyptian phone number normalization: 01... (11 digits) -> +201...
        string? normalizedPhone = null;
        if (Regex.IsMatch(trimmedInput, @"^01[0-2,5]\d{8}$"))
        {
            normalizedPhone = "+20" + trimmedInput[1..];
        }

        var normalizedEmail = trimmedInput.ToUpperInvariant();

        // Find user by normalized email, username, or normalized phone
        var user = await _unitOfWork.AppUsers.FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalizedEmail ||
                 u.Username == trimmedInput ||
                 (normalizedPhone != null && u.Phone == normalizedPhone),
            cancellationToken);

        if (user == null)
        {
            // Execute dummy password check to prevent timing analysis enumeration
            await _identityService.CheckPasswordAsync(trimmedInput, request.Password, cancellationToken);
            return Error.Unauthorized("AUTH_INVALID_CREDENTIALS", "اسم المستخدم أو كلمة المرور غير صحيحة.");
        }

        if (!user.IsActive)
        {
            return Error.Unauthorized("AUTH_USER_DEACTIVATED", "تم تعطيل حساب المستخدم. يرجى التواصل مع الإدارة.");
        }

        // Verify password against ASP.NET Identity
        var (passwordValid, _) = await _identityService.CheckPasswordAsync(user.Username, request.Password, cancellationToken);
        if (!passwordValid)
        {
            return Error.Unauthorized("AUTH_INVALID_CREDENTIALS", "اسم المستخدم أو كلمة المرور غير صحيحة.");
        }

        // Generate token pair
        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenPlaintext = _jwtTokenService.GenerateRefreshToken();
        var tokenHash = _jwtTokenService.HashToken(refreshTokenPlaintext);

        var refreshTokenEntity = DomainRefreshToken.Create(
            user.Id,
            tokenHash,
            _dateTimeProvider.UtcNow.AddDays(7));

        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

        user.RecordLogin(_dateTimeProvider.UtcNow);
        _unitOfWork.AppUsers.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Fetch user workspaces with subscription status
        var memberships = await _unitOfWork.WorkspaceMembers.FindAsync(
            m => m.UserId == user.Id && m.IsActive,
            cancellationToken);

        var workspaceDtos = new List<UserWorkspaceMembershipDto>();
        foreach (var membership in memberships)
        {
            var workspace = await _unitOfWork.Workspaces.GetByIdAsync(membership.WorkspaceId, cancellationToken);
            if (workspace == null) continue;

            var subscriptions = await _unitOfWork.WorkspaceSubscriptions.FindAsync(
                s => s.WorkspaceId == workspace.Id,
                cancellationToken);

            var latestSub = subscriptions.OrderByDescending(s => s.EndsAt).FirstOrDefault();
            WorkspaceSubscriptionSummaryDto? subSummary = null;
            if (latestSub != null)
            {
                var isLockedOut = latestSub.ManualSuspendedAt != null || latestSub.EndsAt < _dateTimeProvider.UtcNow;
                subSummary = new WorkspaceSubscriptionSummaryDto(
                    latestSub.Status,
                    latestSub.StartsAt,
                    latestSub.EndsAt,
                    isLockedOut);
            }

            workspaceDtos.Add(new UserWorkspaceMembershipDto(
                workspace.Id,
                workspace.Name,
                workspace.Kind,
                membership.Role,
                subSummary));
        }

        var userDto = new UserProfileDto(
            user.Id,
            user.Username,
            user.NormalizedEmail,
            user.DisplayName,
            user.Phone,
            user.PreferredLanguage,
            user.PreferredTheme,
            user.IsPlatformAdmin);

        return Result.Success(new AuthResultDto(
            accessToken,
            expiresAt,
            refreshTokenPlaintext,
            userDto,
            workspaceDtos));
    }
}
