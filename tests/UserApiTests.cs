using System.Net;
using System.Text;
using System.Text.Json;
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

    // ---------- GET /user ----------

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user returns 200 and a UserResponse for the authenticated caller")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given a registered user, When GET /user, Then 200 + UserResponse (QEBS-82 AC1, AC2).")]
    public async Task GetUser_Authenticated_Returns200AndUserSchema()
    {
        var (username, email, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asOwner.GetAsync("user");
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for GET /user (QEBS-82 AC1)"),
            "GET /user returns the documented 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "UserResponse", "GET /user happy path"),
            "Body matches the UserResponse schema.");

        var user = (await response.Content.ReadFromJsonAsync<UserResponse>())!.User;
        AllureReportUtils.LogAndAssert(user.Username,
            u => u.Should().Be(username, "the caller's own username must be returned"),
            "Returned username matches the registered account.");
        AllureReportUtils.LogAndAssert(user.Email,
            e => e.Should().Be(email, "the caller's own email must be returned (QEBS-82 AC2)"),
            "Returned email matches the registered account.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user response never carries password, passwordHash, salt or id (QEBS-82 AC4)")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given a registered user, When GET /user, Then the user object exposes only the documented keys (QEBS-82 AC4).")]
    public async Task GetUser_ResponseNeverLeaksSensitiveFields()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asOwner.GetAsync("user");
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for GET /user"),
            "GET /user returns the documented 200.");

        using var doc = JsonDocument.Parse(body);
        var keys = doc.RootElement.GetProperty("user").EnumerateObject().Select(p => p.Name).ToArray();

        AllureReportUtils.LogAndAssert(keys,
            k => k.Should().NotContain(new[] { "password", "passwordHash", "salt", "id" },
                "QEBS-82 AC4 forbids leaking credential or internal-id fields"),
            "user object carries no password/passwordHash/salt/id key.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user token is reusable - a second call with the same token also succeeds (QEBS-82 AC3)")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given a token from registration, When GET /user is called twice with it, Then both calls return 200 (QEBS-82 AC3).")]
    public async Task GetUser_TokenIsReusable_SecondCallSucceeds()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);

        var first = await asOwner.GetAsync("user");
        var second = await asOwner.GetAsync("user");

        AllureReportUtils.LogAndAssert(first.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the first GET /user must succeed"),
            "First GET /user returns 200.");
        AllureReportUtils.LogAndAssert(second.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the same token must remain usable for a second call (QEBS-82 AC3)"),
            "Second GET /user with the same token returns 200.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user returns the caller's own account, never another registered user's (QEBS-82 AC1)")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given two registered users, When A calls GET /user, Then A's own username is returned, never B's (QEBS-82 AC1).")]
    public async Task GetUser_ReturnsCallersOwnUser_NotAnotherUsers()
    {
        var (usernameA, _, _, tokenA) = await RegisterUserAsync();
        var (usernameB, _, _, _) = await RegisterUserAsync();
        using var asA = ApiClientFactory.CreateRawClientForToken(tokenA);

        var response = await asA.GetAsync("user");
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for GET /user"),
            "GET /user returns the documented 200.");

        var user = (await response.Content.ReadFromJsonAsync<UserResponse>())!.User;

        AllureReportUtils.LogAndAssert(user.Username,
            u => u.Should().Be(usernameA, "GET /user must return the calling token's own account"),
            "Returned username is caller A's.");
        AllureReportUtils.LogAndAssert(user.Username,
            u => u.Should().NotBe(usernameB, "GET /user must never leak another user's account"),
            "Returned username is not user B's.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user without auth returns 401")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given no token, When GET /user, Then 401 Unauthorized is returned.")]
    public async Task GetUser_NoAuth_Returns401()
    {
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        var response = await anon.GetAsync("user");

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized,
                "the spec marks GET /user secured and documents 401 without a token"),
            "Unauthenticated GET /user returns 401.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "GET /user with a Bearer-scheme token (wrong scheme) returns 401")]
    [AllureFeature("Get Current User")]
    [AllureDescription("Given a valid token sent as 'Bearer' instead of this API's 'Token' scheme, When GET /user, Then 401 Unauthorized is returned (QEBS-82 AC20).")]
    public async Task GetUser_WrongAuthScheme_Returns401()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var wrongScheme = CreateRawClientWithBearerToken(token);

        var response = await wrongScheme.GetAsync("user");

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized,
                "this API's scheme is 'Token', not 'Bearer' - a wrong scheme must be rejected the same as no token (QEBS-82 AC20)"),
            "GET /user with a Bearer-scheme token returns 401.");
    }

    // ---------- PUT /user ----------

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user updates a single field and leaves the rest unchanged (QEBS-82 AC5)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given a registered user, When PUT /user changes only bio, Then 200 + UserResponse with bio updated and username/email unchanged (QEBS-82 AC5).")]
    public async Task UpdateUser_SingleField_UpdatesBioOthersUnchanged()
    {
        var (username, email, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);
        var newBio = $"bio-{Guid.NewGuid():N}";

        var response = await asOwner.PutAsJsonAsync("user", new { user = new { bio = newBio } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for PUT /user (QEBS-82 AC5)"),
            "PUT /user returns the documented 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "UserResponse", "PUT /user single-field update"),
            "Body matches the UserResponse schema.");

        var user = (await response.Content.ReadFromJsonAsync<UserResponse>())!.User;
        AllureReportUtils.LogAndAssert(user.Bio,
            b => b.Should().Be(newBio, "the updated field must reflect the new value"),
            "bio was updated.");
        AllureReportUtils.LogAndAssert(user.Username,
            u => u.Should().Be(username, "a field not in the update body must be left unchanged"),
            "username is unchanged.");
        AllureReportUtils.LogAndAssert(user.Email,
            e => e.Should().Be(email, "a field not in the update body must be left unchanged"),
            "email is unchanged.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with an empty user object leaves every field unchanged (QEBS-82 AC17)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given a registered user, When PUT /user sends {\"user\":{}}, Then 200 and a follow-up GET /user shows every field unchanged (QEBS-82 AC17).")]
    public async Task UpdateUser_EmptyUserObject_LeavesFieldsUnchanged()
    {
        var (username, email, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asOwner.PutAsJsonAsync("user", new { user = new { } });

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "an empty update object is a documented no-op, not an error (QEBS-82 AC17)"),
            "PUT /user with an empty user object returns 200.");

        var after = await asOwner.GetAsync("user");
        var user = (await after.Content.ReadFromJsonAsync<UserResponse>())!.User;
        AllureReportUtils.LogAndAssert(user.Username,
            u => u.Should().Be(username, "no field was in the update body, so username must be unchanged"),
            "username is unchanged after the empty update.");
        AllureReportUtils.LogAndAssert(user.Email,
            e => e.Should().Be(email, "no field was in the update body, so email must be unchanged"),
            "email is unchanged after the empty update.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with bio explicitly set to \"\" clears the field (QEBS-82 AC6)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given a registered user with a non-empty bio, When PUT /user sends bio: \"\", Then the field is cleared, distinct from omitting it (QEBS-82 AC6).")]
    public async Task UpdateUser_ExplicitEmptyBio_ClearsField()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);
        await asOwner.PutAsJsonAsync("user", new { user = new { bio = $"bio-{Guid.NewGuid():N}" } });

        var response = await asOwner.PutAsJsonAsync("user", new { user = new { bio = "" } });

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "an explicit empty string is a valid value to set, not an error"),
            "PUT /user with bio: \"\" returns 200.");

        var user = (await response.Content.ReadFromJsonAsync<UserResponse>())!.User;
        AllureReportUtils.LogAndAssert(user.Bio,
            b => b.Should().BeEmpty("an explicit \"\" must clear bio, distinct from omitting the field (QEBS-82 AC6)"),
            "bio is now the empty string.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with an email already in use returns 422 (KNOWN DEFECT - QEBS-82 AC7)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given user A's email, When user B updates to it, Then the spec's documented 422 is expected. KNOWN DEFECT: the live API currently returns 200, allowing the duplicate (QEBS-82 AC7).")]
    public async Task UpdateUser_DuplicateEmail_Returns422_KnownDefect()
    {
        var (_, emailA, _, _) = await RegisterUserAsync();
        var (_, _, _, tokenB) = await RegisterUserAsync();
        using var asB = ApiClientFactory.CreateRawClientForToken(tokenB);

        var response = await asB.PutAsJsonAsync("user", new { user = new { email = emailA } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity,
                "QEBS-82 AC7 documents 422 for a duplicate email - KNOWN DEFECT: this API currently returns 200, allowing the duplicate"),
            "PUT /user with a duplicate email returns the documented 422 (QEBS-82 AC7).");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "duplicate-email rejection (QEBS-82 AC7)"),
            "Body matches GenericErrorModel.");
        AllureReportUtils.LogAndAssert(body,
            b => b.Should().ContainEquivalentOf("email", "the error must name the offending field per QEBS-82 AC7"),
            "Error body names 'email'.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with a username already taken (case-insensitive) returns 422 (QEBS-82 AC8)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given user A's username, When user B updates to it in a different case, Then 422 + GenericErrorModel naming 'username' (QEBS-82 AC8).")]
    public async Task UpdateUser_DuplicateUsernameCaseInsensitive_Returns422()
    {
        var (usernameA, _, _, _) = await RegisterUserAsync();
        var (_, _, _, tokenB) = await RegisterUserAsync();
        using var asB = ApiClientFactory.CreateRawClientForToken(tokenB);

        var response = await asB.PutAsJsonAsync("user", new { user = new { username = usernameA.ToUpperInvariant() } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity,
                "a username already taken must be rejected, case-insensitively (QEBS-82 AC8)"),
            "PUT /user with a case-variant duplicate username returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "duplicate-username rejection"),
            "Body matches GenericErrorModel.");
        AllureReportUtils.LogAndAssert(body,
            b => b.Should().ContainEquivalentOf("username", "the error must name the offending field (QEBS-82 AC8)"),
            "Error body names 'username'.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with an invalid username format returns 422 (QEBS-82 AC9)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given a username that is too short and padded with whitespace, When PUT /user, Then 422 + GenericErrorModel (QEBS-82 AC9).")]
    public async Task UpdateUser_InvalidUsernameFormat_Returns422()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asOwner.PutAsJsonAsync("user", new { user = new { username = "  a  " } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity,
                "a whitespace-padded, single-character username violates the documented username rules (QEBS-82 AC9)"),
            "PUT /user with an invalid username format returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "invalid-username-format rejection"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with a malformed email returns 422 (QEBS-82 AC10)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given an email with no '@', When PUT /user, Then 422 + GenericErrorModel (QEBS-82 AC10).")]
    public async Task UpdateUser_MalformedEmail_Returns422()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asOwner.PutAsJsonAsync("user", new { user = new { email = "not-an-email" } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity,
                "a malformed email must be rejected per the documented email format (QEBS-82 AC10)"),
            "PUT /user with a malformed email returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "malformed-email rejection"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with a weak password returns 422 (QEBS-82 AC11)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given a password under 8 characters, When PUT /user, Then 422 + GenericErrorModel is expected. If the API accepts it, that is an enforcement gap to report, not a reason to soften this assertion (QEBS-82 AC11).")]
    public async Task UpdateUser_WeakPassword_Returns422()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);

        var response = await asOwner.PutAsJsonAsync("user", new { user = new { password = "weak" } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity,
                "a 4-character password fails the documented password policy - an accepted weak password is an enforcement gap (QEBS-82 AC11)"),
            "PUT /user with a weak password returns 422.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "weak-password rejection"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user changing password invalidates the old token and old password (QEBS-82 AC12)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given a password change, When the old token is reused or login is attempted with the old password, Then both fail; the new password logs in successfully (QEBS-82 AC12).")]
    public async Task UpdateUser_PasswordChange_InvalidatesOldToken()
    {
        var (_, email, oldPassword, oldToken) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(oldToken);
        var newPassword = $"Pw!{Guid.NewGuid():N}";

        var update = await asOwner.PutAsJsonAsync("user", new { user = new { password = newPassword } });
        AllureReportUtils.LogAndAssert(update.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the password change itself must succeed before the invalidation can be checked"),
            "PUT /user password change returns 200.");

        var oldTokenAttempt = await asOwner.GetAsync("user");
        AllureReportUtils.LogAndAssert(oldTokenAttempt.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized,
                "the pre-change token must stop working once the password changes (QEBS-82 AC12)"),
            "GET /user with the old token returns 401 after the password change.");

        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);
        var oldLogin = await anon.PostAsJsonAsync("users/login", new { user = new { email, password = oldPassword } });
        AllureReportUtils.LogAndAssert(oldLogin.StatusCode,
            s => s.Should().BeOneOf(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.UnprocessableEntity },
                "the old password must no longer authenticate (QEBS-82 AC12)"),
            "Login with the old password fails.");

        var newLogin = await anon.PostAsJsonAsync("users/login", new { user = new { email, password = newPassword } });
        AllureReportUtils.LogAndAssert(newLogin.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the new password must authenticate (QEBS-82 AC12)"),
            "Login with the new password returns 200.");

        var newToken = (await newLogin.Content.ReadFromJsonAsync<UserResponse>())!.User.Token;
        using var asNewToken = ApiClientFactory.CreateRawClientForToken(newToken);
        var newTokenCheck = await asNewToken.GetAsync("user");
        AllureReportUtils.LogAndAssert(newTokenCheck.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the token from the new login must work"),
            "GET /user with the freshly issued token returns 200.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user changing email keeps the old token alive and old email login fails (QEBS-82 AC13)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given an email change, When the same token is reused, Then it still authorizes GET /user; login with the old email fails and the new email succeeds (QEBS-82 AC13).")]
    public async Task UpdateUser_EmailChange_KeepsOldTokenSessionAlive()
    {
        var (_, oldEmail, password, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);
        var newEmail = $"qa_{Guid.NewGuid():N}@conduit-qe.test";

        var update = await asOwner.PutAsJsonAsync("user", new { user = new { email = newEmail } });
        AllureReportUtils.LogAndAssert(update.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the email change itself must succeed before the session check applies"),
            "PUT /user email change returns 200.");

        var stillAlive = await asOwner.GetAsync("user");
        AllureReportUtils.LogAndAssert(stillAlive.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "changing email must not invalidate the existing token (QEBS-82 AC13)"),
            "GET /user with the same token still returns 200 after the email change.");

        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);
        var oldEmailLogin = await anon.PostAsJsonAsync("users/login", new { user = new { email = oldEmail, password } });
        AllureReportUtils.LogAndAssert(oldEmailLogin.StatusCode,
            s => s.Should().BeOneOf(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.UnprocessableEntity },
                "the old email must no longer authenticate once changed (QEBS-82 AC13)"),
            "Login with the old email fails.");

        var newEmailLogin = await anon.PostAsJsonAsync("users/login", new { user = new { email = newEmail, password } });
        AllureReportUtils.LogAndAssert(newEmailLogin.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the new email must authenticate (QEBS-82 AC13)"),
            "Login with the new email returns 200.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with a non-absolute-http(s) image URL returns 422 (QEBS-82 AC14)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given an image value that is not an absolute http(s) URL, When PUT /user, Then 422 + GenericErrorModel for each malformed form (QEBS-82 AC14).")]
    public async Task UpdateUser_ImageNotAbsoluteUrl_Returns422()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);
        string[] invalidImages = { "/relative/path.png", "javascript:alert(1)", "ftp://host/image.png", "not a url" };

        foreach (var image in invalidImages)
        {
            await AllureApi.Step($"PUT /user with image '{image}'", async () =>
            {
                var response = await asOwner.PutAsJsonAsync("user", new { user = new { image } });
                var body = await response.Content.ReadAsStringAsync();

                AllureReportUtils.LogAndAssert(response.StatusCode,
                    s => s.Should().Be(HttpStatusCode.UnprocessableEntity,
                        $"'{image}' is not an absolute http(s) URL (QEBS-82 AC14)"),
                    $"image '{image}' returns 422.");
                AllureReportUtils.LogAndAssert(body,
                    b => SchemaValidator.AssertMatches(b, "GenericErrorModel", $"invalid image '{image}'"),
                    "Body matches GenericErrorModel.");
            });
        }
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user bio at 1024 chars round-trips; 1025 chars is rejected (QEBS-82 AC15)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given a bio of exactly 1024 characters, When PUT /user, Then it round-trips byte-for-byte; at 1025 characters, 422 (QEBS-82 AC15).")]
    public async Task UpdateUser_BioBoundary_1024RoundTrips_1025Rejected()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);
        var bio1024 = new string('b', 1024);
        var bio1025 = new string('b', 1025);

        var atLimit = await asOwner.PutAsJsonAsync("user", new { user = new { bio = bio1024 } });
        AllureReportUtils.LogAndAssert(atLimit.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "1024 characters is the documented maximum and must be accepted (QEBS-82 AC15)"),
            "bio at exactly 1024 chars returns 200.");
        var atLimitUser = (await atLimit.Content.ReadFromJsonAsync<UserResponse>())!.User;
        AllureReportUtils.LogAndAssert(atLimitUser.Bio,
            b => b.Should().Be(bio1024, "the stored bio must round-trip byte-for-byte at the documented limit"),
            "bio round-trips at 1024 chars.");

        var overLimit = await asOwner.PutAsJsonAsync("user", new { user = new { bio = bio1025 } });
        var overLimitBody = await overLimit.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(overLimit.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity,
                "1025 characters exceeds the documented bio limit (QEBS-82 AC15)"),
            "bio at 1025 chars returns 422.");
        AllureReportUtils.LogAndAssert(overLimitBody,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "bio-too-long rejection"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user ignores unknown or privileged fields in the body (QEBS-82 AC16)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given a body carrying token/id/role alongside a real field, When PUT /user, Then only the documented field is applied and the privileged ones are ignored (QEBS-82 AC16).")]
    public async Task UpdateUser_UnknownPrivilegedFields_Ignored()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);
        var newBio = $"bio-{Guid.NewGuid():N}";

        var response = await asOwner.PutAsJsonAsync("user",
            new { user = new { bio = newBio, token = "hacked-token", id = 999, role = "admin" } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "unknown fields must not cause the update to fail"),
            "PUT /user with extra fields returns 200.");

        using var doc = JsonDocument.Parse(body);
        var keys = doc.RootElement.GetProperty("user").EnumerateObject().Select(p => p.Name).ToArray();
        AllureReportUtils.LogAndAssert(keys,
            k => k.Should().NotContain(new[] { "id", "role" },
                "the response must never echo a client-supplied privileged field back as an account attribute (QEBS-82 AC16)"),
            "Response carries no 'id' or 'role' key.");

        var user = (await response.Content.ReadFromJsonAsync<UserResponse>())!.User;
        AllureReportUtils.LogAndAssert(user.Bio,
            b => b.Should().Be(newBio, "the documented field in the same body must still apply"),
            "bio was updated despite the extra fields.");
        AllureReportUtils.LogAndAssert(user.Token,
            t => t.Should().NotBe("hacked-token", "a client-supplied token must never be adopted as the session token (QEBS-82 AC16)"),
            "token was not replaced by the client-supplied value.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with malformed JSON or a missing 'user' wrapper is rejected, never 500 (QEBS-82 AC18)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given malformed JSON, and separately a body missing the 'user' wrapper, When PUT /user, Then 422 or 400 - never 500 (QEBS-82 AC18).")]
    public async Task UpdateUser_MalformedJsonOrMissingWrapper_NeverReturns500()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);

        await AllureApi.Step("PUT /user with malformed JSON", async () =>
        {
            using var malformed = new StringContent("{ this is not valid json", Encoding.UTF8, "application/json");
            var response = await asOwner.PutAsync("user", malformed);
            AllureReportUtils.LogAndAssert(response.StatusCode,
                s => s.Should().BeOneOf(new[] { HttpStatusCode.UnprocessableEntity, HttpStatusCode.BadRequest },
                    "malformed JSON must be a client-error rejection, never a server crash (QEBS-82 AC18)"),
                "Malformed JSON body returns 422 or 400, not 500.");
        });

        await AllureApi.Step("PUT /user with the 'user' wrapper missing", async () =>
        {
            var response = await asOwner.PutAsJsonAsync("user", new { email = "no-wrapper@conduit-qe.test" });
            AllureReportUtils.LogAndAssert(response.StatusCode,
                s => s.Should().BeOneOf(new[] { HttpStatusCode.UnprocessableEntity, HttpStatusCode.BadRequest },
                    "a body missing the required 'user' wrapper must be rejected, never a server crash (QEBS-82 AC18)"),
                "Body missing the 'user' wrapper returns 422 or 400, not 500.");
        });
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user without auth returns 401")]
    [AllureFeature("Update User")]
    [AllureDescription("Given no token, When PUT /user, Then 401 Unauthorized is returned.")]
    public async Task UpdateUser_NoAuth_Returns401()
    {
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        var response = await anon.PutAsJsonAsync("user", new { user = new { bio = "anonymous edit" } });

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized,
                "the spec marks PUT /user secured and documents 401 without a token"),
            "Unauthenticated PUT /user returns 401.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "PUT /user with a Bearer-scheme token (wrong scheme) returns 401")]
    [AllureFeature("Update User")]
    [AllureDescription("Given a valid token sent as 'Bearer' instead of this API's 'Token' scheme, When PUT /user, Then 401 Unauthorized is returned (QEBS-82 AC20).")]
    public async Task UpdateUser_WrongAuthScheme_Returns401()
    {
        var (_, _, _, token) = await RegisterUserAsync();
        using var wrongScheme = CreateRawClientWithBearerToken(token);

        var response = await wrongScheme.PutAsJsonAsync("user", new { user = new { bio = "wrong scheme edit" } });

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized,
                "this API's scheme is 'Token', not 'Bearer' - a wrong scheme must be rejected the same as no token (QEBS-82 AC20)"),
            "PUT /user with a Bearer-scheme token returns 401.");
    }

    [Trait("API", "User")]
    [Fact(DisplayName = "A rejected PUT /user update leaves the record unchanged (QEBS-82 AC22)")]
    [AllureFeature("Update User")]
    [AllureDescription("Given a PUT /user that is rejected with 422, When GET /user follows, Then every field is exactly as it was before the rejected attempt - no partial write (QEBS-82 AC22).")]
    public async Task UpdateUser_RejectedUpdateLeavesRecordUnchanged()
    {
        var (username, email, _, token) = await RegisterUserAsync();
        using var asOwner = ApiClientFactory.CreateRawClientForToken(token);

        var rejected = await asOwner.PutAsJsonAsync("user", new { user = new { email = "not-an-email" } });
        AllureReportUtils.LogAndAssert(rejected.StatusCode,
            s => s.Should().Be(HttpStatusCode.UnprocessableEntity,
                "the malformed email must be rejected before the unchanged-record check applies"),
            "The malformed-email update returns 422.");

        var after = await asOwner.GetAsync("user");
        AllureReportUtils.LogAndAssert(after.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the follow-up GET must succeed to prove the record's state"),
            "Follow-up GET /user returns 200.");

        var user = (await after.Content.ReadFromJsonAsync<UserResponse>())!.User;
        AllureReportUtils.LogAndAssert(user.Username,
            u => u.Should().Be(username, "a rejected update must leave every field unchanged (QEBS-82 AC22)"),
            "username is unchanged after the rejected update.");
        AllureReportUtils.LogAndAssert(user.Email,
            e => e.Should().Be(email, "a rejected update must leave every field unchanged (QEBS-82 AC22)"),
            "email is unchanged after the rejected update.");
    }

    /// <summary>Register a throwaway user; returns its username, email, password and token.</summary>
    private async Task<(string Username, string Email, string Password, string Token)> RegisterUserAsync()
    {
        var (username, email, password) = TestDataFactory.NewUser();
        var created = await _client.CreateUserAsync(new NewUserRequest
        {
            User = new NewUser { Username = username, Email = email, Password = password },
        });
        return (username, email, password, created.User.Token);
    }

    /// <summary>
    /// Raw client sending the wrong auth scheme ("Bearer"), to prove the API
    /// rejects it. Internal (not private) so other test classes in this
    /// assembly can reuse this one copy rather than adding another.
    /// </summary>
    internal static HttpClient CreateRawClientWithBearerToken(string token)
    {
        var client = new HttpClient { BaseAddress = new Uri(ApiClientFactory.ResolveBaseAddress(), UriKind.Absolute) };
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }
}
