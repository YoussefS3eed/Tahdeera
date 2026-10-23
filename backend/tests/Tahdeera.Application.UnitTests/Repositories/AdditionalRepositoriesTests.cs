using FluentAssertions;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence.Repositories;

namespace Tahdeera.Application.UnitTests.Repositories;

public class AdditionalRepositoriesTests
{
    [Fact]
    public async Task SubscriptionPlanRepository_QueriesAndFiltersCorrectly()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new SubscriptionPlanRepository(context);

        var plan1 = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = "FREE_TRIAL",
            NameAr = "باقة تجريبية",
            NameEn = "Trial",
            DefaultPrice = 0m,
            IsActive = true
        };
        var plan2 = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = "PRO_ANNUAL",
            NameAr = "باقة سنوية",
            NameEn = "Pro Annual",
            DefaultPrice = 1200m,
            IsActive = true
        };
        var plan3 = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = "LEGACY_TIER",
            NameAr = "باقة قديمة",
            NameEn = "Legacy",
            DefaultPrice = 600m,
            IsActive = false
        };

        context.SubscriptionPlans.AddRange(plan1, plan2, plan3);
        await context.SaveChangesAsync();

        var exists = await repo.ExistsByCodeAsync("pro_annual");
        exists.Should().BeTrue();

        var notExists = await repo.ExistsByCodeAsync("NONEXISTENT");
        notExists.Should().BeFalse();

        var byCode = await repo.GetByCodeAsync("free_trial");
        byCode.Should().NotBeNull();
        byCode!.Code.Should().Be("FREE_TRIAL");

        var activeOnly = await repo.GetPlansAsync(activeOnly: true);
        activeOnly.Should().HaveCount(2);
        activeOnly.Select(p => p.Code).Should().ContainInOrder("FREE_TRIAL", "PRO_ANNUAL");

        var allPlans = await repo.GetPlansAsync(activeOnly: false);
        allPlans.Should().HaveCount(3);
    }

    [Fact]
    public async Task WorkspaceRepository_ExistsAndGetDetails_BehaveExpectedly()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new WorkspaceRepository(context);

        var ws = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "سنتر النجوم",
            Kind = "Center",
            IsActive = true
        };
        context.Workspaces.Add(ws);
        await context.SaveChangesAsync();

        var exists = await repo.ExistsByNameAsync("سنتر النجوم");
        exists.Should().BeTrue();

        var notExists = await repo.ExistsByNameAsync("سنتر غير موجود");
        notExists.Should().BeFalse();

        var details = await repo.GetWithDetailsAsync(ws.Id);
        details.Should().NotBeNull();
        details!.Name.Should().Be("سنتر النجوم");
    }

    [Fact]
    public async Task WorkspaceWhatsAppConfigRepository_GetsByWorkspaceIdWithConnection()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new WorkspaceWhatsAppConfigRepository(context);
        var workspaceId = Guid.NewGuid();

        var conn = WhatsAppSenderConnection.CreateWorkspaceConnection(
            workspaceId, "waba_1", "phone_1", "+201012345678", "enc_token", "My Business");
        context.WhatsAppSenderConnections.Add(conn);

        var config = WorkspaceWhatsAppConfig.Create(workspaceId, WhatsAppSenderMode.WorkspaceOwned);
        config.AssignActiveConnection(conn.Id);
        context.WorkspaceWhatsAppConfigs.Add(config);
        await context.SaveChangesAsync();

        var retrieved = await repo.GetByWorkspaceIdAsync(workspaceId);
        retrieved.Should().NotBeNull();
        retrieved!.WorkspaceId.Should().Be(workspaceId);
        retrieved.ActiveSenderConnection.Should().NotBeNull();
        retrieved.ActiveSenderConnection!.DisplayPhoneNumber.Should().Be("+201012345678");
    }

    [Fact]
    public async Task WorkspaceWhatsAppUsageLedgerRepository_GetOrCreate_CreatesWhenMissing()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new WorkspaceWhatsAppUsageLedgerRepository(context);
        var workspaceId = Guid.NewGuid();
        var ym = "2026-10";

        var ledger = await repo.GetOrCreateCurrentMonthAsync(workspaceId, ym);
        ledger.Should().NotBeNull();
        ledger!.WorkspaceId.Should().Be(workspaceId);
        ledger.YearMonth.Should().Be(ym);
        await context.SaveChangesAsync();

        // Second call returns existing record without creating a duplicate
        var existing = await repo.GetOrCreateCurrentMonthAsync(workspaceId, ym);
        existing.Should().NotBeNull();
        existing!.Id.Should().Be(ledger.Id);
    }

    [Fact]
    public async Task WhatsAppSenderConnectionRepository_HistoryAndActiveLookups()
    {
        using var context = TestDbContextFactory.Create();
        var repo = new WhatsAppSenderConnectionRepository(context);
        var workspaceId = Guid.NewGuid();

        var conn1 = WhatsAppSenderConnection.CreateWorkspaceConnection(
            workspaceId, "waba_old", "phone_old", "+201000000001", "enc_old", "Old Biz");
        conn1.Disconnect("Switched to new line");

        var conn2 = WhatsAppSenderConnection.CreateWorkspaceConnection(
            workspaceId, "waba_new", "phone_new", "+201000000002", "enc_new", "New Biz");

        context.WhatsAppSenderConnections.AddRange(conn1, conn2);
        await context.SaveChangesAsync();

        var active = await repo.GetActiveForWorkspaceAsync(workspaceId);
        active.Should().NotBeNull();
        active!.PhoneNumberId.Should().Be("phone_new");
        active.Status.Should().Be(SenderConnectionStatus.Connected);

        var history = await repo.GetHistoryForWorkspaceAsync(workspaceId);
        history.Should().HaveCount(2);
    }

    [Fact]
    public async Task StudentAccountAndChargeAndPaymentRepositories_GraphQueries()
    {
        using var context = TestDbContextFactory.Create();
        var accountRepo = new StudentAccountRepository(context);
        var chargeRepo = new StudentChargeRepository(context);
        var paymentRepo = new StudentPaymentRepository(context);

        var wsStudentId = Guid.NewGuid();
        var account = StudentAccount.Create(wsStudentId);
        context.StudentAccounts.Add(account);
        await context.SaveChangesAsync();

        var charge = StudentCharge.Create(
            account.Id,
            description: "Monthly charge",
            amount: 300m,
            groupId: null,
            billingPeriodLabel: "2026-10");
        context.StudentCharges.Add(charge);

        var payment = StudentPayment.Create(
            account.Id,
            receiptNumber: "RCP-2026-0099",
            amount: 300m,
            method: PaymentMethod.Cash,
            recordedByUserId: Guid.NewGuid(),
            paidAt: DateTimeOffset.UtcNow,
            reference: null,
            notes: "Notes");
        context.StudentPayments.Add(payment);

        var allocation = PaymentAllocation.Create(payment.Id, charge.Id, 300m);
        context.PaymentAllocations.Add(allocation);
        await context.SaveChangesAsync();

        // StudentAccountRepository
        var accountWithGraph = await accountRepo.GetByWorkspaceStudentIdAsync(wsStudentId);
        accountWithGraph.Should().NotBeNull();
        accountWithGraph!.Charges.Should().HaveCount(1);
        accountWithGraph.Payments.Should().HaveCount(1);

        // StudentChargeRepository
        var chargeWithAllocations = await chargeRepo.GetByIdAsync(charge.Id);
        chargeWithAllocations.Should().NotBeNull();
        chargeWithAllocations!.Allocations.Should().HaveCount(1);

        var openCharges = await chargeRepo.GetOpenChargesByAccountIdAsync(account.Id);
        openCharges.Should().HaveCount(1);

        // StudentPaymentRepository
        var paymentWithAllocations = await paymentRepo.GetByIdWithAllocationsAsync(payment.Id);
        paymentWithAllocations.Should().NotBeNull();
        paymentWithAllocations!.Allocations.Should().HaveCount(1);

        var byReceipt = await paymentRepo.GetByReceiptNumberAsync("RCP-2026-0099");
        byReceipt.Should().NotBeNull();
        byReceipt!.ReceiptNumber.Should().Be("RCP-2026-0099");
    }
}
