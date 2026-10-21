namespace Tahdeera.Infrastructure.Services;

public static class WhatsAppRetryPolicy
{
    private static readonly TimeSpan[] RetryIntervals = new[]
    {
        TimeSpan.FromMinutes(1),   // Attempt 1 -> 2
        TimeSpan.FromMinutes(2),   // Attempt 2 -> 3
        TimeSpan.FromMinutes(5),   // Attempt 3 -> 4
        TimeSpan.FromMinutes(10),  // Attempt 4 -> 5
        TimeSpan.FromMinutes(30)   // Attempt 5 -> 6
    };

    public static TimeSpan GetRetryDelay(int currentAttempt)
    {
        if (currentAttempt <= 0) return TimeSpan.FromMinutes(1);
        if (currentAttempt >= RetryIntervals.Length) return RetryIntervals[^1];
        return RetryIntervals[currentAttempt - 1];
    }
}
