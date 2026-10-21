namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppConfig;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;

public sealed class GetWorkspaceWhatsAppConfigQueryHandler
    : IRequestHandler<GetWorkspaceWhatsAppConfigQuery, Result<WorkspaceWhatsAppConfigDto>>
{
    private readonly ITahdeeraDbContext _dbContext;

    public GetWorkspaceWhatsAppConfigQueryHandler(ITahdeeraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WorkspaceWhatsAppConfigDto>> Handle(
        GetWorkspaceWhatsAppConfigQuery request,
        CancellationToken cancellationToken)
    {
        var config = await _dbContext.WorkspaceWhatsAppConfigs
            .AsNoTracking()
            .Include(c => c.ActiveSenderConnection)
            .FirstOrDefaultAsync(c => c.WorkspaceId == request.WorkspaceId, cancellationToken);

        if (config is null)
        {
            // Default return state
            return Result.Success(new WorkspaceWhatsAppConfigDto(
                request.WorkspaceId,
                "PlatformManaged",
                IsNotificationsEnabled: true,
                MonthlyMessageQuota: 1000,
                MessagesUsedThisMonth: 0,
                ActiveConnection: null));
        }

        var activeDto = config.ActiveSenderConnection is null ? null : new WhatsAppSenderConnectionDto(
            config.ActiveSenderConnection.Id,
            config.ActiveSenderConnection.WabaId,
            config.ActiveSenderConnection.PhoneNumberId,
            config.ActiveSenderConnection.DisplayPhoneNumber,
            config.ActiveSenderConnection.VerifiedBusinessName,
            config.ActiveSenderConnection.Status.ToString(),
            config.ActiveSenderConnection.ConnectedAt,
            HasAccessToken: true);

        return Result.Success(new WorkspaceWhatsAppConfigDto(
            config.WorkspaceId,
            config.SenderMode.ToString(),
            config.IsNotificationsEnabled,
            config.MonthlyMessageQuota,
            config.MessagesUsedThisMonth,
            activeDto));
    }
}
