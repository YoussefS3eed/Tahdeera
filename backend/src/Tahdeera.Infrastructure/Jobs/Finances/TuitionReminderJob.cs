using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Text.Json;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Commands.StageTuitionReminders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Jobs.Finances;

[DisallowConcurrentExecution]
public class TuitionReminderJob : IJob
{
    private readonly ISender _sender;
    private readonly ITahdeeraDbContext _context;
    private readonly ILogger<TuitionReminderJob> _logger;

    public TuitionReminderJob(
        ISender sender,
        ITahdeeraDbContext context,
        ILogger<TuitionReminderJob> logger)
    {
        _sender = sender;
        _context = context;
        _logger = logger;
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scheduled TuitionReminderJob execution...");

        var activeSubscriptions = await _context.WorkspaceSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .Include(s => s.Workspace)
            .Where(s => s.Status == "Active" && s.Workspace != null && s.Workspace.IsActive)
            .ToListAsync(cancellationToken);

        var activeWorkspaces = activeSubscriptions
            .Where(s => IsRemindersFeatureEnabled(s) && s.Workspace != null)
            .Select(s => s.Workspace!)
            .DistinctBy(w => w.Id)
            .ToList();

        _logger.LogInformation("Found {Count} eligible active workspaces for tuition reminder evaluation", activeWorkspaces.Count);

        foreach (var workspace in activeWorkspaces)
        {
            try
            {
                var command = new StageTuitionRemindersCommand(
                    WorkspaceId: workspace.Id,
                    GroupId: null,
                    MinimumBalanceThreshold: 50.00m);

                var result = await _sender.Send(command, cancellationToken);

                _logger.LogInformation(
                    "Tuition reminders staged for workspace {WorkspaceId} ({WorkspaceName}): TotalDebtorsFound={TotalDebtorsFound}, RemindersStagedCount={RemindersStagedCount}, SkippedCooldownCount={SkippedCooldownCount}",
                    workspace.Id, workspace.Name, result.TotalDebtorsFound, result.RemindersStagedCount, result.SkippedCooldownCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stage tuition reminders for workspace {WorkspaceId}", workspace.Id);
            }
        }

        _logger.LogInformation("TuitionReminderJob execution finished successfully.");
    }

    private static bool IsRemindersFeatureEnabled(WorkspaceSubscription subscription)
    {
        bool isEnabled = subscription.Plan != null &&
                         subscription.Plan.Features.PaymentsAndFees &&
                         subscription.Plan.Features.WhatsappNotifications;

        if (!string.IsNullOrWhiteSpace(subscription.FeatureOverrides) && subscription.FeatureOverrides != "{}")
        {
            try
            {
                using var doc = JsonDocument.Parse(subscription.FeatureOverrides);
                if (doc.RootElement.TryGetProperty("tuitionReminders", out var prop))
                {
                    isEnabled = prop.GetBoolean();
                }
                else if (doc.RootElement.TryGetProperty("TuitionReminders", out var propPascal))
                {
                    isEnabled = propPascal.GetBoolean();
                }
            }
            catch
            {
                // Fallback to plan features
            }
        }

        return isEnabled;
    }
}
