using System.Net;
using ConduitApiClientNamespace;
using FluentAssertions;
using Integration.Tests.utils;
using AllureBeforeAttribute = Allure.Xunit.Attributes.Steps.AllureBeforeAttribute;

namespace Integration.Tests.API;

[AllureParentSuite("Tags")]
[Collection("Allure History Collection")]
public class TagsApiTests : IDisposable
{
    private readonly HttpClient _raw;

    [AllureBefore("Set up API client")]
    public TagsApiTests()
    {
        _raw = ApiClientFactory.CreateRawClient();
        AllureReportUtils.ClearRecordedExceptions();
    }

    public void Dispose()
    {
        _raw.Dispose();
        AllureReportUtils.ThrowRecordedExceptions();
    }

    [Trait("API", "Tags")]
    [Fact(DisplayName = "GET /tags returns 200 and a TagsResponse of non-empty strings")]
    [AllureFeature("List Tags")]
    [AllureDescription("Given the site has tags, When GET /tags, Then 200 + TagsResponse.")]
    public async Task GetTags_Returns200AndTagsSchema()
    {
        var response = await _raw.GetAsync("tags");
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for GET /tags"),
            "GET /tags returns the documented 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "TagsResponse", "GET /tags happy path"),
            "Body matches the TagsResponse schema.");

        var tags = (await response.Content.ReadFromJsonAsync<TagsResponse>())!.Tags;
        AllureReportUtils.LogAndAssert(tags,
            t => t.Should().OnlyContain(tag => !string.IsNullOrWhiteSpace(tag),
                "a blank tag is not a usable tag"),
            "Every tag is a non-empty string.");
    }

    [Trait("API", "Tags")]
    [Fact(DisplayName = "GET /tags is public and returns 200 without auth")]
    [AllureFeature("List Tags")]
    [AllureDescription("Given no token, When GET /tags, Then the documented 200 status is returned.")]
    public async Task GetTags_NoAuth_Returns200()
    {
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        var response = await anon.GetAsync("tags");

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK,
                "GET /tags carries no security requirement in the spec"),
            "Unauthenticated GET /tags returns 200.");
    }
}
