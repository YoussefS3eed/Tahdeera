namespace Tahdeera.Infrastructure.BackgroundJobs;

public sealed class AttendanceWorkerOptions
{
    public const string SectionName = "AttendanceWorker";

    public int SessionCompletionIntervalMinutes { get; set; } = 10;
    public int BatchSize { get; set; } = 50;
}
