using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Nobat.Application.Modules.Barbers.DTOs;
using Nobat.Application.Modules.Identity.DTOs;
using Nobat.Application.Modules.Salons.DTOs;
using Nobat.Application.Modules.Services.DTOs;

namespace Nobat.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real API against a throw-away PostgreSQL database and exposes helpers
/// that provision a salon, service, barber and working hours through the public API
/// (so the seeding itself exercises the endpoints).
/// </summary>
public sealed class NobatApiFixture : IAsyncLifetime
{
    public const string AdminPhoneNumber = "09120000000";
    public const string AdminPassword = "Admin@123456";

    private string _database = string.Empty;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public string AdminToken { get; private set; } = string.Empty;

    public Guid SalonId { get; private set; }

    /// <summary>Direct connection string to the throw-away database (for constraint tests).</summary>
    public string ConnectionString => TestDatabase.BuildConnectionString(_database);

    /// <summary>Today in the business timezone (Asia/Tehran).</summary>
    public static DateOnly TehranToday
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TehranZone));

    public static TimeZoneInfo TehranZone { get; } = ResolveTehran();

    /// <summary>A mobile number that is unique within a test run.</summary>
    public static string UniquePhone()
        => "09" + Random.Shared.Next(100_000_000, 999_999_999);

    public async Task InitializeAsync()
    {
        if (!TestDatabase.IsAvailable)
            return;

        // PostgreSQL identifiers are limited to 63 bytes; a 28-char name keeps us well clear.
        _database = ("nobat_test_" + Guid.NewGuid().ToString("N"))[..28];
        TestDatabase.Create(_database);

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Default", TestDatabase.BuildConnectionString(_database));
        });

        Client = Factory.CreateClient();
        AdminToken = await SignInAsync(AdminPhoneNumber, AdminPassword);
        Client.WithToken(AdminToken);

        var salon = await Client.GetJsonAsync<SalonDto>("/api/v1/salons/default");
        Assert.NotNull(salon);
        SalonId = salon!.Id;
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();

        if (!string.IsNullOrEmpty(_database))
            TestDatabase.Drop(_database);

        return Task.CompletedTask;
    }

    public async Task<string> SignInAsync(string phoneNumber, string password)
    {
        using var anonymous = Factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { phoneNumber, password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.ReadAsync<LoginResponse>();
        return payload.AccessToken;
    }

    /// <summary>Registers a fresh customer and returns (token, userId, phoneNumber).</summary>
    public async Task<(string Token, Guid UserId, string PhoneNumber)> CreateCustomerAsync(string? phoneNumber = null)
    {
        phoneNumber ??= UniquePhone();

        using var anonymous = Factory.CreateClient();
        var register = await anonymous.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "مشتری تست",
            phoneNumber,
            password = "Test@12345",
            confirmPassword = "Test@12345",
            role = 2 // Customer
        });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var created = await register.ReadAsync<RegisterResponse>();

        return (await SignInAsync(phoneNumber, "Test@12345"), created.UserId, phoneNumber);
    }

    public async Task<ServiceDto> CreateServiceAsync(
        string name = "اصلاح سر",
        int durationMinutes = 45,
        decimal price = 250_000m)
    {
        Client.WithToken(AdminToken);

        var response = await Client.PostAsJsonAsync("/api/v1/services", new
        {
            salonId = SalonId,
            name,
            description = "خدمت تست",
            durationMinutes,
            price
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<ServiceDto>();
    }

    public async Task<BarberDto> CreateBarberAsync(
        IEnumerable<Guid> serviceIds,
        string? phoneNumber = null)
    {
        Client.WithToken(AdminToken);
        phoneNumber ??= UniquePhone();

        var response = await Client.PostAsJsonAsync("/api/v1/barbers", new
        {
            salonId = SalonId,
            fullName = "آرایشگر تست",
            phoneNumber,
            password = "Barber@12345",
            bio = "متخصص اصلاح",
            avatarUrl = (string?)null,
            serviceIds = serviceIds.ToArray()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<BarberDto>();
    }

    /// <summary>Sets one continuous shift (with an optional break) for a single weekday.</summary>
    public async Task SetWorkingHoursAsync(
        Guid barberId,
        int iranianDayOfWeek,
        string start = "09:00",
        string end = "18:00",
        string? breakStart = "13:00",
        string? breakEnd = "14:00")
    {
        Client.WithToken(AdminToken);

        var response = await Client.PutAsJsonAsync($"/api/v1/barbers/{barberId}/working-hours", new
        {
            items = new[]
            {
                new
                {
                    dayOfWeek = iranianDayOfWeek,
                    startTime = start,
                    endTime = end,
                    breakStart,
                    breakEnd
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public async Task AddHolidayAsync(DateOnly date, string? description = "تعطیلات رسمی")
    {
        Client.WithToken(AdminToken);

        var response = await Client.PostAsJsonAsync("/api/v1/holidays", new
        {
            salonId = SalonId,
            date = date.ToString("yyyy-MM-dd"),
            description
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>Maps a .NET weekday to the Iranian week (Saturday = 0 ... Friday = 6).</summary>
    public static int IranianDayOfWeek(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Saturday => 0,
        DayOfWeek.Sunday => 1,
        DayOfWeek.Monday => 2,
        DayOfWeek.Tuesday => 3,
        DayOfWeek.Wednesday => 4,
        DayOfWeek.Thursday => 5,
        _ => 6
    };

    /// <summary>Converts a Tehran wall-clock time on <paramref name="date"/> to UTC.</summary>
    public static DateTime ToUtc(DateOnly date, int hour, int minute = 0)
        => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(hour, minute)), DateTimeKind.Unspecified),
            TehranZone);

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
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Test/Tehran", TimeSpan.FromMinutes(210), "Tehran (fixed)", "Tehran (fixed)");
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<NobatApiFixture>
{
    public const string Name = "Integration";
}
