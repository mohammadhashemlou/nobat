using System.Net;
using System.Net.Http.Json;
using Nobat.Api.Tests.Infrastructure;
using Nobat.Application.Modules.Appointments.DTOs;

namespace Nobat.Api.Tests;

[Collection(IntegrationCollection.Name)]
public class AuthorizationTests
{
    private static int _dayOffset = 200;

    private readonly NobatApiFixture _fixture;

    public AuthorizationTests(NobatApiFixture fixture) => _fixture = fixture;

    private static DateOnly NextDate()
        => NobatApiFixture.TehranToday.AddDays(Interlocked.Increment(ref _dayOffset));

    [RequiresDatabaseFact]
    public async Task Customer_OnlySeesTheirOwnAppointments()
    {
        var service = await _fixture.CreateServiceAsync();
        var barber = await _fixture.CreateBarberAsync([service.Id]);
        var date = NextDate();
        await _fixture.SetWorkingHoursAsync(barber.Id, NobatApiFixture.IranianDayOfWeek(date));

        var (tokenA, customerA, _) = await _fixture.CreateCustomerAsync();
        var (tokenB, customerB, _) = await _fixture.CreateCustomerAsync();

        using var clientA = _fixture.Factory.CreateClient();
        clientA.WithToken(tokenA);
        var bookedA = await clientA.PostAsJsonAsync("/api/v1/appointments", new
        {
            barberId = barber.Id,
            serviceId = service.Id,
            startAtUtc = NobatApiFixture.ToUtc(date, 10, 0)
        });
        Assert.Equal(HttpStatusCode.Created, bookedA.StatusCode);

        using var clientB = _fixture.Factory.CreateClient();
        clientB.WithToken(tokenB);
        var bookedB = await clientB.PostAsJsonAsync("/api/v1/appointments", new
        {
            barberId = barber.Id,
            serviceId = service.Id,
            startAtUtc = NobatApiFixture.ToUtc(date, 11, 0)
        });
        Assert.Equal(HttpStatusCode.Created, bookedB.StatusCode);

        var listA = await clientA.GetJsonAsync<List<AppointmentDto>>(
            $"/api/v1/appointments?barberId={barber.Id}");
        Assert.NotNull(listA);
        Assert.All(listA!, a => Assert.Equal(customerA, a.CustomerId));
        Assert.Single(listA!);

        var listB = await clientB.GetJsonAsync<List<AppointmentDto>>(
            $"/api/v1/appointments?barberId={barber.Id}");
        Assert.NotNull(listB);
        Assert.All(listB!, a => Assert.Equal(customerB, a.CustomerId));
        Assert.Single(listB!);

        // The admin sees both.
        using var admin = _fixture.Factory.CreateClient();
        admin.WithToken(_fixture.AdminToken);
        var adminList = await admin.GetJsonAsync<List<AppointmentDto>>(
            $"/api/v1/appointments?barberId={barber.Id}");

        Assert.NotNull(adminList);
        Assert.Equal(2, adminList!.Count);
    }

    [RequiresDatabaseFact]
    public async Task Customer_CannotChangeAppointmentStatus()
    {
        var service = await _fixture.CreateServiceAsync();
        var barber = await _fixture.CreateBarberAsync([service.Id]);
        var date = NextDate();
        await _fixture.SetWorkingHoursAsync(barber.Id, NobatApiFixture.IranianDayOfWeek(date));

        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var client = _fixture.Factory.CreateClient();
        client.WithToken(token);

        var created = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            barberId = barber.Id,
            serviceId = service.Id,
            startAtUtc = NobatApiFixture.ToUtc(date, 10, 0)
        });
        var appointment = await created.ReadAsync<AppointmentDto>();

        var patch = await client.PatchAsJsonAsync(
            $"/api/v1/appointments/{appointment.Id}/status",
            new { status = 2 }); // Completed

        Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task Customer_CannotCreateServices()
    {
        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var client = _fixture.Factory.CreateClient();
        client.WithToken(token);

        var response = await client.PostAsJsonAsync("/api/v1/services", new
        {
            salonId = _fixture.SalonId,
            name = "خدمت غیرمجاز",
            description = (string?)null,
            durationMinutes = 30,
            price = 100_000m
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task Anonymous_CannotListAppointments()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/appointments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task Admin_CanMoveAnAppointmentThroughItsLifecycle()
    {
        var service = await _fixture.CreateServiceAsync();
        var barber = await _fixture.CreateBarberAsync([service.Id]);
        var date = NextDate();
        await _fixture.SetWorkingHoursAsync(barber.Id, NobatApiFixture.IranianDayOfWeek(date));

        var (token, _, _) = await _fixture.CreateCustomerAsync();
        using var customer = _fixture.Factory.CreateClient();
        customer.WithToken(token);

        var created = await customer.PostAsJsonAsync("/api/v1/appointments", new
        {
            barberId = barber.Id,
            serviceId = service.Id,
            startAtUtc = NobatApiFixture.ToUtc(date, 10, 0)
        });
        var appointment = await created.ReadAsync<AppointmentDto>();

        using var admin = _fixture.Factory.CreateClient();
        admin.WithToken(_fixture.AdminToken);

        var complete = await admin.PatchAsJsonAsync(
            $"/api/v1/appointments/{appointment.Id}/status",
            new { status = 2 }); // Completed
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);

        var list = await admin.GetJsonAsync<List<AppointmentDto>>(
            $"/api/v1/appointments?barberId={barber.Id}");
        var updated = Assert.Single(list!, a => a.Id == appointment.Id);
        Assert.Equal("Completed", updated.Status);
    }

    [RequiresDatabaseFact]
    public async Task Health_EndpointReportsHealthy()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [RequiresDatabaseFact]
    public async Task PublicSalonPage_IsAnonymous_AndExposesCatalog()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/salons/default");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
