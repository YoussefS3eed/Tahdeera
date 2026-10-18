using MediatR;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Auth.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Repositories;
using DomainRefreshToken = Tahdeera.Domain.Entities.RefreshToken;

namespace Tahdeera.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokenDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditLogService _auditLogService;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
        _auditLogService = auditLogService;
    }

    public async Task<Result<AuthTokenDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);

        var token = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(
            r => r.TokenHash == tokenHash,
            cancellationToken);

        if (token == null)
        {
            return Error.Unauthorized("AUTH_REFRESH_TOKEN_INVALID", "رمز التحديث غير صالح.");
        }

        // Replay attack / Token theft detection
        if (token.RevokedAt != null)
        {
            // Breach containment: revoke all active sessions for this user
            var activeTokens = await _unitOfWork.RefreshTokens.FindAsync(
                r => r.UserId == token.UserId && r.RevokedAt == null,
                cancellationToken);

            foreach (var activeToken in activeTokens)
            {
                activeToken.Revoke(_dateTimeProvider.UtcNow);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                token.UserId,
                null,
                "AUTH_TOKEN_BREACH_CONTAINED",
                "RefreshToken",
                token.Id.ToString(),
                new { TriggerTokenId = token.Id, RevokedSessionsCount = activeTokens.Count },
                cancellationToken);

            return Error.Unauthorized("AUTH_REFRESH_TOKEN_REVOKED", "تم إبطال رمز التحديث مسبقاً. تم إلغاء الجلسة لأسباب أمنية.");
        }

        if (token.ExpiresAt <= _dateTimeProvider.UtcNow)
        {
            return Error.Unauthorized("AUTH_REFRESH_TOKEN_EXPIRED", "انتهت صلاحية رمز التحديث. يرجى إعادة تسجيل الدخول.");
        }

        var user = await _unitOfWork.AppUsers.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return Error.Unauthorized("AUTH_USER_DEACTIVATED", "تم تعطيل حساب المستخدم. يرجى التواصل مع الإدارة.");
        }

        // Generate replacement token pair
        var (newAccessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshTokenPlaintext = _jwtTokenService.GenerateRefreshToken();
        var newHash = _jwtTokenService.HashToken(newRefreshTokenPlaintext);

        // Atomic rotation: revoke old token linking to new hash, and insert new token
        token.Revoke(_dateTimeProvider.UtcNow, newHash);

        var newRefreshTokenEntity = DomainRefreshToken.Create(
            token.UserId,
            newHash,
            _dateTimeProvider.UtcNow.AddDays(7));

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthTokenDto(newAccessToken, expiresAt, newRefreshTokenPlaintext));
    }
}
