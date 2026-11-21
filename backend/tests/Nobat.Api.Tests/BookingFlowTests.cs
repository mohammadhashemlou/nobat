using System.Net;
using System.Net.Http.Json;
using Nobat.Api.Tests.Infrastructure;
using Nobat.Application.Modules.Appointments.DTOs;
using Nobat.Application.Modules.Barbers.DTOs;
using Nobat.Application.Modules.Services.DTOs;

namespace Nobat.Api.Tests;

/// <summary>
/// End-to-end coverage of the booking engine: availability generation, conflict
/// prevention, cancellation and every rejection path.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class BookingFlowTests
{
    /// <summary>Monotonic per-run day offset so no two tests share a date (holidays are per salon+date).</summary>
    private static int _dayOffset = 3;

    private readonly NobatApiFixture _fixture;

    public BookingFlowTests(NobatApiFixture fixture) => _fixture = fixture;

    private static DateOnly NextDate()
        => NobatApiFixture.TehranToday.AddDays(Interlocked.Increment(ref _dayOffset));

    /// <summary>Creates a service + barber and a 09:00-18:00 shift with a 13:00-14:00 break.</summary>
    private async Task<(BarberDto Barber, ServiceDto Service, DateOnly Date)> ArrangeWorkingDayAsync(
        int durationMinutes = 45)
    {
        var service = await _fixture.CreateServiceAsync(durationMinutes: durationMinutes);
        var barber = await _fixture.CreateBarberAsync([service.Id]);
        var date = NextDate();
        await _fixture.SetWorkingHoursAsync(barber.Id, NobatApiFixture.IranianDayOfWeek(date));
        return (barber, service, date);
    }

    private HttpClient ClientWith(string token)
    {
        var client = _fixture.Factory.CreateClient();
        client.WithToken(token);
        return client;
    }

    private async Task<AvailabilityDto> GetAvailabilityAsync(Guid barberId, Guid serviceId, DateOnly date)
    {
        using var client = _fixture.Factory.CreateClient();
        var url = $"/api/v1/appointments/availability?barberId={barberId}&serviceId={serviceId}&date={date:yyyy-MM-dd}";
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<AvailabilityDto>();
    }

    private static object BookingBody(Guid barberId, Guid serviceId, DateTime startUtc, Guid? customerId = null)
        => new { barberId, serviceId, startAtUtc = startUtc, notes = (string?)null, customerId };

    // ---------------------------------------------------------------- availability

    [RequiresDatabaseFact]
    public async Task Availability_ReturnsSlotsInsideWorkingHours_ExcludingTheBreak()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();

        var availability = await GetAvailabilityAsync(barber.Id, service.Id, date);

        Assert.True(availability.IsWorkingDay);
        Assert.False(availability.IsHoliday);

        // 09:00-18:00 step 15 with a 45 min service => 34 candidates, 6 lost to the break.
        Assert.Equal(28, availability.Slots.Count);
        Assert.Equal("09:00", availability.Slots[0].LocalStart);
        Assert.Equal("17:15", availability.Slots[^1].LocalStart);

        // Nothing may straddle or sit inside 13:00-14:00.
        Assert.All(availability.Slots, slot =>
        {
            var start = TimeOnly.Parse(slot.LocalStart);
            var end = TimeOnly.Parse(slot.LocalEnd);
            Assert.False(start < new TimeOnly(14, 0) && end > new TimeOnly(13, 0),
                $"slot {slot.LocalStart}-{slot.LocalEnd} overlaps the break");
        });

        // 12:15 ends exactly at 13:00 and must survive.
        Assert.Contains(availability.Slots, s => s.LocalStart == "12:15" && s.LocalEnd == "13:00");
    }

    [RequiresDatabaseFact]
    public async Task Availability_OnNonWorkingDay_ReturnsNoSlots()
    {
        var service = await _fixture.CreateServiceAsync();
        var barber = await _fixture.CreateBarberAsync([service.Id]);
        var workingDate = NextDate();
        await _fixture.SetWorkingHoursAsync(barber.Id, NobatApiFixture.IranianDayOfWeek(workingDate));

        // The very next day always has a different weekday, so no shift is configured.
        var availability = await GetAvailabilityAsync(barber.Id, service.Id, workingDate.AddDays(1));

        Assert.False(availability.IsWorkingDay);
        Assert.Empty(availability.Slots);
    }

    [RequiresDatabaseFact]
    public async Task Availability_OnHoliday_FlagsTheDay()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        await _fixture.AddHolidayAsync(date, "تعطیلی تست");

        var availability = await GetAvailabilityAsync(barber.Id, service.Id, date);

        Assert.True(availability.IsHoliday);
        Assert.Equal("تعطیلی تست", availability.HolidayDescription);
        Assert.Empty(availability.Slots);
    }

    // ---------------------------------------------------------------- booking

    [RequiresDatabaseFact]
    public async Task Booking_ConfirmsAppointment_AndHidesTheSlot()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        var (token, customerId, _) = await _fixture.CreateCustomerAsync();
        using var client = ClientWith(token);

        var startUtc = NobatApiFixture.ToUtc(date, 10, 0);
        var response = await client.PostAsJsonAsync(
            "/api/v1/appointments", BookingBody(barber.Id, service.Id, startUtc));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.ReadAsync<AppointmentDto>();
        Assert.Equal(customerId, created.CustomerId);
        Assert.Equal("Confirmed", created.Status);
        Assert.Equal(startUtc, created.StartAt);
        Assert.Equal(startUtc.AddMinutes(45), created.EndAt);
        Assert.Equal(45, created.DurationMinutes);

        var availability = await GetAvailabilityAsync(barber.Id, service.Id, date);

        // A 45 minute booking at 10:00 blocks every candidate that would overlap it:
        // 09:30, 09:45, 10:00, 10:15 and 10:30 -> 28 - 5 = 23 remaining.
        Assert.Equal(23, availability.Slots.Count);
        Assert.DoesNotContain(availability.Slots, s => s.StartUtc == startUtc);
        Assert.DoesNotContain(availability.Slots, s => s.StartUtc == startUtc.AddMinutes(15));
        Assert.DoesNotContain(availability.Slots, s => s.StartUtc == startUtc.AddMinutes(-15));

        // 09:15 ends exactly when the booking starts, and 10:45 starts exactly when it ends.
        Assert.Contains(availability.Slots, s => s.StartUtc == startUtc.AddMinutes(-45));
        Assert.Contains(availability.Slots, s => s.StartUtc == startUtc.AddMinutes(45));
    }

    [RequiresDatabaseFact]
    public async Task Booking_TheSameSlotTwice_Returns409()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        var (firstToken, _, _) = await _fixture.CreateCustomerAsync();
        var (secondToken, _, _) = await _fixture.CreateCustomerAsync();

        var startUtc = NobatApiFixture.ToUtc(date, 11, 0);

        using var first = ClientWith(firstToken);
        var ok = await first.PostAsJsonAsync("/api/v1/appointments", BookingBody(barber.Id, service.Id, startUtc));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);

        using var second = ClientWith(secondToken);
        var conflict = await second.PostAsJsonAsync("/api/v1/appointments", BookingBody(barber.Id, service.Id, startUtc));

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("SLOT_ALREADY_BOOKED", await conflict.ProblemCodeAsync());
    }

    [RequiresDatabaseFact]
    public async Task Booking_AnOverlappingSlot_Returns409()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var client = ClientWith(token);

        var startUtc = NobatApiFixture.ToUtc(date, 11, 0);
        var first = await client.PostAsJsonAsync("/api/v1/appointments", BookingBody(barber.Id, service.Id, startUtc));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // 15 minutes later still overlaps the 45 minute appointment.
        var overlap = await client.PostAsJsonAsync(
            "/api/v1/appointments",
            BookingBody(barber.Id, service.Id, startUtc.AddMinutes(15)));

        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        Assert.Equal("SLOT_ALREADY_BOOKED", await overlap.ProblemCodeAsync());
    }

    [RequiresDatabaseFact]
    public async Task Booking_BackToBackSlot_IsAllowed()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var client = ClientWith(token);

        var startUtc = NobatApiFixture.ToUtc(date, 11, 0);
        var first = await client.PostAsJsonAsync("/api/v1/appointments", BookingBody(barber.Id, service.Id, startUtc));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Starts exactly when the previous one ends — must not be treated as a conflict.
        var adjacent = await client.PostAsJsonAsync(
            "/api/v1/appointments",
            BookingBody(barber.Id, service.Id, startUtc.AddMinutes(45)));

        Assert.Equal(HttpStatusCode.Created, adjacent.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task Cancelling_FreesTheSlot_AndAllowsRebooking()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var client = ClientWith(token);

        var startUtc = NobatApiFixture.ToUtc(date, 15, 0);
        var created = await client.PostAsJsonAsync("/api/v1/appointments", BookingBody(barber.Id, service.Id, startUtc));
        var appointment = await created.ReadAsync<AppointmentDto>();

        var cancel = await client.PostAsync($"/api/v1/appointments/{appointment.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var availability = await GetAvailabilityAsync(barber.Id, service.Id, date);
        Assert.Contains(availability.Slots, s => s.StartUtc == startUtc);

        var rebook = await client.PostAsJsonAsync("/api/v1/appointments", BookingBody(barber.Id, service.Id, startUtc));
        Assert.Equal(HttpStatusCode.Created, rebook.StatusCode);
    }

    // ---------------------------------------------------------------- rejection paths

    [RequiresDatabaseFact]
    public async Task Booking_InThePast_Returns400()
    {
        var (barber, service, _) = await ArrangeWorkingDayAsync();
        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var client = ClientWith(token);

        var yesterday = NobatApiFixture.TehranToday.AddDays(-1);
        var response = await client.PostAsJsonAsync(
            "/api/v1/appointments",
            BookingBody(barber.Id, service.Id, NobatApiFixture.ToUtc(yesterday, 10, 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("PAST_TIME", await response.ProblemCodeAsync());
    }

    [RequiresDatabaseFact]
    public async Task Booking_OnAHoliday_Returns400()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        await _fixture.AddHolidayAsync(date);
        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var client = ClientWith(token);

        var response = await client.PostAsJsonAsync(
            "/api/v1/appointments",
            BookingBody(barber.Id, service.Id, NobatApiFixture.ToUtc(date, 10, 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("HOLIDAY", await response.ProblemCodeAsync());
    }

    [RequiresDatabaseFact]
    public async Task Booking_OutsideWorkingHours_Returns400()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var client = ClientWith(token);

        // 20:00 local, the shift ends at 18:00.
        var response = await client.PostAsJsonAsync(
            "/api/v1/appointments",
            BookingBody(barber.Id, service.Id, NobatApiFixture.ToUtc(date, 20, 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("OUTSIDE_WORKING_HOURS", await response.ProblemCodeAsync());
    }

    [RequiresDatabaseFact]
    public async Task Booking_DuringTheBreak_Returns400()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var client = ClientWith(token);

        // 13:15 sits inside the 13:00-14:00 break.
        var response = await client.PostAsJsonAsync(
            "/api/v1/appointments",
            BookingBody(barber.Id, service.Id, NobatApiFixture.ToUtc(date, 13, 15)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("BREAK_OVERLAP", await response.ProblemCodeAsync());
    }

    [RequiresDatabaseFact]
    public async Task Booking_OnANonWorkingDay_Returns400()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var client = ClientWith(token);

        var response = await client.PostAsJsonAsync(
            "/api/v1/appointments",
            BookingBody(barber.Id, service.Id, NobatApiFixture.ToUtc(date.AddDays(1), 10, 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("NOT_WORKING_DAY", await response.ProblemCodeAsync());
    }

    [RequiresDatabaseFact]
    public async Task Customer_CannotBookOnBehalfOfSomeoneElse()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        var (token, _, _) = await _fixture.CreateCustomerAsync();
        var (_, otherCustomerId, _) = await _fixture.CreateCustomerAsync();
        using var client = ClientWith(token);

        var response = await client.PostAsJsonAsync(
            "/api/v1/appointments",
            BookingBody(barber.Id, service.Id, NobatApiFixture.ToUtc(date, 10, 0), otherCustomerId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("FORBIDDEN", await response.ProblemCodeAsync());
    }

    [RequiresDatabaseFact]
    public async Task Booking_WithoutAuthentication_Returns401()
    {
        var (barber, service, date) = await ArrangeWorkingDayAsync();
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/appointments",
            BookingBody(barber.Id, service.Id, NobatApiFixture.ToUtc(date, 10, 0)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
