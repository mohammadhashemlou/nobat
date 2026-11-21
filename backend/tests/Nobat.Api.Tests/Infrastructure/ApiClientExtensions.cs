using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nobat.Api.Tests.Infrastructure;

internal static class ApiClientExtensions
{
    /// <summary>
    /// Mirrors the API's own serializer settings (camelCase + string enums) so DTOs
    /// round-trip exactly the way the browser sees them.
    /// </summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body)
        => client.PostAsJsonAsync(url, body, Json);

    public static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string url, T body)
        => client.PutAsJsonAsync(url, body, Json);

    public static Task<HttpResponseMessage> PatchJsonAsync<T>(this HttpClient client, string url, T body)
        => client.PatchAsJsonAsync(url, body, Json);

    public static async Task<T> GetJsonAsync<T>(this HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsync<T>();
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        Assert.NotNull(value);
        return value!;
    }

    /// <summary>Reads the RFC 7807 <c>code</c> extension emitted by the exception middleware.</summary>
    public static async Task<string?> ProblemCodeAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            return null;

        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    /// <summary>Reads the <c>errors[field]</c> array from a ValidationProblemDetails body.</summary>
    public static async Task<string[]> ProblemErrorsAsync(this HttpResponseMessage response, string field)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        if (!document.RootElement.TryGetProperty("errors", out var errors))
            return [];

        // ASP.NET Core emits camelCase keys; match case-insensitively.
        foreach (var property in errors.EnumerateObject())
        {
            if (!string.Equals(property.Name, field, StringComparison.OrdinalIgnoreCase))
                continue;

            return property.Value.EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .ToArray();
        }

        return [];
    }
}
