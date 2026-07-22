using ConduitApiClientNamespace;
using FluentAssertions;
using Integration.Tests.utils;
using Allure.Net.Commons;
using AllureBeforeAttribute = Allure.Xunit.Attributes.Steps.AllureBeforeAttribute;

namespace Integration.Tests.API;

// Typed deserialization by the generated client validates each response against
// the ProfileResponse schema.
[AllureParentSuite("Profiles")]
[Collection("Allure History Collection")]
public class ProfilesApiTests : IDisposable
{
    private readonly ConduitApiClient _client;

    [AllureBefore("Set up API client and auth")]
    public ProfilesApiTests()
    {
        _client = ApiClientFactory.CreateConduitApiClient();
        AllureReportUtils.ClearRecordedExceptions();
    }

    public void Dispose() => AllureReportUtils.ThrowRecordedExceptions();

    [Trait("API", "Profiles")]
    [Fact(DisplayName = "POST then DELETE /profiles/{username}/follow flips the following flag")]
    [AllureFeature("Follow")]
    [AllureDescription("Given another user, When followed then unfollowed, Then 'following' reflects it.")]
    public async Task FollowThenUnfollow_TogglesFollowing()
    {
        // Register a second user to follow, so the test is self-contained.
        var (username, email, password) = TestDataFactory.NewUser();
        await _client.CreateUserAsync(new NewUserRequest
        {
            User = new NewUser { Username = username, Email = email, Password = password },
        });

        ProfileResponse? followed = null;
        await AllureApi.Step($"When following {username}", async () =>
        {
            followed = await _client.FollowUserByUsernameAsync(username);
        });
        followed!.Profile.Following.Should().BeTrue();
        followed.Profile.Username.Should().Be(username);

        var unfollowed = await _client.UnfollowUserByUsernameAsync(username);
        unfollowed.Profile.Following.Should().BeFalse();
    }

    [Trait("API", "Profiles")]
    [Fact(DisplayName = "GET /profiles/{username} for an unknown user returns 404-style error")]
    [AllureFeature("Get Profile")]
    [AllureDescription("Given no such profile, When GET /profiles/{username}, Then an error status is returned.")]
    public async Task GetProfile_UnknownUser_ReturnsError()
    {
        var act = async () => await _client.GetProfileByUsernameAsync($"nobody-{Guid.NewGuid():N}");

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().BeOneOf(404, 422);
    }
}
