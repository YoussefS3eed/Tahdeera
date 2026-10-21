namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppMessages;

using MediatR;
using Tahdeera.Application.Common.Models;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;

public sealed record GetWorkspaceWhatsAppMessagesQuery(
    Guid WorkspaceId,
    bool IsOwner,
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? MessageType = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null) : IRequest<Result<PagedList<WhatsAppMessageListItemDto>>>;
