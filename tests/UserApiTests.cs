using System.Net;
using System.Net.Http.Headers;
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
    private readonly HttpClient _raw;

    [AllureBefore("Set up API client and auth")]
    public UserApiTests()
    {
        _client = ApiClientFactory.CreateConduitApiClient();
        _raw = ApiClientFactory.CreateRawClient();
        AllureReportUtils.ClearRecordedExceptions();
    }

    public void Dispose()
    {
        _raw.Dispose();
        AllureReportUtils.ThrowRecordedExceptions();
    }

    // -----------------------------------------------------------------------
    // GET /user
    // -----------------------------------------------------------------------

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user with valid token returns 200 and UserResponse schema")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given an authenticated user, When GET /user, Then 200 + UserResponse; no sensitive fields leak.")]
    public async Task GetCurrentUser_ValidToken_Returns200AndUserSchema()
    {
        var (_, token) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asUser.GetAsync("user");
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for GET /user with a valid token"),
            "GET /user returns 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "UserResponse", "GET /user happy path"),
            "Body matches the UserResponse schema.");
        AllureReportUtils.LogAndAssert(body,
            b =>
            {
                var lower = b.ToLower();
                lower.Should().NotContain("\"password\"", "password must not be exposed");
                lower.Should().NotContain("\"passwordhash\"", "passwordHash must not be exposed");
                lower.Should().NotContain("\"salt\"", "salt must not be exposed");
                lower.Should().NotContain("\"id\":", "id must not be exposed");
            },
            "Response contains no password, passwordHash, salt, or id fields.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user token from response is usable on a subsequent call")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given a registered user, When GET /user, Then the returned token is valid for a second call.")]
    public async Task GetCurrentUser_TokenFromResponse_IsUsable()
    {
        var (_, firstToken) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(firstToken);

        var firstResponse = await asUser.GetAsync("user");
        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var parsed = System.Text.Json.JsonDocument.Parse(firstBody);
        var rotatedToken = parsed.RootElement.GetProperty("user").GetProperty("token").GetString()!;

        using var withRotated = ApiClientFactory.CreateRawClientForToken(rotatedToken);
        var secondResponse = await withRotated.GetAsync("user");

        AllureReportUtils.LogAndAssert(secondResponse.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the token returned by GET /user must itself be usable"),
            "Second GET /user with returned token returns 200.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user without token returns 401")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given no auth token, When GET /user, Then 401 Unauthorized.")]
    public async Task GetCurrentUser_NoToken_Returns401()
    {
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);
        var response = await anon.GetAsync("user");
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "the spec documents 401 for an unauthenticated GET /user"),
            "Unauthenticated GET /user returns 401.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user with Bearer scheme returns 401")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given Authorization: Bearer {token} (wrong scheme), When GET /user, Then 401.")]
    public async Task GetCurrentUser_BearerScheme_Returns401()
    {
        var (_, token) = await RegisterUserAsync();
        using var bearer = new HttpClient { BaseAddress = new Uri(ApiClientFactory.ResolveBaseAddress(), UriKind.Absolute) };
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await bearer.GetAsync("user");
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "Bearer is the wrong scheme; the spec requires 'Token'"),
            "GET /user with Bearer scheme returns 401.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user with expired token returns 401")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given a synthetic expired JWT, When GET /user, Then 401.")]
    public async Task GetCurrentUser_ExpiredToken_Returns401()
    {
        const string expiredJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjF9.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"; // appqe: allow-secret — synthetic expired JWT for rejection test, never a real credential
        using var client = new HttpClient { BaseAddress = new Uri(ApiClientFactory.ResolveBaseAddress(), UriKind.Absolute) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", expiredJwt);

        var response = await client.GetAsync("user");
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "an expired JWT must be rejected"),
            "GET /user with expired token returns 401.");
    }

    // -----------------------------------------------------------------------
    // PUT /user
    // -----------------------------------------------------------------------

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user single-field update preserves other fields")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user with bio set, When PUT /user with only username, Then 200 and bio unchanged.")]
    public async Task UpdateCurrentUser_SingleFieldUpdate_PreservesOtherFields()
    {
        var (_, token) = await RegisterUserAsync(bio: "original-bio");
        var newUsername = TestDataFactory.NewUser().Username;
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asUser.PutAsJsonAsync("user", new { user = new { username = newUsername } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "a partial update must return 200"),
            "PUT /user single-field update returns 200.");
        AllureReportUtils.LogAndAssert(body,
            b =>
            {
                b.Should().Contain(newUsername, "username must be updated");
                b.Should().Contain("original-bio", "bio must be unchanged by a username-only update");
            },
            "Username changed; bio preserved.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with bio='' clears bio")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user with bio, When PUT /user with bio='', Then 200 and follow-up GET confirms bio is empty or null.")]
    public async Task UpdateCurrentUser_ExplicitEmptyString_ClearsBio()
    {
        var (_, token) = await RegisterUserAsync(bio: "some bio");
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        var putResponse = await asUser.PutAsJsonAsync("user", new { user = new { bio = "" } });
        AllureReportUtils.LogAndAssert(putResponse.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "clearing bio with empty string must return 200"),
            "PUT /user with bio='' returns 200.");

        var getResponse = await asUser.GetAsync("user");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(getBody,
            b =>
            {
                var cleared = b.Contains("\"bio\":null") || b.Contains("\"bio\":\"\"")
                    || b.Contains("\"bio\": null") || b.Contains("\"bio\": \"\"");
                cleared.Should().BeTrue("follow-up GET must confirm bio is empty or null");
            },
            "GET /user confirms bio is empty or null after clear.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with empty object is a no-op")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given an existing user, When PUT /user with { user:{} }, Then 200 and all fields unchanged.")]
    public async Task UpdateCurrentUser_EmptyBody_IsNoOp()
    {
        var (username, token) = await RegisterUserAsync(bio: "no-op-bio");
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asUser.PutAsJsonAsync("user", new { user = new { } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "an empty update object must return 200"),
            "PUT /user empty body returns 200.");
        AllureReportUtils.LogAndAssert(body,
            b =>
            {
                b.Should().Contain(username, "username must be unchanged");
                b.Should().Contain("no-op-bio", "bio must be unchanged");
            },
            "All original fields preserved.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user ignores privileged fields like token, id, role, createdAt")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user body includes forged privilege fields, Then 200; GET /user confirms those fields are absent.")]
    public async Task UpdateCurrentUser_PrivilegedFieldsIgnored()
    {
        var (_, token) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        var payload = new
        {
            user = new
            {
                bio = "updated-bio",
                token = "forged",
                id = 1,
                role = "admin",
                createdAt = "1970-01-01"
            }
        };
        var putResponse = await asUser.PutAsJsonAsync("user", payload);
        AllureReportUtils.LogAndAssert(putResponse.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "valid fields alongside ignored ones must still return 200"),
            "PUT /user with privileged fields returns 200.");

        var getResponse = await asUser.GetAsync("user");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(getBody,
            b =>
            {
                b.Should().Contain("updated-bio", "the legitimate bio update must apply");
                b.Should().NotContain("\"id\":", "id must not appear in the response");
                b.Should().NotContain("\"role\":", "role must not appear in the response");
                b.Should().NotContain("\"createdAt\":", "createdAt must not appear in the response");
            },
            "Bio updated; id, role, createdAt absent from GET /user response.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with duplicate email returns 422", Skip = "Known defect: API returns 200 when a duplicate email is submitted; story requires 422. Ref: qe-8350452bca95")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given user A and user B, When PUT /user (as B) with A's email, Then 422 + GenericErrorModel naming email.")]
    public async Task UpdateCurrentUser_DuplicateEmail_Returns422()
    {
        var (usernameA, emailA, passwordA) = TestDataFactory.NewUser();
        await _client.CreateUserAsync(new NewUserRequest
        {
            User = new NewUser { Username = usernameA, Email = emailA, Password = passwordA }
        });

        var (_, tokenB) = await RegisterUserAsync();
        using var asB = ApiClientFactory.CreateRawClientForToken(tokenB);

        var response = await asB.PutAsJsonAsync("user", new { user = new { email = emailA } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity, "duplicate email must return 422"),
            "PUT /user with duplicate email returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b =>
            {
                SchemaValidator.AssertMatches(b, "GenericErrorModel", "duplicate email 422");
                b.Should().Contain("email", "errors.body must name the email field");
            },
            "Body matches GenericErrorModel with email error.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with duplicate username (case-insensitive) returns 422")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given user A and user B, When PUT /user (as B) with uppercase of A's username, Then 422 + GenericErrorModel.")]
    public async Task UpdateCurrentUser_DuplicateUsernameCaseInsensitive_Returns422()
    {
        var (usernameA, emailA, passwordA) = TestDataFactory.NewUser();
        await _client.CreateUserAsync(new NewUserRequest
        {
            User = new NewUser { Username = usernameA, Email = emailA, Password = passwordA }
        });

        var (_, tokenB) = await RegisterUserAsync();
        using var asB = ApiClientFactory.CreateRawClientForToken(tokenB);

        var response = await asB.PutAsJsonAsync("user", new { user = new { username = usernameA.ToUpperInvariant() } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity, "a case-insensitive duplicate username must return 422"),
            "PUT /user with duplicate username (case-insensitive) returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "duplicate username 422"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with username shorter than 3 chars returns 422")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user with a 2-char username (below minimum), Then 422 + GenericErrorModel.")]
    public async Task UpdateCurrentUser_UsernameTooShort_Returns422()
    {
        var (_, token) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asUser.PutAsJsonAsync("user", new { user = new { username = "ab" } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity, "a 2-char username is below the 3-char minimum"),
            "PUT /user with 2-char username returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "username too short 422"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with username containing spaces or special chars returns 422")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user with username='user name!', Then 422 + GenericErrorModel.")]
    public async Task UpdateCurrentUser_InvalidUsernameChars_Returns422()
    {
        var (_, token) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asUser.PutAsJsonAsync("user", new { user = new { username = "user name!" } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity, "spaces and special chars are invalid in a username"),
            "PUT /user with special-char username returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "invalid username chars 422"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with invalid email format returns 422")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user with email='not-an-email', Then 422 + GenericErrorModel.")]
    public async Task UpdateCurrentUser_InvalidEmailFormat_Returns422()
    {
        var (_, token) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asUser.PutAsJsonAsync("user", new { user = new { email = "not-an-email" } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity, "a malformed email must return 422"),
            "PUT /user with invalid email returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "invalid email 422"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with password shorter than 8 chars returns 422")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user with a password shorter than 8 chars, Then 422 + GenericErrorModel.")]
    public async Task UpdateCurrentUser_WeakPassword_Returns422()
    {
        var (_, token) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        // appqe: allow-secret — deliberately under-length value to trigger password-validation 422; not a real credential
        var underLengthValue = "short";
        var response = await asUser.PutAsJsonAsync("user", new { user = new { password = underLengthValue } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity, "a password shorter than 8 chars must return 422"),
            "PUT /user with weak password returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "weak password 422"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user password change rotates token and invalidates old credentials")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user changes password, Then new token returned; old token rejected; new password accepted at login.")]
    public async Task UpdateCurrentUser_PasswordChange_RotatesTokenAndInvalidatesOld()
    {
        var (username, email, oldPassword) = TestDataFactory.NewUser();
        var created = await _client.CreateUserAsync(new NewUserRequest
        {
            User = new NewUser { Username = username, Email = email, Password = oldPassword }
        });
        var oldToken = created.User.Token;

        // Factory-generated password distinct from the original
        var newPassword = TestDataFactory.NewUser().Password;

        using var asUser = ApiClientFactory.CreateRawClientForToken(oldToken);
        var putResponse = await asUser.PutAsJsonAsync("user", new { user = new { password = newPassword } });
        var putBody = await putResponse.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(putResponse.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "a valid password change must return 200"),
            "PUT /user with new password returns 200.");

        var parsedPut = System.Text.Json.JsonDocument.Parse(putBody);
        var newToken = parsedPut.RootElement.GetProperty("user").GetProperty("token").GetString()!;
        AllureReportUtils.LogAndAssert(newToken,
            t => t.Should().NotBe(oldToken, "password change must rotate the session token"),
            "New token differs from old token.");

        using var withOldToken = ApiClientFactory.CreateRawClientForToken(oldToken);
        var oldTokenGet = await withOldToken.GetAsync("user");
        AllureReportUtils.LogAndAssert(oldTokenGet.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "old token must be rejected after password change"),
            "GET /user with old token returns 401.");

        using var loginClient = ApiClientFactory.CreateRawClient(authenticated: false);
        var loginOld = await loginClient.PostAsJsonAsync("users/login",
            new { user = new { email, password = oldPassword } });
        AllureReportUtils.LogAndAssert(loginOld.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "login with old password must fail after rotation"),
            "Login with old password returns 401.");

        var loginNew = await loginClient.PostAsJsonAsync("users/login",
            new { user = new { email, password = newPassword } });
        AllureReportUtils.LogAndAssert(loginNew.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "login with new password must succeed"),
            "Login with new password returns 200.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user email change keeps session but moves login identity")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user changes email, Then existing token stays valid; login with new email succeeds; login with old email rejected.")]
    public async Task UpdateCurrentUser_EmailChange_KeepsSessionMovesLoginIdentity()
    {
        var (username, oldEmail, password) = TestDataFactory.NewUser();
        var created = await _client.CreateUserAsync(new NewUserRequest
        {
            User = new NewUser { Username = username, Email = oldEmail, Password = password }
        });
        var token = created.User.Token;
        var (_, newEmail, _) = TestDataFactory.NewUser();

        using var asUser = ApiClientFactory.CreateRawClientForToken(token);
        var putResponse = await asUser.PutAsJsonAsync("user", new { user = new { email = newEmail } });
        AllureReportUtils.LogAndAssert(putResponse.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "email change must return 200"),
            "PUT /user email change returns 200.");

        var getResponse = await asUser.GetAsync("user");
        AllureReportUtils.LogAndAssert(getResponse.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "existing session must remain valid after email change"),
            "GET /user with original token returns 200 after email change.");

        using var loginClient = ApiClientFactory.CreateRawClient(authenticated: false);
        var loginNew = await loginClient.PostAsJsonAsync("users/login",
            new { user = new { email = newEmail, password } });
        AllureReportUtils.LogAndAssert(loginNew.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "login must succeed with the new email"),
            "Login with new email returns 200.");

        var loginOld = await loginClient.PostAsJsonAsync("users/login",
            new { user = new { email = oldEmail, password } });
        AllureReportUtils.LogAndAssert(loginOld.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "login with old email must be rejected after email change"),
            "Login with old email returns 401.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with relative image URL returns 422")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user with image='/relative/path.png', Then 422 + GenericErrorModel.")]
    public async Task UpdateCurrentUser_RelativeImageUrl_Returns422()
    {
        var (_, token) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asUser.PutAsJsonAsync("user", new { user = new { image = "/relative/path.png" } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity, "a relative URL is not a valid absolute image URL"),
            "PUT /user with relative image URL returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "relative image URL 422"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with bio at exact 1024-char limit returns 200")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user with bio = 1024 'a' chars, Then 200 and returned bio matches exactly.")]
    public async Task UpdateCurrentUser_BioAtExactLimit_Returns200()
    {
        var (_, token) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);
        var longBio = new string('a', 1024);

        var response = await asUser.PutAsJsonAsync("user", new { user = new { bio = longBio } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "a 1024-char bio is at the documented limit"),
            "PUT /user with 1024-char bio returns 200.");

        var parsed = System.Text.Json.JsonDocument.Parse(body);
        var returnedBio = parsed.RootElement.GetProperty("user").GetProperty("bio").GetString();
        AllureReportUtils.LogAndAssert(returnedBio,
            b =>
            {
                b.Should().NotBeNull("bio must be present in the response");
                b!.Length.Should().Be(1024, "bio length must match the submitted 1024 chars");
                b.Should().Be(longBio, "bio value must match exactly");
            },
            "Response bio is 1024 chars and matches input.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with malformed JSON body returns 400 or 422, not 500")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user with body='not-json' (Content-Type: application/json), Then 400 or 422 (not 500).")]
    public async Task UpdateCurrentUser_MalformedJson_ReturnsSafeError()
    {
        var (_, token) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        using var content = new StringContent("not-json", System.Text.Encoding.UTF8, "application/json");
        var response = await asUser.PutAsync("user", content);
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s =>
            {
                var code = (int)s;
                (code == 400 || code == 422).Should().BeTrue($"malformed JSON must return 400 or 422, but got {code}");
            },
            "PUT /user with malformed JSON returns 400 or 422 (not 500).");
        AllureReportUtils.LogAndAssert(body,
            b => b.Should().NotBeNullOrWhiteSpace("the error response body must not be empty"),
            "Response body is non-empty.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user failed validation leaves record unchanged")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user with an original bio, When PUT /user with an invalid email, Then 422 and GET /user confirms bio unchanged.")]
    public async Task UpdateCurrentUser_FailedValidation_LeavesRecordUnchanged()
    {
        var (_, token) = await RegisterUserAsync(bio: "original-bio-unchanged");
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        var putResponse = await asUser.PutAsJsonAsync("user", new { user = new { email = "bad" } });
        var putBody = await putResponse.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(putResponse.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity, "an invalid email must return 422"),
            "PUT /user with invalid email returns 422.");
        AllureReportUtils.LogAndAssert(putBody,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "failed validation 422 schema"),
            "PUT /user 422 body matches GenericErrorModel.");

        var getResponse = await asUser.GetAsync("user");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(getBody,
            b => b.Should().Contain("original-bio-unchanged", "a failed PUT must not mutate the record"),
            "GET /user confirms bio is unchanged after a failed PUT.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user without token returns 401")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given no auth token, When PUT /user with a body, Then 401 Unauthorized.")]
    public async Task UpdateCurrentUser_NoToken_Returns401()
    {
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);
        var response = await anon.PutAsJsonAsync("user", new { user = new { bio = "should-be-rejected" } });
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "the spec documents 401 for an unauthenticated PUT /user"),
            "Unauthenticated PUT /user returns 401.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with Bearer scheme returns 401")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given Authorization: Bearer {token} (wrong scheme), When PUT /user, Then 401.")]
    public async Task UpdateCurrentUser_BearerScheme_Returns401()
    {
        var (_, token) = await RegisterUserAsync();
        using var bearer = new HttpClient { BaseAddress = new Uri(ApiClientFactory.ResolveBaseAddress(), UriKind.Absolute) };
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await bearer.PutAsJsonAsync("user", new { user = new { bio = "bearer-attempt" } });
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "Bearer is the wrong scheme; the spec requires 'Token'"),
            "PUT /user with Bearer scheme returns 401.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with an invalid token returns 401")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given Authorization: Token invalid-token, When PUT /user, Then 401 Unauthorized.")]
    public async Task UpdateCurrentUser_InvalidToken_Returns401()
    {
        using var invalid = new HttpClient { BaseAddress = new Uri(ApiClientFactory.ResolveBaseAddress(), UriKind.Absolute) };
        invalid.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "invalid-token-for-rejection-test");

        var response = await invalid.PutAsJsonAsync("user", new { user = new { bio = "invalid-token-attempt" } });
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "a syntactically invalid token must return 401"),
            "PUT /user with invalid token returns 401.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user bio update is confirmed by subsequent GET /user")]
    [AllureFeature("Update Current User")]
    [AllureDescription("Given a user, When PUT /user sets a unique bio, Then 200; response and GET /user both return the same bio.")]
    public async Task UpdateCurrentUser_UpdateBio_ResponseAndGetAgreeField()
    {
        var (_, token) = await RegisterUserAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);
        var uniqueBio = $"bio-{Guid.NewGuid():N}";

        var putResponse = await asUser.PutAsJsonAsync("user", new { user = new { bio = uniqueBio } });
        var putBody = await putResponse.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(putResponse.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "a valid bio update must return 200"),
            "PUT /user bio update returns 200.");
        AllureReportUtils.LogAndAssert(putBody,
            b => SchemaValidator.AssertMatches(b, "UserResponse", "PUT /user 200 response schema"),
            "PUT /user 200 body matches the UserResponse schema.");
        AllureReportUtils.LogAndAssert(putBody,
            b => b.Should().Contain(uniqueBio, "PUT response bio must match the submitted value"),
            "PUT response contains the updated bio.");

        var getResponse = await asUser.GetAsync("user");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(getBody,
            b => b.Should().Contain(uniqueBio, "GET /user must confirm the bio persisted"),
            "GET /user confirms bio matches what was set.");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<(string Username, string Token)> RegisterUserAsync(string? bio = null)
    {
        var (username, email, password) = TestDataFactory.NewUser();
        var created = await _client.CreateUserAsync(new NewUserRequest
        {
            User = new NewUser { Username = username, Email = email, Password = password }
        });
        if (bio != null)
        {
            using var asUser = ApiClientFactory.CreateRawClientForToken(created.User.Token);
            await asUser.PutAsJsonAsync("user", new { user = new { bio } });
        }
        return (username, created.User.Token);
    }
}
