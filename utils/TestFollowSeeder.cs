namespace Integration.Tests.utils;

/// <summary>
/// Seeds a follow relationship between two registered users. Throws when setup
/// does not happen - a precondition that did not happen is a test that never ran,
/// not a failed expectation.
/// </summary>
public static class TestFollowSeeder
{
    /// <summary>
    /// Makes the token's owner follow <paramref name="authorUsername"/>
    /// (POST profiles/{username}/follow via the typed client) and confirms the
    /// API reports <c>following: true</c>.
    /// </summary>
    public static async Task FollowAsync(string readerToken, string authorUsername)
    {
        var client = ApiClientFactory.CreateConduitApiClientForToken(readerToken);
        var profile = await client.FollowUserByUsernameAsync(authorUsername);

        if (profile?.Profile is null || !profile.Profile.Following)
        {
            throw new InvalidOperationException(
                $"Following '{authorUsername}' did not return following=true - " +
                "this precondition did not happen, so the test that needed it never ran.");
        }
    }
}
