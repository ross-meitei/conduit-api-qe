using System.Net;
using ConduitApiClientNamespace;
using FluentAssertions;
using Integration.Tests.utils;
using Allure.Net.Commons;
using AllureBeforeAttribute = Allure.Xunit.Attributes.Steps.AllureBeforeAttribute;

namespace Integration.Tests.API;

[AllureParentSuite("User")]
[Collection("Allure History Collection")]
public class UserApiTests : IDisposable
{
    private readonly ConduitApiClient _client;

    [AllureBefore("Set up API client and auth")]
    public UserApiTests()
    {
        // Registration only - POST /users is unsecured, so the session client is
        // safe here. Deliberately no session-authenticated raw client: PUT /user
        // mutates whoever calls it, and ApiClientFactory hands the same
        // SessionToken to the whole suite, so a PUT sent through it would rewrite
        // the identity Articles, Comments and Profiles authenticate as. Every
        // case below talks to the API as its own throwaway user instead.
        _client = ApiClientFactory.CreateConduitApiClient();
        AllureReportUtils.ClearRecordedExceptions();
    }

    public void Dispose() => AllureReportUtils.ThrowRecordedExceptions();

    // ── GET /user ────────────────────────────────────────────────────────

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user returns 200 and a UserResponse for the authenticated caller")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given a registered user, When GET /user, Then 200 + UserResponse for that user.")]
    public async Task GetCurrentUser_Authenticated_Returns200AndUserSchema()
    {
        var user = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(user.Token);

        var response = await asUser.GetAsync("user");
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for an authenticated GET /user"),
            "GET /user returns the documented 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "UserResponse", "GET /user happy path"),
            "Body matches the UserResponse schema.");

        var current = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        AllureReportUtils.LogAndAssert(current.User.Email,
            e => e.Should().Be(user.Email, "GET /user must return the caller's own record, not another user's"),
            $"GET /user returns the caller '{user.Username}'.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user without auth returns 401")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given no token, When GET /user, Then 401 Unauthorized is returned.")]
    public async Task GetCurrentUser_NoAuth_Returns401()
    {
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        var response = await anon.GetAsync("user");

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized,
                "the spec marks GET /user secured and documents 401 without a token"),
            "Unauthenticated GET /user returns 401.");
    }

    // ── PUT /user ────────────────────────────────────────────────────────

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user updates bio and the change is readable on GET /user")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a registered user, When PUT /user sets a new bio, Then 200 + the bio persists.")]
    public async Task UpdateCurrentUser_Bio_Returns200AndPersists()
    {
        var user = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(user.Token);
        var newBio = $"bio-{Guid.NewGuid():N}";

        var response = await asUser.PutAsJsonAsync("user", new { user = new { bio = newBio } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for a valid PUT /user"),
            "PUT /user returns the documented 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "UserResponse", "PUT /user happy path"),
            "Body matches the UserResponse schema.");

        var updated = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        AllureReportUtils.LogAndAssert(updated.User.Bio,
            b => b.Should().Be(newBio, "the response must reflect the value that was just written"),
            "PUT /user response carries the new bio.");

        // Read it back: a 200 that returns the new value but never stored it is
        // the failure this case exists to catch.
        var reread = (await asUser.GetFromJsonAsync<UserResponse>("user"))!;
        AllureReportUtils.LogAndAssert(reread.User.Bio,
            b => b.Should().Be(newBio, "the update must be persisted, not just echoed back"),
            "GET /user reads back the new bio.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with only bio leaves username and email untouched")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a partial body, When PUT /user, Then unspecified fields keep their values.")]
    public async Task UpdateCurrentUser_PartialUpdate_LeavesOtherFieldsIntact()
    {
        // UpdateUser declares no required fields, so a body carrying one property
        // is the ordinary case, not an edge one. Silently clearing the fields the
        // caller omitted would be data loss with a 200 on it.
        var user = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(user.Token);

        var response = await asUser.PutAsJsonAsync("user", new { user = new { bio = $"bio-{Guid.NewGuid():N}" } });

        // Assert the update succeeded before reading anything back. A rejected
        // PUT leaves every field at its registered value, so the two checks below
        // would pass on a 500 and report coverage this test never provided.
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for a valid partial PUT /user"),
            "Partial PUT /user returns the documented 200.");

        var after = (await asUser.GetFromJsonAsync<UserResponse>("user"))!;
        AllureReportUtils.LogAndAssert(after.User.Username,
            u => u.Should().Be(user.Username, "a body that omits 'username' must not change it"),
            "Username survives a partial update.");
        AllureReportUtils.LogAndAssert(after.User.Email,
            e => e.Should().Be(user.Email, "a body that omits 'email' must not change it"),
            "Email survives a partial update.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with an email already taken returns 422")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given another user's email, When PUT /user, Then 422 + GenericErrorModel.")]
    public async Task UpdateCurrentUser_DuplicateEmail_Returns422()
    {
        var existing = await RegisterUserAsync();
        var claimant = await RegisterUserAsync();
        using var asClaimant = ApiClientFactory.CreateRawClientForToken(claimant.Token);

        var response = await asClaimant.PutAsJsonAsync("user", new { user = new { email = existing.Email } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity,
                "the spec documents 422 for an invalid PUT /user body, and a taken email is invalid"),
            "Claiming a taken email returns the documented 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "PUT /user duplicate-email case"),
            "Error body matches the GenericErrorModel schema.");
        AllureReportUtils.LogAndAssert(body,
            b => b.Should().Contain("email", "the error must name the field that failed validation"),
            "The 422 error names 'email'.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user without auth returns 401")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given no token, When PUT /user, Then 401 Unauthorized is returned.")]
    public async Task UpdateCurrentUser_NoAuth_Returns401()
    {
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        // A valid UpdateUserRequest on purpose: if the body were malformed, a 422
        // would be an equally correct answer and the test would prove nothing
        // about the auth boundary.
        var response = await anon.PutAsJsonAsync("user", new { user = new { bio = "unauthenticated" } });

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized,
                "the spec marks PUT /user secured and documents 401 without a token"),
            "Unauthenticated PUT /user returns 401.");
    }

    /// <summary>Register a throwaway user; returns its credentials and token.</summary>
    private async Task<(string Username, string Email, string Password, string Token)> RegisterUserAsync()
    {
        var (username, email, password) = TestDataFactory.NewUser();
        var created = await _client.CreateUserAsync(new NewUserRequest
        {
            User = new NewUser { Username = username, Email = email, Password = password },
        });
        return (username, email, password, created.User.Token);
    }
}
