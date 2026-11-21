using Nobat.Application.Common.Interfaces;

namespace Nobat.Infrastructure.Common.Services;

public class BusinessTimeProvider : IBusinessTimeProvider
{
    public BusinessTimeProvider(string timeZoneId = "Asia/Tehran")
    {
        TimeZone = ResolveTimeZone(timeZoneId);
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public TimeZoneInfo TimeZone { get; }

    public DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone));

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows fallback for Asia/Tehran
            return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
