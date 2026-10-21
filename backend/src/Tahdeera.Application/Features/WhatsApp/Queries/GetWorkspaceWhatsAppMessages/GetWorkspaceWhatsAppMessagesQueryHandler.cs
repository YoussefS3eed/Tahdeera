namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppMessages;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Enums;

public sealed class GetWorkspaceWhatsAppMessagesQueryHandler
    : IRequestHandler<GetWorkspaceWhatsAppMessagesQuery, Result<PagedList<WhatsAppMessageListItemDto>>>
{
    private readonly ITahdeeraDbContext _dbContext;

    public GetWorkspaceWhatsAppMessagesQueryHandler(ITahdeeraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<WhatsAppMessageListItemDto>>> Handle(
        GetWorkspaceWhatsAppMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MessageOutboxes
            .AsNoTracking()
            .Include(m => m.Student)
            .Where(m => m.WorkspaceId == request.WorkspaceId);

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<OutboxMessageStatus>(request.Status, true, out var status))
        {
            query = query.Where(m => m.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.MessageType))
        {
            query = query.Where(m => m.MessageType == request.MessageType);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new WhatsAppMessageListItemDto(
                m.Id,
                m.StudentId,
                m.Student != null ? m.Student.FullName : null,
                request.IsOwner ? m.RecipientPhone : MaskPhone(m.RecipientPhone),
                m.MessageType,
                m.TemplateName,
                m.LanguageCode,
                m.Status.ToString(),
                m.Attempts,
                m.SentAt,
                m.DeliveredAt,
                m.CostEgp,
                m.ErrorCode,
                m.ErrorDetails,
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        var pagedList = new PagedList<WhatsAppMessageListItemDto>(items, totalCount, request.Page, request.PageSize);
        return Result.Success(pagedList);
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length < 7) return phone;
        return $"{phone[..3]}****{phone[^4..]}";
    }
}
