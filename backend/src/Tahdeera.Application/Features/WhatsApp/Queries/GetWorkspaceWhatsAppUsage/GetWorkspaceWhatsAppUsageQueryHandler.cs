using MediatR;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Features.WhatsApp.DTOs;
using Tahdeera.Domain.Common;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppUsage;

public sealed class GetWorkspaceWhatsAppUsageQueryHandler : IRequestHandler<GetWorkspaceWhatsAppUsageQuery, Result<WorkspaceWhatsAppUsageDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppRateCardService _rateCardService;

    public GetWorkspaceWhatsAppUsageQueryHandler(
        IUnitOfWork unitOfWork,
        IWhatsAppRateCardService rateCardService)
    {
        _unitOfWork = unitOfWork;
        _rateCardService = rateCardService;
    }

    public async Task<Result<WorkspaceWhatsAppUsageDto>> Handle(
        GetWorkspaceWhatsAppUsageQuery request,
        CancellationToken cancellationToken)
    {
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
        {
            return Error.NotFound("WORKSPACE_NOT_FOUND", "مساحة العمل المطلوبة غير موجودة.");
        }

        var config = await _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);
        int monthlyQuota = config?.MonthlyMessageQuota ?? 1000;

        var targetMonth = string.IsNullOrWhiteSpace(request.YearMonth)
            ? DateTimeOffset.UtcNow.ToString("yyyy-MM")
            : request.YearMonth;

        var ledger = await _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(
            request.WorkspaceId,
            targetMonth,
            cancellationToken);

        int dispatched = ledger?.MessagesDispatchedCount ?? 0;
        int delivered = ledger?.MessagesDeliveredCount ?? 0;
        int failed = ledger?.MessagesFailedCount ?? 0;
        decimal costUsd = ledger?.EstimatedCostUsd ?? 0m;
        decimal costEgp = ledger?.EstimatedCostEgp ?? 0m;

        int remaining = Math.Max(0, monthlyQuota - dispatched);
        bool isQuotaWarning = monthlyQuota > 0 && dispatched >= (int)Math.Ceiling(monthlyQuota * 0.80);
        bool isQuotaExceeded = monthlyQuota > 0 && dispatched >= monthlyQuota;
        double successRate = dispatched > 0 ? Math.Round((double)delivered / dispatched * 100.0, 2) : 100.0;

        var dto = new WorkspaceWhatsAppUsageDto(
            WorkspaceId: request.WorkspaceId,
            YearMonth: targetMonth,
            MonthlyMessageQuota: monthlyQuota,
            MessagesDispatchedCount: dispatched,
            MessagesDeliveredCount: delivered,
            MessagesFailedCount: failed,
            MessagesRemainingThisMonth: remaining,
            IsQuotaWarning: isQuotaWarning,
            IsQuotaExceeded: isQuotaExceeded,
            DeliverySuccessRatePercentage: successRate,
            EstimatedCostUsd: costUsd,
            EstimatedCostEgp: costEgp,
            RatePerMessageUsd: _rateCardService.DefaultPerMessageUsd,
            RatePerMessageEgp: _rateCardService.DefaultPerMessageEgp,
            ExchangeRateEgpPerUsd: _rateCardService.ExchangeRateEgpPerUsd
        );

        return Result.Success(dto);
    }
}
