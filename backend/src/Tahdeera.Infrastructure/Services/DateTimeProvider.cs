using Tahdeera.Application.Common.Interfaces;

namespace Tahdeera.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
