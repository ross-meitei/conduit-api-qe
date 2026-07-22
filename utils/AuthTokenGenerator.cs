using System.Net.Http.Json;
using System.Text.Json;
using AllureStepAttribute = Allure.Xunit.Attributes.Steps.AllureStepAttribute;

namespace Integration.Tests.utils;

/// <summary>
/// Obtains a RealWorld JWT at runtime. Uses TEST_EMAIL/TEST_PASSWORD when set;
/// otherwise registers a throwaway account against the demo backend. The token is
/// never persisted to source - callers pass it straight into the HttpClient.
/// </summary>
public static class AuthTokenGenerator
{
    [AllureStep("Generate auth token")]
    public static async Task<string> GenerateAuthToken()
    {
        var baseUrl = Environment.GetEnvironmentVariable("BASE_URL")
                      ?? throw new InvalidOperationException("BASE_URL is not set.");
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl, UriKind.Absolute) };

        var email = Environment.GetEnvironmentVariable("TEST_EMAIL");
        var password = Environment.GetEnvironmentVariable("TEST_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return await RegisterThrowawayUser(http);
        }

        var payload = new { user = new { email, password } };
        var response = await http.PostAsJsonAsync("/users/login", payload);
        response.EnsureSuccessStatusCode();
        return ExtractToken(await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> RegisterThrowawayUser(HttpClient http)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            user = new
            {
                username = $"qa_{suffix}",
                email = $"qa_{suffix}@conduit-qe.test",
                password = $"Pw!{suffix}",
            },
        };
        var response = await http.PostAsJsonAsync("/users", payload);
        response.EnsureSuccessStatusCode();
        return ExtractToken(await response.Content.ReadAsStringAsync());
    }

    private static string ExtractToken(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("user").GetProperty("token").GetString()
               ?? throw new InvalidOperationException("No token in auth response.");
    }
}
