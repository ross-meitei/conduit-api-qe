using ConduitApiClientNamespace;
using AllureStepAttribute = Allure.Xunit.Attributes.Steps.AllureStepAttribute;

namespace Integration.Tests.utils;

/// <summary>
/// Builds the NSwag-generated <see cref="ConduitApiClient"/> pointed at the
/// configured base URL, with a RealWorld "Token" auth header attached. Mirrors
/// the client-factory pattern used in the Keystone integration suite.
/// </summary>
public static class ApiClientFactory
{
    [AllureStep("Create API client and load environment")]
    public static ConduitApiClient CreateConduitApiClient()
    {
        DotEnv.LoadForCurrentEnvironment();

        var baseUrl = Environment.GetEnvironmentVariable("BASE_URL")
                      ?? throw new InvalidOperationException("BASE_URL is not set.");

        var httpClient = CreateHttpClient(baseUrl);
        return new ConduitApiClient(httpClient) { BaseUrl = baseUrl };
    }

    private static HttpClient CreateHttpClient(string baseUrl)
    {
        var handler = new HttpClientHandler
        {
            // The public demo uses a valid cert; kept permissive to mirror the
            // Keystone setup where staging certs are sometimes self-signed.
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri(baseUrl, UriKind.Absolute) };

        var token = Environment.GetEnvironmentVariable("token");
        if (string.IsNullOrEmpty(token))
        {
            var tokenTask = AuthTokenGenerator.GenerateAuthToken();
            tokenTask.Wait();
            token = tokenTask.Result;
        }

        // RealWorld expects the literal scheme "Token", not "Bearer".
        client.DefaultRequestHeaders.Add("Authorization", $"Token {token}");
        return client;
    }
}
