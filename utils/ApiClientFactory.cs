using ConduitApiClientNamespace;
using AllureStepAttribute = Allure.Xunit.Attributes.Steps.AllureStepAttribute;

namespace Integration.Tests.utils;

/// <summary>
/// Builds clients against the configured base URL with the RealWorld "Token"
/// auth header attached.
/// </summary>
/// <remarks>
/// Two clients, on purpose. The NSwag-generated <see cref="ConduitApiClient"/>
/// is convenient for exercising an operation, but it returns a DTO and hides the
/// HTTP response - so a test using it alone cannot assert the documented status
/// code (200 vs 201 are different contracts) and cannot validate the body
/// against the spec. <see cref="CreateRawClient"/> exposes both, which is what
/// the assertion standards actually require.
/// </remarks>
public static class ApiClientFactory
{
    /// <summary>
    /// One token for the whole test session, acquired on first use.
    /// </summary>
    /// <remarks>
    /// Every call to a client factory used to mint its own token, which - with
    /// no <c>token</c> in the environment - meant registering a fresh throwaway
    /// user per client. A test holding both a typed and a raw client was then
    /// acting as two different people: it would create an article as one user
    /// and fetch it as another, and this backend scopes resources to their
    /// owner, so the fetch returned 404 and looked like a product defect.
    /// Acquiring once per session also matches the auth guidance - log in once,
    /// reuse, do not authenticate per test.
    /// </remarks>
    private static readonly Lazy<string> SessionToken = new(() =>
    {
        var configured = Environment.GetEnvironmentVariable("token");
        return !string.IsNullOrWhiteSpace(configured)
            ? configured
            : AuthTokenGenerator.GenerateAuthToken().GetAwaiter().GetResult();
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    [AllureStep("Create API client and load environment")]
    public static ConduitApiClient CreateConduitApiClient()
    {
        var baseUrl = ResolveBaseUrl();
        var httpClient = BuildClient(baseUrl, authenticated: true);
        return new ConduitApiClient(httpClient) { BaseUrl = baseUrl };
    }

    /// <summary>
    /// A plain <see cref="HttpClient"/> for tests that must see the status code
    /// and the raw body - status assertions, schema validation, and the
    /// unauthenticated 401 cases.
    /// </summary>
    public static HttpClient CreateRawClient(bool authenticated = true) =>
        BuildClient(ResolveBaseAddress(), authenticated);

    /// <summary>
    /// A raw client authenticated as a specific user. Needed because this
    /// backend serves a profile only to its owner, so "fetch a profile" is only
    /// testable as the user who owns it.
    /// </summary>
    public static HttpClient CreateRawClientForToken(string token)
    {
        var client = new HttpClient { BaseAddress = new Uri(ResolveBaseAddress(), UriKind.Absolute) };
        client.DefaultRequestHeaders.Add("Authorization", $"Token {token}");
        return client;
    }

    /// <summary>
    /// A typed client authenticated as a specific user, not the shared session
    /// token. Needed wherever a test's Act must be the generated client (per the
    /// operation's own seam) but the caller has to be a throwaway user the test
    /// just registered - e.g. a per-user feed - rather than whoever
    /// <see cref="SessionToken"/> happens to be.
    /// </summary>
    public static ConduitApiClient CreateConduitApiClientForToken(string token)
    {
        var httpClient = CreateRawClientForToken(token);
        return new ConduitApiClient(httpClient) { BaseUrl = ResolveBaseUrl() };
    }

    public static string ResolveBaseUrl()
    {
        DotEnv.LoadForCurrentEnvironment();
        return Environment.GetEnvironmentVariable("BASE_URL")
               ?? throw new InvalidOperationException("BASE_URL is not set.");
    }

    /// <summary>
    /// The base URL with a guaranteed trailing slash.
    /// </summary>
    /// <remarks>
    /// <see cref="HttpClient.BaseAddress"/> resolves a request URI against the
    /// base by URI rules, not string concatenation. With a base of
    /// <c>https://host/api</c>, the last segment is treated as a file: a request
    /// for <c>users</c> resolves to <c>https://host/users</c>, and a request for
    /// <c>/users</c> does the same - the <c>/api</c> prefix is dropped either
    /// way, and every call 404s. The base must end in <c>/</c> and request URIs
    /// must be relative (no leading slash) for the prefix to survive.
    /// </remarks>
    public static string ResolveBaseAddress()
    {
        var baseUrl = ResolveBaseUrl();
        return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
    }

    private static HttpClient BuildClient(string baseUrl, bool authenticated)
    {
        // Certificate validation is left at the platform default on purpose.
        // A blanket "accept any certificate" callback disables verification for
        // every request the suite makes, and nothing in the suite would ever
        // fail because of it - so a broken chain, or an intercepted connection,
        // would look exactly like a healthy run. If a target ever presents a
        // private CA, trust that CA on the machine rather than trusting all.
        var client = new HttpClient { BaseAddress = new Uri(baseUrl, UriKind.Absolute) };
        if (!authenticated)
        {
            return client;
        }

        var token = SessionToken.Value;

        // Fail loudly on an empty token. Sending "Authorization: Token " is
        // indistinguishable, from the test's side, from sending nothing: the API
        // answers 401 "token is missing" - or, on some endpoints here, 404 - and
        // the test looks like a product defect instead of a broken fixture.
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Auth token is empty. Set 'token' in env/.env, or let " +
                "AuthTokenGenerator register a throwaway user - but do not send a " +
                "blank Authorization header.");
        }

        // RealWorld expects the literal scheme "Token", not "Bearer".
        client.DefaultRequestHeaders.Add("Authorization", $"Token {token}");
        return client;
    }
}
