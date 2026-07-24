using System.Net;
using ConduitApiClientNamespace;
using FluentAssertions;
using Integration.Tests.utils;
using AllureBeforeAttribute = Allure.Xunit.Attributes.Steps.AllureBeforeAttribute;

namespace Integration.Tests.API;

[AllureParentSuite("Comments")]
[Collection("Allure History Collection")]
public class CommentsApiTests : IDisposable
{
    private readonly ConduitApiClient _client;
    private readonly HttpClient _raw;

    [AllureBefore("Set up API client and auth")]
    public CommentsApiTests()
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

    // ── GET /articles/{slug}/comments ────────────────────────────────────

    [Trait("API", "Comments")]
    [Fact(DisplayName = "GET /articles/{slug}/comments returns 200 and the posted comment")]
    [AllureFeature("List Comments")]
    [AllureDescription("Given an article with a comment, When GET .../comments, Then 200 + MultipleCommentsResponse containing it.")]
    public async Task GetComments_ForArticle_Returns200AndContainsComment()
    {
        var slug = await CreateArticleAsync();
        var commentBody = TestDataFactory.NewComment();
        await _client.CreateArticleCommentAsync(slug, new NewCommentRequest
        {
            Comment = new NewComment { Body = commentBody },
        });

        var response = await _raw.GetAsync($"articles/{slug}/comments");
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for listing comments"),
            "GET /articles/{slug}/comments returns the documented 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "MultipleCommentsResponse", "GET comments happy path"),
            "Body matches the MultipleCommentsResponse schema.");

        var comments = (await response.Content.ReadFromJsonAsync<MultipleCommentsResponse>())!.Comments;
        AllureReportUtils.LogAndAssert(comments,
            c => c.Should().Contain(x => x.Body == commentBody,
                "the comment just posted must appear in the listing"),
            "The posted comment is listed.");

        await _raw.DeleteAsync($"articles/{slug}");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "GET /articles/{slug}/comments for an unknown slug returns 404")]
    [AllureFeature("List Comments")]
    [AllureDescription("Given no such article, When GET .../comments, Then 404 + GenericErrorModel.")]
    public async Task GetComments_UnknownSlug_Returns404()
    {
        var response = await _raw.GetAsync($"articles/does-not-exist-{Guid.NewGuid():N}/comments");
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.NotFound,
                "the spec documents 404 for comments on an unknown article"),
            "Unknown slug returns the documented 404.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "GET comments error case"),
            "Error body matches the GenericErrorModel schema.");
    }

    // ── POST /articles/{slug}/comments ───────────────────────────────────

    [Trait("API", "Comments")]
    [Fact(DisplayName = "POST /articles/{slug}/comments returns 200 and the created comment")]
    [AllureFeature("Create Comment")]
    [AllureDescription("Given an article, When a comment is posted, Then 200 + SingleCommentResponse echoing the body.")]
    public async Task CreateComment_ForArticle_Returns200AndCommentSchema()
    {
        var slug = await CreateArticleAsync();
        var commentBody = TestDataFactory.NewComment();

        var response = await _raw.PostAsJsonAsync($"articles/{slug}/comments",
            new { comment = new { body = commentBody } });
        var body = await response.Content.ReadAsStringAsync();

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for creating a comment"),
            "POST /articles/{slug}/comments returns the documented 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "SingleCommentResponse", "POST comment happy path"),
            "Body matches the SingleCommentResponse schema.");

        var comment = (await response.Content.ReadFromJsonAsync<SingleCommentResponse>())!.Comment;
        AllureReportUtils.LogAndAssert(comment.Body, b => b.Should().Be(commentBody),
            "The created comment keeps the posted body.");

        await _raw.DeleteAsync($"articles/{slug}");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "POST /articles/{slug}/comments without auth returns 401")]
    [AllureFeature("Create Comment")]
    [AllureDescription("Given no token, When posting a comment, Then 401 Unauthorized is returned.")]
    public async Task CreateComment_NoAuth_Returns401()
    {
        var slug = await CreateArticleAsync();
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        var response = await anon.PostAsJsonAsync($"articles/{slug}/comments",
            new { comment = new { body = "unauthenticated" } });

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized,
                "the spec documents 401 for an unauthenticated comment"),
            "Unauthenticated POST comment returns 401.");

        await _raw.DeleteAsync($"articles/{slug}");
    }

    // ── DELETE /articles/{slug}/comments/{id} ────────────────────────────

    [Trait("API", "Comments")]
    [Fact(DisplayName = "DELETE /articles/{slug}/comments/{id} removes the comment and returns 200")]
    [AllureFeature("Delete Comment")]
    [AllureDescription("Given an article with a comment, When it is deleted, Then 200 and the comment is gone.")]
    public async Task DeleteComment_OwnComment_Returns200AndRemovesIt()
    {
        var slug = await CreateArticleAsync();
        var created = await _client.CreateArticleCommentAsync(slug, new NewCommentRequest
        {
            Comment = new NewComment { Body = TestDataFactory.NewComment() },
        });
        var id = created.Comment.Id;

        var response = await _raw.DeleteAsync($"articles/{slug}/comments/{id}");

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for deleting a comment"),
            "DELETE comment returns the documented 200.");

        var remaining = await _client.GetArticleCommentsAsync(slug);
        AllureReportUtils.LogAndAssert(remaining.Comments,
            c => c.Should().NotContain(x => x.Id == id, "a deleted comment must no longer be listed"),
            "The deleted comment is gone.");

        await _raw.DeleteAsync($"articles/{slug}");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "DELETE /articles/{slug}/comments/{id} without auth returns 401")]
    [AllureFeature("Delete Comment")]
    [AllureDescription("Given no token, When deleting a comment, Then 401 Unauthorized is returned.")]
    public async Task DeleteComment_NoAuth_Returns401()
    {
        var slug = await CreateArticleAsync();
        var created = await _client.CreateArticleCommentAsync(slug, new NewCommentRequest
        {
            Comment = new NewComment { Body = TestDataFactory.NewComment() },
        });
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        var response = await anon.DeleteAsync($"articles/{slug}/comments/{created.Comment.Id}");

        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized,
                "the spec documents 401 for an unauthenticated delete"),
            "Unauthenticated DELETE comment returns 401.");

        await _raw.DeleteAsync($"articles/{slug}");
    }

    /// <summary>Create an article owned by the session user; returns its slug.</summary>
    private async Task<string> CreateArticleAsync()
    {
        var (title, description, body, tags) = TestDataFactory.NewArticle();
        var created = await _client.CreateArticleAsync(new NewArticleRequest
        {
            Article = new NewArticle
            {
                Title = title, Description = description, Body = body,
                TagList = new List<string>(tags),
            },
        });
        return created.Article.Slug;
    }
}
