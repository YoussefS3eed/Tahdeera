namespace Tahdeera.Application.Features.WhatsApp.Commands.ConnectWhatsAppSender;

using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

public sealed class ConnectWhatsAppSenderCommandHandler
    : IRequestHandler<ConnectWhatsAppSenderCommand, Result<WhatsAppSenderConnectionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMetaOAuthClient _metaOAuthClient;
    private readonly IWhatsAppSenderResolver _senderResolver;
    private readonly IDataProtector _protector;

    public ConnectWhatsAppSenderCommandHandler(
        IUnitOfWork unitOfWork,
        IMetaOAuthClient metaOAuthClient,
        IWhatsAppSenderResolver senderResolver,
        IDataProtectionProvider dataProtectionProvider)
    {
        _unitOfWork = unitOfWork;
        _metaOAuthClient = metaOAuthClient;
        _senderResolver = senderResolver;
        _protector = dataProtectionProvider.CreateProtector("Tahdeera.WhatsApp.Tokens.v1");
    }

    public async Task<Result<WhatsAppSenderConnectionDto>> Handle(
        ConnectWhatsAppSenderCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Server-to-server OAuth code exchange
        var oauthResponse = await _metaOAuthClient.ExchangeCodeForTokenAsync(request.Code, cancellationToken);
        if (oauthResponse is null || string.IsNullOrWhiteSpace(oauthResponse.AccessToken))
        {
            return Result.Failure<WhatsAppSenderConnectionDto>(DomainErrors.WhatsApp.MetaServiceUnavailable);
        }

        // 2. Encrypt System User token using authenticated Data Protection
        var encryptedToken = _protector.Protect(oauthResponse.AccessToken);

        // 3. Mark any existing active connection for this workspace as Disconnected
        var existingActive = await _unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(request.WorkspaceId, cancellationToken);
        existingActive?.Disconnect("Replaced by new Embedded Signup connection");

        // 4. Create new WhatsAppSenderConnection record
        var connection = WhatsAppSenderConnection.CreateWorkspaceConnection(
            request.WorkspaceId,
            request.WabaId,
            request.PhoneNumberId,
            request.DisplayPhoneNumber,
            encryptedToken,
            verifiedBusinessName: null);

        await _unitOfWork.WhatsAppSenderConnections.AddAsync(connection, cancellationToken);

        // 5. Update WorkspaceWhatsAppConfig to point to new connection and set SenderMode = WorkspaceOwned
        var config = await _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);
        if (config is null)
        {
            config = WorkspaceWhatsAppConfig.Create(request.WorkspaceId, WhatsAppSenderMode.WorkspaceOwned);
            config.AssignActiveConnection(connection.Id);
            await _unitOfWork.WorkspaceWhatsAppConfigs.AddAsync(config, cancellationToken);
        }
        else
        {
            config.UpdateSenderMode(WhatsAppSenderMode.WorkspaceOwned, connection.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Immediately evict cached sender descriptor
        _senderResolver.InvalidateSenderCache(request.WorkspaceId);

        var dto = new WhatsAppSenderConnectionDto(
            connection.Id,
            connection.WabaId,
            connection.PhoneNumberId,
            connection.DisplayPhoneNumber,
            connection.VerifiedBusinessName,
            connection.Status.ToString(),
            connection.ConnectedAt,
            HasAccessToken: true);

        return Result.Success(dto);
    }
}
