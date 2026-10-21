namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWhatsAppMessageById;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;

public sealed record GetWhatsAppMessageByIdQuery(
    Guid WorkspaceId,
    Guid MessageId,
    bool IsOwner) : IRequest<Result<WhatsAppMessageDetailDto>>;

public sealed class GetWhatsAppMessageByIdQueryHandler
    : IRequestHandler<GetWhatsAppMessageByIdQuery, Result<WhatsAppMessageDetailDto>>
{
    private readonly ITahdeeraDbContext _dbContext;

    public GetWhatsAppMessageByIdQueryHandler(ITahdeeraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<WhatsAppMessageDetailDto>> Handle(
        GetWhatsAppMessageByIdQuery request,
        CancellationToken cancellationToken)
    {
        var message = await _dbContext.MessageOutboxes
            .AsNoTracking()
            .Include(m => m.Student)
            .FirstOrDefaultAsync(m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken);

        if (message is null)
        {
            return Result.Failure<WhatsAppMessageDetailDto>(Error.NotFound("MESSAGE_NOT_FOUND", $"Outbox message with ID '{request.MessageId}' was not found."));
        }

        var phone = request.IsOwner ? message.RecipientPhone : MaskPhone(message.RecipientPhone);

        var dto = new WhatsAppMessageDetailDto(
            message.Id,
            message.WorkspaceId,
            message.StudentId,
            message.Student != null ? message.Student.FullName : null,
            message.SessionId,
            phone,
            message.MessageType,
            message.TemplateName,
            message.LanguageCode,
            message.Payload,
            message.Status.ToString(),
            message.SenderMode,
            message.Attempts,
            message.MaxAttempts,
            message.SentAt,
            message.DeliveredAt,
            message.ErrorCode,
            message.ErrorDetails,
            message.CreatedAt,
            message.UpdatedAt);

        return Result.Success(dto);
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length < 7) return phone;
        return $"{phone[..3]}****{phone[^4..]}";
    }
}
