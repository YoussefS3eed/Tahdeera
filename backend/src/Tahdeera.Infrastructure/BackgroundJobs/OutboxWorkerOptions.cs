namespace Tahdeera.Infrastructure.BackgroundJobs;

public sealed class OutboxWorkerOptions
{
    public const string SectionName = "OutboxWorker";

    public int PollingIntervalMilliseconds { get; set; } = 2000;
    public int BatchSize { get; set; } = 50;
    public int LeaseTimeoutMinutes { get; set; } = 5;
}
