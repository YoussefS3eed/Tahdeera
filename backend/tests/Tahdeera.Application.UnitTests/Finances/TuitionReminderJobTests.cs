using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Commands.StageTuitionReminders;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.ValueObjects;
using Tahdeera.Infrastructure.Jobs.Finances;

namespace Tahdeera.Application.UnitTests.Finances;

public class TuitionReminderJobTests
{
    private readonly ISender _sender;
    private readonly ITahdeeraDbContext _dbContext;
    private readonly ILogger<TuitionReminderJob> _logger;
    private readonly TuitionReminderJob _job;

    public TuitionReminderJobTests()
    {
        _sender = Substitute.For<ISender>();
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _logger = Substitute.For<ILogger<TuitionReminderJob>>();
        _job = new TuitionReminderJob(_sender, _dbContext, _logger);
    }

    [Fact]
    public async Task Execute_ActiveWorkspaceWithRemindersFeature_DispatchesStageTuitionRemindersCommand()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace
        {
            Id = workspaceId,
            Name = "Center Alpha",
            IsActive = true
        };

        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = "CENTER_PRO",
            Features = new SubscriptionPlanFeatures(
                PaymentsAndFees: true,
                WhatsappNotifications: true)
        };

        var subscription = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Workspace = workspace,
            PlanId = plan.Id,
            Plan = plan,
            Status = "Active"
        };

        var subsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { subscription });
        _dbContext.WorkspaceSubscriptions.Returns(subsDbSet);

        _sender.Send(Arg.Any<StageTuitionRemindersCommand>(), Arg.Any<CancellationToken>())
            .Returns(new StageTuitionRemindersResultDto(
                WorkspaceId: workspaceId,
                GroupId: null,
                MinimumBalanceThreshold: 50.00m,
                TotalDebtorsFound: 5,
                RemindersStagedCount: 5,
                SkippedCooldownCount: 0,
                SkippedOptedOutCount: 0,
                SkippedInvalidPhoneCount: 0,
                TotalOutstandingArrearsNotified: 1500m));

        var jobContext = Substitute.For<IJobExecutionContext>();
        jobContext.CancellationToken.Returns(CancellationToken.None);

        // Act
        await _job.Execute(jobContext);

        // Assert
        await _sender.Received(1).Send(
            Arg.Is<StageTuitionRemindersCommand>(c => c.WorkspaceId == workspaceId && c.GroupId == null && c.MinimumBalanceThreshold == 50.00m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WorkspaceWithDisabledFeature_DoesNotDispatchCommand()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = new Workspace
        {
            Id = workspaceId,
            Name = "Center Beta",
            IsActive = true
        };

        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Code = "BASIC",
            Features = new SubscriptionPlanFeatures(
                PaymentsAndFees: false,
                WhatsappNotifications: false)
        };

        var subscription = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Workspace = workspace,
            PlanId = plan.Id,
            Plan = plan,
            Status = "Active"
        };

        var subsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { subscription });
        _dbContext.WorkspaceSubscriptions.Returns(subsDbSet);

        var jobContext = Substitute.For<IJobExecutionContext>();
        jobContext.CancellationToken.Returns(CancellationToken.None);

        // Act
        await _job.Execute(jobContext);

        // Assert
        await _sender.DidNotReceive().Send(
            Arg.Any<StageTuitionRemindersCommand>(),
            Arg.Any<CancellationToken>());
    }
}
