using System.Net;
using System.Net.Http.Json;
using Npgsql;
using Nobat.Api.Tests.Infrastructure;
using Nobat.Application.Modules.Barbers.DTOs;
using Nobat.Application.Modules.Services.DTOs;

namespace Nobat.Api.Tests;

/// <summary>
/// Proves that double-booking is impossible even when the application-level check is
/// bypassed entirely: the PostgreSQL exclusion constraint must reject the row.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class DatabaseConstraintTests
{
    private static int _dayOffset = 100;

    private readonly NobatApiFixture _fixture;

    public DatabaseConstraintTests(NobatApiFixture fixture) => _fixture = fixture;

    private async Task<(BarberDto Barber, ServiceDto Service, DateOnly Date)> ArrangeAsync()
    {
        var service = await _fixture.CreateServiceAsync();
        var barber = await _fixture.CreateBarberAsync([service.Id]);
        var date = NobatApiFixture.TehranToday.AddDays(Interlocked.Increment(ref _dayOffset));
        await _fixture.SetWorkingHoursAsync(barber.Id, NobatApiFixture.IranianDayOfWeek(date));
        return (barber, service, date);
    }

    private async Task InsertAppointmentAsync(
        Guid salonId,
        Guid barberId,
        Guid customerId,
        Guid serviceId,
        DateTime startUtc,
        DateTime endUtc,
        int status)
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "Appointments"
                ("Id", "SalonId", "BarberId", "CustomerId", "ServiceId", "StartAt", "EndAt", "Status", "CreatedAt")
            VALUES
                (@id, @salonId, @barberId, @customerId, @serviceId, @startAt, @endAt, @status, @createdAt)
            """;
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("salonId", salonId);
        command.Parameters.AddWithValue("barberId", barberId);
        command.Parameters.AddWithValue("customerId", customerId);
        command.Parameters.AddWithValue("serviceId", serviceId);
        command.Parameters.AddWithValue("startAt", startUtc);
        command.Parameters.AddWithValue("endAt", endUtc);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();
    }

    [RequiresDatabaseFact]
    public async Task ExclusionConstraint_RejectsOverlappingRows_EvenWithoutTheApplicationCheck()
    {
        var (barber, service, date) = await ArrangeAsync();
        var (_, customerId, _) = await _fixture.CreateCustomerAsync();

        var start = NobatApiFixture.ToUtc(date, 10, 0);

        await InsertAppointmentAsync(
            _fixture.SalonId, barber.Id, customerId, service.Id, start, start.AddMinutes(45), 1);

        // Same interval, different row — the database itself must refuse it.
        var ex = await Assert.ThrowsAsync<PostgresException>(() => InsertAppointmentAsync(
            _fixture.SalonId, barber.Id, customerId, service.Id, start, start.AddMinutes(45), 1));

        Assert.Equal(PostgresErrorCodes.ExclusionViolation, ex.SqlState);
        Assert.Contains("ck_appointments_nooverlap", ex.ConstraintName);
    }

    [RequiresDatabaseFact]
    public async Task ExclusionConstraint_RejectsPartialOverlap()
    {
        var (barber, service, date) = await ArrangeAsync();
        var (_, customerId, _) = await _fixture.CreateCustomerAsync();

        var start = NobatApiFixture.ToUtc(date, 12, 0);
        await InsertAppointmentAsync(
            _fixture.SalonId, barber.Id, customerId, service.Id, start, start.AddMinutes(45), 1);

        // Starts 15 minutes into the existing appointment.
        var ex = await Assert.ThrowsAsync<PostgresException>(() => InsertAppointmentAsync(
            _fixture.SalonId, barber.Id, customerId, service.Id, start.AddMinutes(15), start.AddMinutes(60), 1));

        Assert.Equal(PostgresErrorCodes.ExclusionViolation, ex.SqlState);
    }

    [RequiresDatabaseFact]
    public async Task ExclusionConstraint_AllowsBackToBackRows()
    {
        var (barber, service, date) = await ArrangeAsync();
        var (_, customerId, _) = await _fixture.CreateCustomerAsync();

        var start = NobatApiFixture.ToUtc(date, 15, 0);
        await InsertAppointmentAsync(
            _fixture.SalonId, barber.Id, customerId, service.Id, start, start.AddMinutes(45), 1);

        // Touching but not overlapping — must succeed.
        await InsertAppointmentAsync(
            _fixture.SalonId, barber.Id, customerId, service.Id, start.AddMinutes(45), start.AddMinutes(90), 1);
    }

    [RequiresDatabaseFact]
    public async Task ExclusionConstraint_IgnoresCancelledAppointments()
    {
        var (barber, service, date) = await ArrangeAsync();
        var (_, customerId, _) = await _fixture.CreateCustomerAsync();

        var start = NobatApiFixture.ToUtc(date, 16, 0);

        // Status 3 == Cancelled: the partial constraint excludes it.
        await InsertAppointmentAsync(
            _fixture.SalonId, barber.Id, customerId, service.Id, start, start.AddMinutes(45), 3);

        await InsertAppointmentAsync(
            _fixture.SalonId, barber.Id, customerId, service.Id, start, start.AddMinutes(45), 1);
    }

    [RequiresDatabaseFact]
    public async Task TheApplication_StillReturns409_WhenTheConstraintWouldFire()
    {
        var (barber, service, date) = await ArrangeAsync();
        var (token, customerId, _) = await _fixture.CreateCustomerAsync();
        var start = NobatApiFixture.ToUtc(date, 11, 0);

        // Bypass the API to plant a row the application never saw.
        await InsertAppointmentAsync(
            _fixture.SalonId, barber.Id, customerId, service.Id, start, start.AddMinutes(45), 1);

        using var client = _fixture.Factory.CreateClient();
        client.WithToken(token);

        var response = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            barberId = barber.Id,
            serviceId = service.Id,
            startAtUtc = start
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("SLOT_ALREADY_BOOKED", await response.ProblemCodeAsync());
    }
}
