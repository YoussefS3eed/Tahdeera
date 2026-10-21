namespace Tahdeera.Application.Features.WhatsApp.Commands.DisconnectWhatsAppSender;

using MediatR;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;

public sealed class DisconnectWhatsAppSenderCommandHandler
    : IRequestHandler<DisconnectWhatsAppSenderCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppSenderResolver _senderResolver;

    public DisconnectWhatsAppSenderCommandHandler(
        IUnitOfWork unitOfWork,
        IWhatsAppSenderResolver senderResolver)
    {
        _unitOfWork = unitOfWork;
        _senderResolver = senderResolver;
    }

    public async Task<Result> Handle(
        DisconnectWhatsAppSenderCommand request,
        CancellationToken cancellationToken)
    {
        var activeConnection = await _unitOfWork.WhatsAppSenderConnections.GetActiveForWorkspaceAsync(request.WorkspaceId, cancellationToken);
        if (activeConnection is null)
        {
            return Result.Failure(DomainErrors.WhatsApp.WabaConnectionNotFound);
        }

        // 1. Mark connection as Disconnected
        activeConnection.Disconnect("Disconnected by user");

        // 2. Clear pointer on config and switch to Disabled
        var config = await _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);
        if (config is not null)
        {
            config.ClearActiveConnection();
        }

        // 3. DEC-01: Cancel pending & transient outbox messages
        await _unitOfWork.MessageOutbox.CancelPendingAndTransientForWorkspaceAsync(
            request.WorkspaceId,
            "WhatsApp sender disconnected by center owner",
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Invalidate cache
        _senderResolver.InvalidateSenderCache(request.WorkspaceId);

        return Result.Success();
    }
}
