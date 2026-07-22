using ConduitApiClientNamespace;
using FluentAssertions;
using Integration.Tests.utils;
using Allure.Net.Commons;
using AllureBeforeAttribute = Allure.Xunit.Attributes.Steps.AllureBeforeAttribute;

namespace Integration.Tests.API;

// Response bodies are deserialized by the NSwag-generated client into DTOs that
// mirror components.schemas in specs/openapi.json - typed deserialization is the
// schema validation these tests rely on.
[AllureParentSuite("Users and Authentication")]
[Collection("Allure History Collection")]
public class UsersApiTests : IDisposable
{
    private readonly ConduitApiClient _client;

    [AllureBefore("Set up API client and auth")]
    public UsersApiTests()
    {
        _client = ApiClientFactory.CreateConduitApiClient();
        AllureReportUtils.ClearRecordedExceptions();
    }

    public void Dispose() => AllureReportUtils.ThrowRecordedExceptions();

    [Trait("API", "Users")]
    [Fact(DisplayName = "GET /user returns the authenticated user with a token")]
    [AllureFeature("Current User")]
    [AllureDescription("Given a valid token, When GET /user, Then the current user is returned.")]
    public async Task GetCurrentUser_WithValidToken_ReturnsUser()
    {
        UserResponse? response = null;
        await AllureApi.Step("When fetching the current user", async () =>
        {
            response = await _client.GetCurrentUserAsync();
        });

        AllureReportUtils.LogAndAssert(response, r => r.Should().NotBeNull(), "Response is not null.");
        AllureReportUtils.LogAndAssert(response!.User.Token, t => t.Should().NotBeNullOrWhiteSpace(),
            "User has a JWT token.");
        AllureReportUtils.LogAndAssert(response.User.Email, e => e.Should().Contain("@"),
            "User email looks valid.");
    }

    [Trait("API", "Users")]
    [Fact(DisplayName = "POST /users/login with bad credentials returns 401/422")]
    [AllureFeature("Login")]
    [AllureDescription("Given wrong credentials, When POST /users/login, Then it is rejected.")]
    public async Task Login_WithBadCredentials_IsRejected()
    {
        // Derive a wrong-but-realistic password rather than hardcoding a literal.
        var wrongPassword = $"wrong-{Guid.NewGuid():N}";
        var request = new LoginUserRequest
        {
            User = new LoginUser { Email = "nobody@conduit-qe.test", Password = wrongPassword },
        };

        var act = async () => await _client.LoginAsync(request);

        var ex = await act.Should().ThrowAsync<ApiException>();
        // RealWorld answers invalid logins with 403/422 depending on deployment.
        ex.Which.StatusCode.Should().BeOneOf(401, 403, 422);
    }

    [Trait("API", "Users")]
    [Fact(DisplayName = "POST /users registers a new account and returns 201-shaped body")]
    [AllureFeature("Registration")]
    [AllureDescription("Given unique details, When POST /users, Then a user with a token is returned.")]
    public async Task Register_NewUser_ReturnsUserWithToken()
    {
        var (username, email, password) = TestDataFactory.NewUser();
        var request = new NewUserRequest
        {
            User = new NewUser { Username = username, Email = email, Password = password },
        };

        UserResponse? response = null;
        await AllureApi.Step("When registering a new user", async () =>
        {
            response = await _client.CreateUserAsync(request);
        });

        response.Should().NotBeNull();
        response!.User.Username.Should().Be(username);
        response.User.Token.Should().NotBeNullOrWhiteSpace();
    }
}
