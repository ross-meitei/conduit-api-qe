using System.Net;
using ConduitApiClientNamespace;
using FluentAssertions;
using Integration.Tests.utils;
using Allure.Net.Commons;
using AllureBeforeAttribute = Allure.Xunit.Attributes.Steps.AllureBeforeAttribute;

namespace Integration.Tests.API;

// GET /tags is public; the generated client deserializes the TagsResponse schema.
[AllureParentSuite("Tags")]
[Collection("Allure History Collection")]
public class TagsApiTests : IDisposable
{
    private readonly ConduitApiClient _client;

    [AllureBefore("Set up API client")]
    public TagsApiTests()
    {
        _client = ApiClientFactory.CreateConduitApiClient();
        AllureReportUtils.ClearRecordedExceptions();
    }

    public void Dispose() => AllureReportUtils.ThrowRecordedExceptions();

    [Trait("API", "Tags")]
    [Fact(DisplayName = "GET /tags returns a non-null list of string tags")]
    [AllureFeature("List Tags")]
    [AllureDescription("Given the site has tags, When GET /tags, Then a list of string tags is returned.")]
    public async Task GetTags_ReturnsStringList()
    {
        TagsResponse? response = null;
        await AllureApi.Step("When fetching tags", async () =>
        {
            response = await _client.GetTagsAsync();
        });

        AllureReportUtils.LogAndAssert(response, r => r.Should().NotBeNull(), "Response is not null.");
        AllureReportUtils.LogAndAssert(response!.Tags, t => t.Should().NotBeNull(), "Tags collection exists.");
        response.Tags.Should().OnlyContain(tag => !string.IsNullOrWhiteSpace(tag));
    }

    [Trait("API", "Tags")]
    [Fact(DisplayName = "GET /tags is public and returns HTTP 200 without auth")]
    [AllureFeature("List Tags")]
    [AllureDescription("Given no token, When GET /tags, Then the documented 200 status is returned.")]
    public async Task GetTags_IsPubliclyAccessible_Returns200()
    {
        var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "https://api.realworld.show/api";
        using var anon = new HttpClient { BaseAddress = new Uri(baseUrl, UriKind.Absolute) };

        var response = await anon.GetAsync("/tags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
