using FluentAssertions;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Domain.UnitTests.Entities;

public class WorkspaceWhatsAppUsageLedgerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();

    [Fact]
    public void Create_InitializesZeroCountersAndCosts()
    {
        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, "2026-10");

        ledger.WorkspaceId.Should().Be(_workspaceId);
        ledger.YearMonth.Should().Be("2026-10");
        ledger.MessagesDispatchedCount.Should().Be(0);
        ledger.MessagesDeliveredCount.Should().Be(0);
        ledger.MessagesFailedCount.Should().Be(0);
        ledger.EstimatedCostUsd.Should().Be(0m);
        ledger.EstimatedCostEgp.Should().Be(0m);
    }

    [Fact]
    public void RecordDispatch_AccumulatesDualCurrencies()
    {
        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, "2026-10");

        ledger.RecordDispatch(0.0270m, 1.3500m);

        ledger.MessagesDispatchedCount.Should().Be(1);
        ledger.EstimatedCostUsd.Should().Be(0.0270m);
        ledger.EstimatedCostEgp.Should().Be(1.3500m);
    }

    [Fact]
    public void RecordDispatch_MultipleCalls_AccumulatesAccurately()
    {
        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, "2026-10");

        for (int i = 0; i < 10; i++)
        {
            ledger.RecordDispatch(0.0270m, 1.3500m);
        }

        ledger.MessagesDispatchedCount.Should().Be(10);
        ledger.EstimatedCostUsd.Should().Be(0.2700m);
        ledger.EstimatedCostEgp.Should().Be(13.5000m);
    }

    [Fact]
    public void RecordDeliveredAndRecordFailed_IncrementsRespectiveCounters()
    {
        var ledger = WorkspaceWhatsAppUsageLedger.Create(_workspaceId, "2026-10");

        ledger.RecordDelivered();
        ledger.RecordDelivered();
        ledger.RecordFailed();

        ledger.MessagesDeliveredCount.Should().Be(2);
        ledger.MessagesFailedCount.Should().Be(1);
    }
}
