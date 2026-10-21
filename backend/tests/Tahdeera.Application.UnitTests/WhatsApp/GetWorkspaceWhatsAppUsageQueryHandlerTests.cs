using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces.Services;
using Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppUsage;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Domain.Repositories;
using Xunit;

namespace Tahdeera.Application.UnitTests.WhatsApp;

public sealed class GetWorkspaceWhatsAppUsageQueryHandlerTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppRateCardService _rateCardService;
    private readonly GetWorkspaceWhatsAppUsageQueryHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public GetWorkspaceWhatsAppUsageQueryHandlerTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _rateCardService = Substitute.For<IWhatsAppRateCardService>();
        _rateCardService.DefaultPerMessageUsd.Returns(0.0270m);
        _rateCardService.DefaultPerMessageEgp.Returns(1.3500m);
        _rateCardService.ExchangeRateEgpPerUsd.Returns(50.0m);

        _handler = new GetWorkspaceWhatsAppUsageQueryHandler(_unitOfWork, _rateCardService);
    }

    [Fact]
    public async Task Handle_WorkspaceNotFound_ReturnsFailureNotFound()
    {
        _unitOfWork.Workspaces.GetByIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns((Workspace?)null);

        var result = await _handler.Handle(new GetWorkspaceWhatsAppUsageQuery(_workspaceId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("WORKSPACE_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_UsageBelowThreshold_FlagsAreFalse()
    {
        var workspace = new Workspace { Id = _workspaceId, Name = "Test Academy", ContactPhone = "+201011112222" };
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged, monthlyQuota: 1000);
        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, "2026-10");
        ledger.RecordDispatch(0.0270m, 1.35m); // 1 dispatched out of 1000

        _unitOfWork.Workspaces.GetByIdAsync(_workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>()).Returns(config);
        _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(_workspaceId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ledger);

        var result = await _handler.Handle(new GetWorkspaceWhatsAppUsageQuery(_workspaceId, "2026-10"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.MonthlyMessageQuota.Should().Be(1000);
        dto.MessagesDispatchedCount.Should().Be(1);
        dto.MessagesRemainingThisMonth.Should().Be(999);
        dto.IsQuotaWarning.Should().BeFalse();
        dto.IsQuotaExceeded.Should().BeFalse();
        dto.RatePerMessageUsd.Should().Be(0.0270m);
        dto.RatePerMessageEgp.Should().Be(1.3500m);
        dto.ExchangeRateEgpPerUsd.Should().Be(50.0m);
    }

    [Fact]
    public async Task Handle_UsageAt80Percent_FlagsQuotaWarning()
    {
        var workspace = new Workspace { Id = _workspaceId, Name = "Test Academy", ContactPhone = "+201011112222" };
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged, monthlyQuota: 100);
        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, "2026-10");
        for (int i = 0; i < 80; i++)
        {
            ledger.RecordDispatch(0.0270m, 1.35m);
        }

        _unitOfWork.Workspaces.GetByIdAsync(_workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>()).Returns(config);
        _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(_workspaceId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ledger);

        var result = await _handler.Handle(new GetWorkspaceWhatsAppUsageQuery(_workspaceId, "2026-10"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.MessagesDispatchedCount.Should().Be(80);
        dto.MessagesRemainingThisMonth.Should().Be(20);
        dto.IsQuotaWarning.Should().BeTrue();
        dto.IsQuotaExceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UsageAtOrAbove100Percent_FlagsQuotaExceeded()
    {
        var workspace = new Workspace { Id = _workspaceId, Name = "Test Academy", ContactPhone = "+201011112222" };
        var config = WorkspaceWhatsAppConfig.Create(_workspaceId, WhatsAppSenderMode.PlatformManaged, monthlyQuota: 100);
        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, "2026-10");
        for (int i = 0; i < 105; i++)
        {
            ledger.RecordDispatch(0.0270m, 1.35m);
        }

        _unitOfWork.Workspaces.GetByIdAsync(_workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);
        _unitOfWork.WorkspaceWhatsAppConfigs.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>()).Returns(config);
        _unitOfWork.WorkspaceWhatsAppUsageLedgers.GetOrCreateCurrentMonthAsync(_workspaceId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ledger);

        var result = await _handler.Handle(new GetWorkspaceWhatsAppUsageQuery(_workspaceId, "2026-10"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.MessagesDispatchedCount.Should().Be(105);
        dto.MessagesRemainingThisMonth.Should().Be(0); // Clamped to 0
        dto.IsQuotaWarning.Should().BeTrue();
        dto.IsQuotaExceeded.Should().BeTrue();
    }
}
