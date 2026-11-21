using System.Net;
using System.Net.Http.Json;
using Nobat.Api.Tests.Infrastructure;
using Nobat.Application.Modules.Identity.DTOs;

namespace Nobat.Api.Tests;

[Collection(IntegrationCollection.Name)]
public class AuthEndpointsTests
{
    private readonly NobatApiFixture _fixture;

    public AuthEndpointsTests(NobatApiFixture fixture) => _fixture = fixture;

    private static string UniquePhone() => NobatApiFixture.UniquePhone();

    private HttpClient Anonymous() => _fixture.Factory.CreateClient();

    [RequiresDatabaseFact]
    public async Task Register_ThenLogin_ReturnsTokensAndIdentity()
    {
        var phoneNumber = UniquePhone();
        using var client = Anonymous();

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "سارا محمدی",
            phoneNumber,
            password = "Test@12345",
            confirmPassword = "Test@12345",
            role = 2
        });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var created = await register.ReadAsync<RegisterResponse>();
        Assert.NotEqual(Guid.Empty, created.UserId);
        Assert.Equal(phoneNumber, created.PhoneNumber);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { phoneNumber, password = "Test@12345" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var tokens = await login.ReadAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.Equal("Customer", tokens.User.Role);
        Assert.Equal(created.UserId, tokens.User.Id);
        Assert.Equal(phoneNumber, tokens.User.PhoneNumber);
        Assert.True(tokens.AccessTokenExpiresAt > DateTime.UtcNow);
        Assert.True(tokens.RefreshTokenExpiresAt > tokens.AccessTokenExpiresAt);
    }

    [RequiresDatabaseFact]
    public async Task Register_WithDuplicatePhone_Returns409()
    {
        var phoneNumber = UniquePhone();
        using var client = Anonymous();

        var payload = new
        {
            fullName = "کاربر تکراری",
            phoneNumber,
            password = "Test@12345",
            confirmPassword = "Test@12345",
            role = 2
        };

        var first = await client.PostAsJsonAsync("/api/v1/auth/register", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/v1/auth/register", payload);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("PHONE_EXISTS", await second.ProblemCodeAsync());
    }

    [RequiresDatabaseTheory]
    [InlineData("")]
    [InlineData("0912345")]
    [InlineData("09123456789012")]
    [InlineData("9912345678")]
    [InlineData("0912345678a")]
    [InlineData("+989123456789")]
    public async Task Register_WithInvalidPhone_Returns400(string phoneNumber)
    {
        using var client = Anonymous();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "کاربر تست",
            phoneNumber,
            password = "Test@12345",
            confirmPassword = "Test@12345",
            role = 2
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEmpty(await response.ProblemErrorsAsync("PhoneNumber"));
    }

    [RequiresDatabaseFact]
    public async Task Register_WithWeakPassword_ReturnsStructuredValidationErrors()
    {
        using var client = Anonymous();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "کاربر ضعیف",
            phoneNumber = UniquePhone(),
            password = "abc",
            confirmPassword = "abc",
            role = 2
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await response.ProblemCodeAsync());

        var passwordErrors = await response.ProblemErrorsAsync("Password");
        Assert.NotEmpty(passwordErrors);
        Assert.Contains(passwordErrors, m => m.Contains("رمز عبور"));
    }

    [RequiresDatabaseFact]
    public async Task Register_WithMismatchedConfirmation_Returns400()
    {
        using var client = Anonymous();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "کاربر تست",
            phoneNumber = UniquePhone(),
            password = "Test@12345",
            confirmPassword = "Different@12345",
            role = 2
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEmpty(await response.ProblemErrorsAsync("ConfirmPassword"));
    }

    [RequiresDatabaseFact]
    public async Task Register_AsAdmin_IsRejected()
    {
        using var client = Anonymous();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "مدیر جعلی",
            phoneNumber = UniquePhone(),
            password = "Test@12345",
            confirmPassword = "Test@12345",
            role = 0 // Admin
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEmpty(await response.ProblemErrorsAsync("Role"));
    }

    [RequiresDatabaseFact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var phoneNumber = UniquePhone();
        using var client = Anonymous();

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "کاربر تست",
            phoneNumber,
            password = "Test@12345",
            confirmPassword = "Test@12345",
            role = 2
        });

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { phoneNumber, password = "Wrong@12345" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task Login_WithUnknownPhone_Returns401()
    {
        using var client = Anonymous();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { phoneNumber = UniquePhone(), password = "Test@12345" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task Me_WithoutToken_Returns401()
    {
        using var client = Anonymous();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [RequiresDatabaseFact]
    public async Task Me_WithToken_ReturnsCurrentIdentity()
    {
        var (token, userId, phoneNumber) = await _fixture.CreateCustomerAsync();
        using var client = Anonymous();
        client.WithToken(token);

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsync<Dictionary<string, object>>();
        Assert.Equal(phoneNumber, body["phoneNumber"].ToString());
        Assert.Equal("Customer", body["role"].ToString());
        Assert.Equal(userId.ToString(), body["id"].ToString());
    }

    [RequiresDatabaseFact]
    public async Task Refresh_RotatesToken_AndRevokesTheOldOne()
    {
        var phoneNumber = UniquePhone();
        using var client = Anonymous();

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "کاربر تست",
            phoneNumber,
            password = "Test@12345",
            confirmPassword = "Test@12345",
            role = 2
        });

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { phoneNumber, password = "Test@12345" });
        var tokens = await login.ReadAsync<LoginResponse>();

        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var renewed = await refresh.ReadAsync<RefreshTokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(renewed.AccessToken));
        Assert.NotEqual(tokens.RefreshToken, renewed.RefreshToken);

        var replay = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal("INVALID_REFRESH_TOKEN", await replay.ProblemCodeAsync());
    }

    [RequiresDatabaseFact]
    public async Task Refresh_WithGarbageToken_Returns401()
    {
        using var client = Anonymous();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "not-a-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
