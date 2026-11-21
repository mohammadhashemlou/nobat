using Nobat.Domain.Enums;
using IranianDayOfWeek = Nobat.Domain.Enums.DayOfWeek;

namespace Nobat.Domain.Tests.Enums;

/// <summary>
/// The Iranian week starts on Saturday. These numeric values are persisted to the
/// database and compared in availability queries, so they must never drift.
/// </summary>
public class IranianDayOfWeekTests
{
    [Theory]
    [InlineData(IranianDayOfWeek.Saturday, 0)]
    [InlineData(IranianDayOfWeek.Sunday, 1)]
    [InlineData(IranianDayOfWeek.Monday, 2)]
    [InlineData(IranianDayOfWeek.Tuesday, 3)]
    [InlineData(IranianDayOfWeek.Wednesday, 4)]
    [InlineData(IranianDayOfWeek.Thursday, 5)]
    [InlineData(IranianDayOfWeek.Friday, 6)]
    public void PersianWeekOrder_IsStable(IranianDayOfWeek day, int expected)
    {
        Assert.Equal(expected, (int)day);
    }

    [Fact]
    public void WeekHasSevenDistinctDays()
    {
        var values = Enum.GetValues<IranianDayOfWeek>();

        Assert.Equal(7, values.Length);
        Assert.Equal(7, values.Distinct().Count());
    }
}

public class UserRoleTests
{
    [Theory]
    [InlineData(UserRole.Admin, 0)]
    [InlineData(UserRole.Barber, 1)]
    [InlineData(UserRole.Customer, 2)]
    public void RoleValues_AreStable(UserRole role, int expected)
    {
        Assert.Equal(expected, (int)role);
    }
}

public class AppointmentStatusTests
{
    [Fact]
    public void AllExpectedStatusesExist()
    {
        var names = Enum.GetNames<AppointmentStatus>();

        Assert.Contains(nameof(AppointmentStatus.Pending), names);
        Assert.Contains(nameof(AppointmentStatus.Confirmed), names);
        Assert.Contains(nameof(AppointmentStatus.Completed), names);
        Assert.Contains(nameof(AppointmentStatus.Cancelled), names);
        Assert.Contains(nameof(AppointmentStatus.NoShow), names);
    }

    [Fact]
    public void DefaultStatusIsPending()
    {
        Assert.Equal(0, (int)AppointmentStatus.Pending);
    }
}
