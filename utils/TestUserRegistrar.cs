using ConduitApiClientNamespace;

namespace Integration.Tests.utils;

/// <summary>
/// Registers a throwaway user and returns its credentials and token.
/// </summary>
/// <remarks>
/// This same helper already exists, independently, as a private method in
/// <c>tests/ProfilesApiTests.cs</c> and <c>tests/UserApiTests.cs</c>. A third
/// endpoint group needing it is the point at which a third private copy stops
/// being reasonable, so it lives here instead - new tests call this one; the
/// two existing copies are left as-is (converging passing tests is a separate,
/// deliberate refactor, not a side effect of adding coverage elsewhere).
/// </remarks>
public static class TestUserRegistrar
{
    /// <summary>
    /// Registers a unique throwaway user via <see cref="ApiClientFactory.CreateConduitApiClient"/>.
    /// Throws if registration does not return a usable token - a precondition
    /// that did not happen, not a failed assertion.
    /// </summary>
    public static async Task<(string Username, string Email, string Password, string Token)> RegisterUserAsync()
    {
        var client = ApiClientFactory.CreateConduitApiClient();
        var (username, email, password) = TestDataFactory.NewUser();
        var created = await client.CreateUserAsync(new NewUserRequest
        {
            User = new NewUser { Username = username, Email = email, Password = password },
        });

        if (created?.User is null || string.IsNullOrWhiteSpace(created.User.Token))
        {
            throw new InvalidOperationException(
                $"Registering throwaway user '{username}' did not return a token - " +
                "this precondition did not happen, so the test that needed it never ran.");
        }

        return (username, email, password, created.User.Token);
    }
}
