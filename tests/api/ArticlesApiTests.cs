using ConduitApiClientNamespace;
using FluentAssertions;
using Integration.Tests.utils;
using Allure.Net.Commons;
using AllureBeforeAttribute = Allure.Xunit.Attributes.Steps.AllureBeforeAttribute;

namespace Integration.Tests.API;

// The generated client deserializes each response into schema-derived DTOs, so
// asserting on those typed fields validates the response against the OpenAPI schema.
[AllureParentSuite("Articles")]
[Collection("Allure History Collection")]
public class ArticlesApiTests : IDisposable
{
    private readonly ConduitApiClient _client;

    [AllureBefore("Set up API client and auth")]
    public ArticlesApiTests()
    {
        _client = ApiClientFactory.CreateConduitApiClient();
        AllureReportUtils.ClearRecordedExceptions();
    }

    public void Dispose() => AllureReportUtils.ThrowRecordedExceptions();

    [Trait("API", "Articles")]
    [Fact(DisplayName = "GET /articles returns a bounded, well-formed page")]
    [AllureFeature("List Articles")]
    [AllureDescription("Given articles exist, When GET /articles?limit=5, Then <=5 valid articles come back.")]
    public async Task ListArticles_RespectsLimit_AndShape()
    {
        MultipleArticlesResponse? response = null;
        await AllureApi.Step("When listing articles with limit=5", async () =>
        {
            response = await _client.GetArticlesAsync(tag: null, author: null, favorited: null,
                limit: 5, offset: 0);
        });

        AllureReportUtils.LogAndAssert(response, r => r.Should().NotBeNull(), "Response is not null.");
        AllureReportUtils.LogAndAssert(response!.Articles.Count, c => c.Should().BeLessThanOrEqualTo(5),
            "Page respects the limit.");
        foreach (var article in response.Articles)
        {
            article.Slug.Should().NotBeNullOrEmpty();
            article.Author.Username.Should().NotBeNullOrEmpty();
            article.FavoritesCount.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Trait("API", "Articles")]
    [Fact(DisplayName = "POST /articles then GET /articles/{slug} round-trips the article")]
    [AllureFeature("Create Article")]
    [AllureDescription("Given a new article, When it is created, Then it can be fetched by slug.")]
    public async Task CreateArticle_ThenFetchBySlug_RoundTrips()
    {
        var (title, description, body, tags) = TestDataFactory.NewArticle();
        var request = new NewArticleRequest
        {
            Article = new NewArticle
            {
                Title = title, Description = description, Body = body,
                TagList = new List<string>(tags),
            },
        };

        var created = await _client.CreateArticleAsync(request);
        created.Article.Title.Should().Be(title);
        var slug = created.Article.Slug;

        var fetched = await _client.GetArticleAsync(slug);
        fetched.Article.Slug.Should().Be(slug);
        fetched.Article.TagList.Should().Contain("qe");

        // cleanup keeps the shared demo backend tidy
        await _client.DeleteArticleAsync(slug);
    }

    [Trait("API", "Articles")]
    [Fact(DisplayName = "POST /articles favorite toggles favoritesCount")]
    [AllureFeature("Favorite Article")]
    [AllureDescription("Given an article, When favorited then unfavorited, Then the count reflects it.")]
    public async Task FavoriteAndUnfavorite_UpdatesCount()
    {
        var (title, description, body, tags) = TestDataFactory.NewArticle();
        var created = await _client.CreateArticleAsync(new NewArticleRequest
        {
            Article = new NewArticle { Title = title, Description = description, Body = body, TagList = new List<string>(tags) },
        });
        var slug = created.Article.Slug;

        var favorited = await _client.CreateArticleFavoriteAsync(slug);
        favorited.Article.Favorited.Should().BeTrue();
        favorited.Article.FavoritesCount.Should().BeGreaterThan(0);

        var unfavorited = await _client.DeleteArticleFavoriteAsync(slug);
        unfavorited.Article.Favorited.Should().BeFalse();

        await _client.DeleteArticleAsync(slug);
    }

    [Trait("API", "Articles")]
    [Fact(DisplayName = "GET /articles/{slug} for an unknown slug returns 404")]
    [AllureFeature("Get Article")]
    [AllureDescription("Given no such article, When GET /articles/{slug}, Then 404 is returned.")]
    public async Task GetArticle_UnknownSlug_Returns404()
    {
        var act = async () => await _client.GetArticleAsync($"does-not-exist-{Guid.NewGuid():N}");

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(404);
    }
}
