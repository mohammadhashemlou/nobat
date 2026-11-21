namespace Nobat.Application.Tests;

/// <summary>
/// Resolves the business timezone in a platform-independent way.
/// .NET on Linux/macOS uses IANA ids, Windows uses the legacy registry ids.
/// </summary>
internal static class TestTimeZones
{
    public static TimeZoneInfo Tehran { get; } = ResolveTehran();

    private static TimeZoneInfo ResolveTehran()
    {
        foreach (var id in new[] { "Asia/Tehran", "Iran Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // try the next id
            }
            catch (InvalidTimeZoneException)
            {
                // try the next id
            }
        }

        // Fallback: Iran has observed no DST since 2022, so a fixed +03:30 zone is faithful.
        return TimeZoneInfo.CreateCustomTimeZone(
            "Test/Tehran", TimeSpan.FromMinutes(210), "Tehran (fixed)", "Tehran (fixed)");
    }

    /// <summary>Converts a local Tehran wall-clock time on <paramref name="date"/> to UTC.</summary>
    public static DateTime ToUtc(DateOnly date, int hour, int minute = 0)
        => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(hour, minute)), DateTimeKind.Unspecified),
            Tehran);
}
