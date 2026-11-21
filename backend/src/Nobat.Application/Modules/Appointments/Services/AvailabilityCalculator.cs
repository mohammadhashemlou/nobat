namespace Nobat.Application.Modules.Appointments.Services;

/// <summary>
/// Pure availability calculation — no I/O, fully unit-testable.
/// Works in the business timezone and returns UTC slots.
/// </summary>
public class AvailabilityCalculator
{
    /// <summary>
    /// Computes bookable slots for a single day.
    /// </summary>
    /// <param name="date">Local business date.</param>
    /// <param name="workStart">Working-hours start (local).</param>
    /// <param name="workEnd">Working-hours end (local).</param>
    /// <param name="breakStart">Break start (local), optional.</param>
    /// <param name="breakEnd">Break end (local), optional.</param>
    /// <param name="serviceDurationMinutes">Length of the service.</param>
    /// <param name="slotStepMinutes">Granularity of offered slots.</param>
    /// <param name="busy">Existing busy intervals in UTC.</param>
    /// <param name="nowUtc">Current UTC time (slots before this + lead time are dropped).</param>
    /// <param name="minLeadTimeMinutes">Minimum notice before a slot can be booked.</param>
    /// <param name="tz">Business timezone.</param>
    public IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> Calculate(
        DateOnly date,
        TimeOnly workStart,
        TimeOnly workEnd,
        TimeOnly? breakStart,
        TimeOnly? breakEnd,
        int serviceDurationMinutes,
        int slotStepMinutes,
        IEnumerable<(DateTime StartUtc, DateTime EndUtc)> busy,
        DateTime nowUtc,
        int minLeadTimeMinutes,
        TimeZoneInfo tz)
    {
        var results = new List<(DateTime, DateTime)>();

        if (serviceDurationMinutes <= 0 || slotStepMinutes <= 0)
            return results;

        var localStart = date.ToDateTime(workStart, DateTimeKind.Unspecified);
        var localEnd = date.ToDateTime(workEnd, DateTimeKind.Unspecified);

        if (localEnd <= localStart)
            return results;

        // Break window in local time (optional)
        DateTime? localBreakStart = null;
        DateTime? localBreakEnd = null;
        if (breakStart.HasValue && breakEnd.HasValue && breakEnd > breakStart)
        {
            localBreakStart = date.ToDateTime(breakStart.Value, DateTimeKind.Unspecified);
            localBreakEnd = date.ToDateTime(breakEnd.Value, DateTimeKind.Unspecified);
        }

        var earliestBookable = nowUtc.AddMinutes(minLeadTimeMinutes);
        var busyList = busy.ToList();

        var cursor = localStart;
        while (true)
        {
            var candidateLocalEnd = cursor.AddMinutes(serviceDurationMinutes);
            if (candidateLocalEnd > localEnd)
                break;

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(cursor, DateTimeKind.Unspecified), tz);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(candidateLocalEnd, DateTimeKind.Unspecified), tz);

            var overlapsBreak = localBreakStart.HasValue
                && cursor < localBreakEnd!.Value
                && candidateLocalEnd > localBreakStart!.Value;

            var overlapsBusy = busyList.Any(b => startUtc < b.EndUtc && endUtc > b.StartUtc);

            var isFuture = startUtc >= earliestBookable;

            if (!overlapsBreak && !overlapsBusy && isFuture)
                results.Add((startUtc, endUtc));

            cursor = cursor.AddMinutes(slotStepMinutes);
        }

        return results;
    }
}
