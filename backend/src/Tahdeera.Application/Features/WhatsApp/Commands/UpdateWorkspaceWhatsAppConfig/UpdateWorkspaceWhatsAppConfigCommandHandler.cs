namespace Tahdeera.Application.Features.WhatsApp.Commands.UpdateWorkspaceWhatsAppConfig;

using MediatR;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

public sealed class UpdateWorkspaceWhatsAppConfigCommandHandler
    : IRequestHandler<UpdateWorkspaceWhatsAppConfigCommand, Result<WorkspaceWhatsAppConfigDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppSenderResolver _senderResolver;

    public UpdateWorkspaceWhatsAppConfigCommandHandler(
        IUnitOfWork unitOfWork,
        IWhatsAppSenderResolver senderResolver)
    {
        _unitOfWork = unitOfWork;
        _senderResolver = senderResolver;
    }

    public async Task<Result<WorkspaceWhatsAppConfigDto>> Handle(
        UpdateWorkspaceWhatsAppConfigCommand request,
        CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);
        if (config is null)
        {
            config = WorkspaceWhatsAppConfig.Create(request.WorkspaceId);
            await _unitOfWork.WorkspaceWhatsAppConfigs.AddAsync(config, cancellationToken);
        }

        var parsedMode = Enum.Parse<WhatsAppSenderMode>(request.SenderMode, ignoreCase: true);

        // Validation: WorkspaceOwned requires an active connected WABA
        WhatsAppSenderConnection? activeConn = null;
        if (parsedMode == WhatsAppSenderMode.WorkspaceOwned)
        {
            activeConn = await _unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(request.WorkspaceId, cancellationToken);
            if (activeConn is null || activeConn.Status != SenderConnectionStatus.Connected)
            {
                return Result.Failure<WorkspaceWhatsAppConfigDto>(DomainErrors.WhatsApp.SenderNotConfigured);
            }
        }

        config.UpdateSenderMode(parsedMode, activeConn?.Id);
        config.ToggleNotifications(request.IsNotificationsEnabled);

        // DEC-01 / ADR-009: Cascade cancellation on disable
        if (!request.IsNotificationsEnabled || parsedMode == WhatsAppSenderMode.Disabled)
        {
            await _unitOfWork.MessageOutbox.CancelPendingAndTransientForWorkspaceAsync(
                request.WorkspaceId,
                "Notifications disabled for workspace",
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Evict cached sender descriptor immediately
        _senderResolver.InvalidateSenderCache(request.WorkspaceId);

        var dto = new WorkspaceWhatsAppConfigDto(
            config.WorkspaceId,
            config.SenderMode.ToString(),
            config.IsNotificationsEnabled,
            config.MonthlyMessageQuota,
            config.MessagesUsedThisMonth,
            activeConn is null ? null : new WhatsAppSenderConnectionDto(
                activeConn.Id,
                activeConn.WabaId,
                activeConn.PhoneNumberId,
                activeConn.DisplayPhoneNumber,
                activeConn.VerifiedBusinessName,
                activeConn.Status.ToString(),
                activeConn.ConnectedAt,
                HasAccessToken: true));

        return Result.Success(dto);
    }
}
