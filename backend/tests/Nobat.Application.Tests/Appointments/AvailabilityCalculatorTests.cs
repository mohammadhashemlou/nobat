using Nobat.Application.Modules.Appointments.Services;

namespace Nobat.Application.Tests.Appointments;

/// <summary>
/// The availability engine is pure (no I/O), so every branch of the booking rules
/// can be pinned down deterministically here.
/// </summary>
public class AvailabilityCalculatorTests
{
    private static readonly DateOnly Day = new(2026, 11, 23);
    private static readonly DateTime LongAgo = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly AvailabilityCalculator _sut = new();

    private IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> Calculate(
        TimeOnly workStart,
        TimeOnly workEnd,
        TimeOnly? breakStart = null,
        TimeOnly? breakEnd = null,
        int duration = 45,
        int step = 15,
        IEnumerable<(DateTime, DateTime)>? busy = null,
        DateTime? nowUtc = null,
        int leadTime = 0)
        => _sut.Calculate(
            Day, workStart, workEnd, breakStart, breakEnd,
            duration, step,
            busy ?? [],
            nowUtc ?? LongAgo,
            leadTime,
            TestTimeZones.Tehran);

    private static TimeOnly LocalTime(DateTime utc)
        => TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, TestTimeZones.Tehran));

    // ---------------------------------------------------------------- happy path

    [Fact]
    public void Generate_RespectsWorkingHours_AndExcludesBreakWindow()
    {
        var slots = Calculate(
            new TimeOnly(9, 0), new TimeOnly(18, 0),
            new TimeOnly(13, 0), new TimeOnly(14, 0));

        // 09:00..17:15 step 15 => 34 candidates; 6 of them collide with the 13:00-14:00 break.
        Assert.Equal(28, slots.Count);

        Assert.Equal(new TimeOnly(9, 0), LocalTime(slots[0].StartUtc));
        Assert.Equal(new TimeOnly(17, 15), LocalTime(slots[^1].StartUtc));
    }

    [Fact]
    public void Generate_NeverProducesSlotEndingAfterClosingTime()
    {
        var slots = Calculate(
            new TimeOnly(9, 0), new TimeOnly(18, 0),
            new TimeOnly(13, 0), new TimeOnly(14, 0));

        Assert.All(slots, s =>
        {
            Assert.True(LocalTime(s.StartUtc) >= new TimeOnly(9, 0),
                $"slot starting at {LocalTime(s.StartUtc)} is before opening time");
            Assert.True(LocalTime(s.EndUtc) <= new TimeOnly(18, 0),
                $"slot ending at {LocalTime(s.EndUtc)} is past closing time");
        });
    }

    [Fact]
    public void Generate_AllowsSlotEndingExactlyAtBreakStart()
    {
        var slots = Calculate(
            new TimeOnly(9, 0), new TimeOnly(18, 0),
            new TimeOnly(13, 0), new TimeOnly(14, 0));

        // 12:15 + 45min = 13:00 — touches the break but does not consume it.
        var last = slots.Single(s => LocalTime(s.StartUtc) == new TimeOnly(12, 15));
        Assert.Equal(new TimeOnly(13, 0), LocalTime(last.EndUtc));

        // 12:30 would run 15 minutes into the break.
        Assert.DoesNotContain(slots, s => LocalTime(s.StartUtc) == new TimeOnly(12, 30));
    }

    [Fact]
    public void Generate_AllowsSlotStartingExactlyAtBreakEnd()
    {
        var slots = Calculate(
            new TimeOnly(9, 0), new TimeOnly(18, 0),
            new TimeOnly(13, 0), new TimeOnly(14, 0));

        Assert.Contains(slots, s => LocalTime(s.StartUtc) == new TimeOnly(14, 0));
        Assert.DoesNotContain(slots, s => LocalTime(s.StartUtc) == new TimeOnly(13, 45));
    }

    [Fact]
    public void Generate_ProducesContiguousSlotsWithFixedStep()
    {
        var slots = Calculate(new TimeOnly(10, 0), new TimeOnly(12, 0), duration: 30, step: 15);

        // 10:00, 10:15, 10:30, 10:45, 11:00, 11:15, 11:30  (11:45 + 30 > 12:00)
        Assert.Equal(7, slots.Count);

        for (var i = 1; i < slots.Count; i++)
            Assert.Equal(15, (slots[i].StartUtc - slots[i - 1].StartUtc).TotalMinutes);
    }

    [Fact]
    public void Generate_ReturnsSlotsInUtc()
    {
        var slots = Calculate(new TimeOnly(9, 0), new TimeOnly(10, 0));

        Assert.All(slots, s => Assert.Equal(DateTimeKind.Utc, s.StartUtc.Kind));

        // 09:00 Tehran (+03:30) == 05:30 UTC
        Assert.Equal(new DateTime(2026, 11, 23, 5, 30, 0, DateTimeKind.Utc), slots[0].StartUtc);
        Assert.Equal(new DateTime(2026, 11, 23, 6, 15, 0, DateTimeKind.Utc), slots[0].EndUtc);
    }

    // ---------------------------------------------------------------- busy intervals

    [Fact]
    public void Generate_ExcludesSlotsOverlappingExistingAppointments()
    {
        // Busy 10:00-11:00 local
        var busy = new[] { (TestTimeZones.ToUtc(Day, 10), TestTimeZones.ToUtc(Day, 11)) };

        var slots = Calculate(new TimeOnly(9, 0), new TimeOnly(18, 0), busy: busy);

        Assert.DoesNotContain(slots, s =>
            s.StartUtc < busy[0].Item2 && s.EndUtc > busy[0].Item1);

        // 09:15+45 = 10:00 touches the busy block, so it survives.
        Assert.Contains(slots, s => LocalTime(s.StartUtc) == new TimeOnly(9, 15));
        // 09:30+45 = 10:15 overlaps.
        Assert.DoesNotContain(slots, s => LocalTime(s.StartUtc) == new TimeOnly(9, 30));
        // 11:00 is the first free slot after the block.
        Assert.Contains(slots, s => LocalTime(s.StartUtc) == new TimeOnly(11, 0));
    }

    [Fact]
    public void Generate_HandlesMultipleBusyIntervals()
    {
        var busy = new[]
        {
            (TestTimeZones.ToUtc(Day, 9, 30), TestTimeZones.ToUtc(Day, 10, 0)),
            (TestTimeZones.ToUtc(Day, 15, 0), TestTimeZones.ToUtc(Day, 16, 30))
        };

        var slots = Calculate(new TimeOnly(9, 0), new TimeOnly(18, 0), busy: busy);

        Assert.All(slots, s => Assert.All(busy, b =>
            Assert.False(s.StartUtc < b.Item2 && s.EndUtc > b.Item1,
                $"slot {LocalTime(s.StartUtc)} overlaps busy {LocalTime(b.Item1)}")));

        // 09:00 + 45min = 09:45 runs into the 09:30 block, so the day opens at 10:00.
        Assert.DoesNotContain(slots, s => LocalTime(s.StartUtc) == new TimeOnly(9, 0));
        Assert.Equal(new TimeOnly(10, 0), LocalTime(slots[0].StartUtc));

        // 16:30 starts exactly when the second block ends.
        Assert.Contains(slots, s => LocalTime(s.StartUtc) == new TimeOnly(16, 30));
    }

    [Fact]
    public void Generate_ReturnsNothing_WhenWholeDayIsBusy()
    {
        var busy = new[] { (TestTimeZones.ToUtc(Day, 8), TestTimeZones.ToUtc(Day, 20)) };

        var slots = Calculate(new TimeOnly(9, 0), new TimeOnly(18, 0), busy: busy);

        Assert.Empty(slots);
    }

    // ---------------------------------------------------------------- lead time / past

    [Fact]
    public void Generate_DropsSlotsInsideMinimumLeadTime()
    {
        // "now" is 12:00 local, 60 minute lead => first bookable slot is 13:00 local
        var nowUtc = TestTimeZones.ToUtc(Day, 12);

        var slots = Calculate(
            new TimeOnly(9, 0), new TimeOnly(18, 0),
            nowUtc: nowUtc, leadTime: 60);

        Assert.All(slots, s => Assert.True(s.StartUtc >= nowUtc.AddMinutes(60)));
        Assert.Equal(new TimeOnly(13, 0), LocalTime(slots[0].StartUtc));
    }

    [Fact]
    public void Generate_ReturnsNothing_WhenDayIsAlreadyOver()
    {
        var nowUtc = TestTimeZones.ToUtc(Day, 19);

        var slots = Calculate(new TimeOnly(9, 0), new TimeOnly(18, 0), nowUtc: nowUtc, leadTime: 60);

        Assert.Empty(slots);
    }

    [Fact]
    public void Generate_IncludesSlotExactlyAtLeadTimeBoundary()
    {
        var nowUtc = TestTimeZones.ToUtc(Day, 12);

        var slots = Calculate(
            new TimeOnly(9, 0), new TimeOnly(18, 0),
            nowUtc: nowUtc, leadTime: 60);

        // 13:00 local == exactly now + 60min, so it is bookable (>= comparison).
        Assert.Contains(slots, s => s.StartUtc == nowUtc.AddMinutes(60));
    }

    // ---------------------------------------------------------------- degenerate input

    [Theory]
    [InlineData(0, 15)]
    [InlineData(-30, 15)]
    [InlineData(45, 0)]
    [InlineData(45, -15)]
    public void Generate_ReturnsEmpty_ForNonPositiveDurationOrStep(int duration, int step)
    {
        var slots = Calculate(new TimeOnly(9, 0), new TimeOnly(18, 0), duration: duration, step: step);

        Assert.Empty(slots);
    }

    [Fact]
    public void Generate_ReturnsEmpty_WhenClosingTimeIsBeforeOpeningTime()
    {
        var slots = Calculate(new TimeOnly(18, 0), new TimeOnly(9, 0));

        Assert.Empty(slots);
    }

    [Fact]
    public void Generate_ReturnsEmpty_WhenClosingTimeEqualsOpeningTime()
    {
        var slots = Calculate(new TimeOnly(9, 0), new TimeOnly(9, 0));

        Assert.Empty(slots);
    }

    [Fact]
    public void Generate_ReturnsEmpty_WhenServiceIsLongerThanTheWorkingDay()
    {
        var slots = Calculate(new TimeOnly(9, 0), new TimeOnly(10, 0), duration: 120);

        Assert.Empty(slots);
    }

    [Theory]
    [InlineData(null, "14:00")] // only break end
    [InlineData("13:00", null)] // only break start
    public void Generate_IgnoresIncompleteBreakDefinition(string? breakStart, string? breakEnd)
    {
        var slots = Calculate(
            new TimeOnly(9, 0), new TimeOnly(18, 0),
            breakStart is null ? null : TimeOnly.Parse(breakStart),
            breakEnd is null ? null : TimeOnly.Parse(breakEnd));

        // Without a complete break window the engine falls back to a continuous day.
        Assert.Equal(34, slots.Count);
    }

    [Fact]
    public void Generate_IgnoresBreak_WhenBreakEndIsBeforeBreakStart()
    {
        var slots = Calculate(
            new TimeOnly(9, 0), new TimeOnly(18, 0),
            new TimeOnly(14, 0), new TimeOnly(13, 0));

        Assert.Equal(34, slots.Count);
    }

    [Fact]
    public void Generate_HandlesSingleSlotDay()
    {
        var slots = Calculate(new TimeOnly(9, 0), new TimeOnly(9, 45), duration: 45, step: 15);

        Assert.Single(slots);
        Assert.Equal(new TimeOnly(9, 0), LocalTime(slots[0].StartUtc));
        Assert.Equal(new TimeOnly(9, 45), LocalTime(slots[0].EndUtc));
    }
}
