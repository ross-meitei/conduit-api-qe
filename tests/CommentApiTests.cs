using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Integration.Tests.utils;
using Allure.Net.Commons;
using AllureBeforeAttribute = Allure.Xunit.Attributes.Steps.AllureBeforeAttribute;

namespace Integration.Tests.API;

[AllureParentSuite("Comments")]
[Collection("Allure History Collection")]
public class CommentApiTests : IDisposable
{
    [AllureBefore("Reset recorded exceptions")]
    public CommentApiTests()
    {
        AllureReportUtils.ClearRecordedExceptions();
    }

    public void Dispose()
    {
        AllureReportUtils.ThrowRecordedExceptions();
    }

    // ---------- GET /articles/{slug}/comments ----------

    [Trait("API", "Comments")]
    [Fact(DisplayName = "GET /articles/{slug}/comments returns 200 and a MultipleCommentsResponse listing the seeded comment")]
    [AllureFeature("Get Comments")]
    [AllureDescription("Given an article with one comment, When GET anonymously, Then 200 + MultipleCommentsResponse containing that comment.")]
    public async Task GetComments_Anonymous_Returns200AndListsSeededComment()
    {
        // Arrange - an article by an author with one seeded comment, and an anonymous client
        var (authorName, slug, text, commentId) = await ArrangeArticleWithCommentAsync();
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        // Act - list the comments anonymously
        var response = await anon.GetAsync($"articles/{slug}/comments");

        // Assert - 200, the documented schema, and the seeded comment's id, body and author
        var body = await response.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for GET /articles/{slug}/comments"),
            "GET comments returns the documented 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "MultipleCommentsResponse", "GET comments happy path"),
            "Body matches the MultipleCommentsResponse schema.");

        var seeded = FindComment(body, commentId);
        AllureReportUtils.LogAndAssert(seeded,
            c => c.Should().NotBeNull("the seeded comment must be listed"),
            "The seeded comment id is listed.");
        AllureReportUtils.LogAndAssert(seeded!.Value.GetProperty("body").GetString(),
            b => b.Should().Be(text, "the listed comment must carry the seeded body"),
            "Listed body matches the seeded body.");
        AllureReportUtils.LogAndAssert(seeded.Value.GetProperty("author").GetProperty("username").GetString(),
            u => u.Should().Be(authorName, "the listed comment must name its author"),
            "Listed author matches the commenter.");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "GET /articles/{slug}/comments on an article with no comments returns 200 and an empty array")]
    [AllureFeature("Get Comments")]
    [AllureDescription("Given an article with no comments, When GET, Then 200 + MultipleCommentsResponse with comments as an empty array.")]
    public async Task GetComments_NoComments_Returns200AndEmptyArray()
    {
        // Arrange - an article with no comments, and an anonymous client
        var (_, _, _, token) = await RegisterAsync();
        var slug = await TestArticleSeeder.CreateArticleAsync(token);
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        // Act - list the comments
        var response = await anon.GetAsync($"articles/{slug}/comments");

        // Assert - 200, the documented schema, and comments is an empty array
        var body = await response.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "an article with no comments is still a valid article"),
            "GET comments returns 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "MultipleCommentsResponse", "GET comments, empty list"),
            "Body matches the MultipleCommentsResponse schema.");

        using var doc = JsonDocument.Parse(body);
        var comments = doc.RootElement.GetProperty("comments");
        AllureReportUtils.LogAndAssert(comments.ValueKind,
            k => k.Should().Be(JsonValueKind.Array, "comments must be an array, not null or missing"),
            "comments is an array.");
        AllureReportUtils.LogAndAssert(comments.GetArrayLength(),
            n => n.Should().Be(0, "no comment was created"),
            "comments is empty.");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "GET /articles/{slug}/comments for an unknown slug returns 404")]
    [AllureFeature("Get Comments")]
    [AllureDescription("Given a slug that does not exist, When GET, Then 404 + GenericErrorModel.")]
    public async Task GetComments_UnknownSlug_Returns404()
    {
        // Arrange - a slug no article has, and an anonymous client
        var slug = $"no-such-article-{Guid.NewGuid():N}";
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        // Act - list the comments of the missing article
        var response = await anon.GetAsync($"articles/{slug}/comments");

        // Assert - 404 and GenericErrorModel
        var body = await response.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.NotFound, "the spec documents 404 for an unknown article slug"),
            "GET comments for an unknown slug returns 404.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "GenericErrorModel", "GET comments unknown slug"),
            "Body matches GenericErrorModel.");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "GET /articles/{slug}/comments as a non-following reader returns 200 with author.following false")]
    [AllureFeature("Get Comments")]
    [AllureDescription("Given a reader who does not follow the author, When GET with a token, Then 200 and every comment's author.following is false.")]
    public async Task GetComments_AuthenticatedNonFollower_Returns200AndFollowingFalse()
    {
        // Arrange - an article with one comment by its author, and a second user who does not follow them
        var (_, slug, _, _) = await ArrangeArticleWithCommentAsync();
        var (_, _, _, readerToken) = await RegisterAsync();
        using var asReader = ApiClientFactory.CreateRawClientForToken(readerToken);

        // Act - list the comments as the reader
        var response = await asReader.GetAsync($"articles/{slug}/comments");

        // Assert - 200, the documented schema, and following is false on every author
        var body = await response.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "authentication is optional, and a valid token must be accepted"),
            "GET comments with a token returns 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "MultipleCommentsResponse", "GET comments as authenticated reader"),
            "Body matches the MultipleCommentsResponse schema.");

        using var doc = JsonDocument.Parse(body);
        var following = doc.RootElement.GetProperty("comments").EnumerateArray()
            .Select(c => c.GetProperty("author").GetProperty("following").GetBoolean()).ToArray();
        AllureReportUtils.LogAndAssert(following,
            f => f.Should().NotBeEmpty().And.OnlyContain(v => v == false, "the reader does not follow the author"),
            "author.following is false for the non-follower.");
    }

    // ---------- POST /articles/{slug}/comments ----------

    [Trait("API", "Comments")]
    [Fact(DisplayName = "POST /articles/{slug}/comments with a valid body returns 200 and a SingleCommentResponse")]
    [AllureFeature("Create Comment")]
    [AllureDescription("Given an article and an authenticated user, When POST a valid comment, Then 200 + SingleCommentResponse echoing the body and naming the caller.")]
    public async Task CreateComment_Valid_Returns200AndSingleCommentSchema()
    {
        // Arrange - an article by one user, and a commenter with a token
        var (_, _, _, authorToken) = await RegisterAsync();
        var slug = await TestArticleSeeder.CreateArticleAsync(authorToken);
        var (commenter, _, _, commenterToken) = await RegisterAsync();
        using var asCommenter = ApiClientFactory.CreateRawClientForToken(commenterToken);
        var text = TestDataFactory.NewComment();

        // Act - post the comment
        var response = await asCommenter.PostAsJsonAsync($"articles/{slug}/comments", new { comment = new { body = text } });

        // Assert - 200, the documented schema, the echoed body, the caller as author, an integer id and parseable timestamps
        var body = await response.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for POST /articles/{slug}/comments"),
            "POST comment returns the documented 200.");
        AllureReportUtils.LogAndAssert(body,
            b => SchemaValidator.AssertMatches(b, "SingleCommentResponse", "POST comment happy path"),
            "Body matches the SingleCommentResponse schema.");

        using var doc = JsonDocument.Parse(body);
        var comment = doc.RootElement.GetProperty("comment");
        AllureReportUtils.LogAndAssert(comment.GetProperty("body").GetString(),
            b => b.Should().Be(text, "the comment body must echo the input"),
            "comment.body echoes the input.");
        AllureReportUtils.LogAndAssert(comment.GetProperty("author").GetProperty("username").GetString(),
            u => u.Should().Be(commenter, "the author of a comment is the caller"),
            "author.username is the caller.");
        AllureReportUtils.LogAndAssert(comment.GetProperty("id").ValueKind,
            k => k.Should().Be(JsonValueKind.Number, "id is documented as an integer"),
            "id is a number.");
        AllureReportUtils.LogAndAssert(comment.GetProperty("id").TryGetInt32(out _),
            ok => ok.Should().BeTrue("id is documented as an integer"),
            "id fits an integer.");
        AllureReportUtils.LogAndAssert(DateTimeOffset.TryParse(comment.GetProperty("createdAt").GetString(), out _),
            ok => ok.Should().BeTrue("createdAt is documented as date-time"),
            "createdAt parses as a date-time.");
        AllureReportUtils.LogAndAssert(DateTimeOffset.TryParse(comment.GetProperty("updatedAt").GetString(), out _),
            ok => ok.Should().BeTrue("updatedAt is documented as date-time"),
            "updatedAt parses as a date-time.");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "POST /articles/{slug}/comments then GET lists the created comment (round trip, 200)")]
    [AllureFeature("Create Comment")]
    [AllureDescription("Given an article, When a comment is posted, Then GET for the same slug lists it.")]
    public async Task CreateComment_ThenGet_ListsCreatedComment()
    {
        // Arrange - an article and a commenter with a token
        var (_, _, _, token) = await RegisterAsync();
        var slug = await TestArticleSeeder.CreateArticleAsync(token);
        using var asCommenter = ApiClientFactory.CreateRawClientForToken(token);
        var text = TestDataFactory.NewComment();

        // Act - post the comment
        var created = await asCommenter.PostAsJsonAsync($"articles/{slug}/comments", new { comment = new { body = text } });

        // Assert - 200, and a follow-up GET lists the new id with the same body
        await UserApiTests.ThrowUnlessStatusAsync(created, HttpStatusCode.OK, "the comment must be created before the round trip can be checked");
        using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var newId = createdDoc.RootElement.GetProperty("comment").GetProperty("id").GetInt32();
        var listed = await asCommenter.GetAsync($"articles/{slug}/comments");
        var listedBody = await listed.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(listed.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the follow-up GET must succeed"),
            "Follow-up GET comments returns 200.");

        var found = FindComment(listedBody, newId);
        AllureReportUtils.LogAndAssert(found,
            c => c.Should().NotBeNull("the created comment must appear in the list"),
            "The created comment id is listed.");
        AllureReportUtils.LogAndAssert(found!.Value.GetProperty("body").GetString(),
            b => b.Should().Be(text, "the listed body must equal the posted body"),
            "Listed body matches the posted body.");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "POST /articles/{slug}/comments without auth returns 401")]
    [AllureFeature("Create Comment")]
    [AllureDescription("Given no token, When POST a comment, Then 401 Unauthorized.")]
    public async Task CreateComment_NoAuth_Returns401()
    {
        // Arrange - an article, and a client that sends no token
        var (_, _, _, token) = await RegisterAsync();
        var slug = await TestArticleSeeder.CreateArticleAsync(token);
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);

        // Act - post a comment anonymously
        var response = await anon.PostAsJsonAsync($"articles/{slug}/comments", new { comment = new { body = TestDataFactory.NewComment() } });

        // Assert - the documented 401
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "the spec marks POST comments secured and documents 401 without a token"),
            "Unauthenticated POST comment returns 401.");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "POST /articles/{slug}/comments with a Bearer-scheme token (wrong scheme) returns 401")]
    [AllureFeature("Create Comment")]
    [AllureDescription("Given a valid token sent as 'Bearer' instead of 'Token', When POST a comment, Then 401 Unauthorized.")]
    public async Task CreateComment_WrongAuthScheme_Returns401()
    {
        // Arrange - an article, and a valid token sent under the 'Bearer' scheme
        var (_, _, _, token) = await RegisterAsync();
        var slug = await TestArticleSeeder.CreateArticleAsync(token);
        using var wrongScheme = UserApiTests.CreateRawClientWithBearerToken(token);

        // Act - post a comment with the wrong auth scheme
        var response = await wrongScheme.PostAsJsonAsync($"articles/{slug}/comments", new { comment = new { body = TestDataFactory.NewComment() } });

        // Assert - rejected with 401, the same as no token
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "this API's scheme is 'Token', not 'Bearer'"),
            "POST comment with a Bearer-scheme token returns 401.");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "POST /articles/{slug}/comments with a garbage Token value returns 401")]
    [AllureFeature("Create Comment")]
    [AllureDescription("Given 'Authorization: Token <garbage>', When POST a comment, Then 401 Unauthorized.")]
    public async Task CreateComment_GarbageToken_Returns401()
    {
        // Arrange - an article, and a client whose token is not a valid JWT
        var (_, _, _, token) = await RegisterAsync();
        var slug = await TestArticleSeeder.CreateArticleAsync(token);
        using var garbage = ApiClientFactory.CreateRawClientForToken($"garbage-{Guid.NewGuid():N}");

        // Act - post a comment with the garbage token
        var response = await garbage.PostAsJsonAsync($"articles/{slug}/comments", new { comment = new { body = TestDataFactory.NewComment() } });

        // Assert - rejected with 401
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "an unparseable token must be rejected like a missing one"),
            "POST comment with a garbage token returns 401.");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "POST /articles/{slug}/comments with malformed bodies is a 4xx, never 5xx (exploratory)")]
    [AllureFeature("Create Comment")]
    [AllureDescription("Given a missing 'comment' wrapper, an empty object, and an empty comment.body, When POST, Then each is a 4xx. The spec documents no 422 for POST, so only the class is asserted.")]
    public async Task CreateComment_MalformedBodies_Return4xxNever5xx()
    {
        // Arrange - an article and an authenticated client
        var (_, _, _, token) = await RegisterAsync();
        var slug = await TestArticleSeeder.CreateArticleAsync(token);
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);

        // Act - send the three malformed bodies in order
        var statuses = await PostMalformedBodiesAsync(asUser, slug);

        // Assert - each is a 4xx client error, never a 5xx
        foreach (var (label, status) in statuses)
        {
            AllureApi.Step($"POST comment with {label}", () =>
            {
                AllureReportUtils.LogAndAssert((int)status,
                    s => s.Should().BeInRange(400, 499,
                        $"{label} must be a client error, never a server crash (actual: {(int)status} {status}; the spec documents no code for it)"),
                    $"{label} returns a 4xx.");
            });
        }
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "POST /articles/{slug}/comments for an unknown slug is a 4xx, never 5xx (exploratory)")]
    [AllureFeature("Create Comment")]
    [AllureDescription("Given a slug that does not exist, When POST a comment, Then a 4xx. The spec documents no 404 for POST, so only the class is asserted.")]
    public async Task CreateComment_UnknownSlug_Returns4xx()
    {
        // Arrange - an authenticated client and a slug no article has
        var (_, _, _, token) = await RegisterAsync();
        using var asUser = ApiClientFactory.CreateRawClientForToken(token);
        var slug = $"no-such-article-{Guid.NewGuid():N}";

        // Act - post a comment to the missing article
        var response = await asUser.PostAsJsonAsync($"articles/{slug}/comments", new { comment = new { body = TestDataFactory.NewComment() } });

        // Assert - a 4xx client error
        AllureReportUtils.LogAndAssert((int)response.StatusCode,
            s => s.Should().BeInRange(400, 499,
                $"commenting on a missing article must be a client error (actual: {(int)response.StatusCode} {response.StatusCode}; the spec documents no code for it)"),
            "POST comment on an unknown slug returns a 4xx.");
    }

    // ---------- DELETE /articles/{slug}/comments/{id} ----------

    [Trait("API", "Comments")]
    [Fact(DisplayName = "DELETE /articles/{slug}/comments/{id} by the author returns 200 and the comment is gone")]
    [AllureFeature("Delete Comment")]
    [AllureDescription("Given a comment by its author, When the author deletes it, Then 200 and GET no longer lists the id.")]
    public async Task DeleteComment_ByAuthor_Returns200AndCommentGone()
    {
        // Arrange - an article with one comment, and a client carrying the commenter's token
        var (_, slug, _, commentId, token) = await ArrangeArticleWithCommentAndTokenAsync();
        using var asAuthor = ApiClientFactory.CreateRawClientForToken(token);

        // Act - the author deletes their comment
        var response = await asAuthor.DeleteAsync($"articles/{slug}/comments/{commentId}");

        // Assert - 200, and a follow-up GET no longer lists the id
        var after = await asAuthor.GetAsync($"articles/{slug}/comments");
        var afterBody = await after.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the spec documents 200 for DELETE /articles/{slug}/comments/{id}"),
            "DELETE comment returns the documented 200.");
        AllureReportUtils.LogAndAssert(after.StatusCode,
            s => s.Should().Be(HttpStatusCode.OK, "the follow-up GET must succeed to prove the state"),
            "Follow-up GET comments returns 200.");
        AllureReportUtils.LogAndAssert(FindComment(afterBody, commentId),
            c => c.Should().BeNull("a deleted comment must no longer be listed"),
            "The deleted comment id is no longer listed.");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "DELETE /articles/{slug}/comments/{id} by a non-author is a 4xx and the comment stays (exploratory)")]
    [AllureFeature("Delete Comment")]
    [AllureDescription("Given another user's comment, When a second user deletes it, Then a 4xx and GET still lists it. The spec documents only 200/401, so only the class and the unchanged data are asserted.")]
    public async Task DeleteComment_ByNonAuthor_Returns4xxAndCommentRemains()
    {
        // Arrange - an article with the author's comment, and a second user's client
        var (_, slug, _, commentId, _) = await ArrangeArticleWithCommentAndTokenAsync();
        var (_, _, _, otherToken) = await RegisterAsync();
        using var asOther = ApiClientFactory.CreateRawClientForToken(otherToken);

        // Act - the second user tries to delete the author's comment
        var response = await asOther.DeleteAsync($"articles/{slug}/comments/{commentId}");

        // Assert - a 4xx, and the comment is still listed
        var after = await asOther.GetAsync($"articles/{slug}/comments");
        var afterBody = await after.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert((int)response.StatusCode,
            s => s.Should().BeInRange(400, 499,
                $"deleting another user's comment must be refused (actual: {(int)response.StatusCode} {response.StatusCode}; the spec documents no code for it)"),
            "DELETE by a non-author returns a 4xx.");
        AllureReportUtils.LogAndAssert(FindComment(afterBody, commentId),
            c => c.Should().NotBeNull("a refused delete must leave the comment in place"),
            "The comment is still listed.");
    }

    [Trait("API", "Comments")]
    [Fact(DisplayName = "DELETE /articles/{slug}/comments/{id} without auth returns 401 and the comment stays")]
    [AllureFeature("Delete Comment")]
    [AllureDescription("Given a comment, When DELETE without a token, Then 401 and GET still lists the comment.")]
    public async Task DeleteComment_NoAuth_Returns401AndCommentRemains()
    {
        // Arrange - an article with one comment, and a client that sends no token
        var (_, slug, _, commentId, token) = await ArrangeArticleWithCommentAndTokenAsync();
        using var anon = ApiClientFactory.CreateRawClient(authenticated: false);
        using var asAuthor = ApiClientFactory.CreateRawClientForToken(token);

        // Act - delete the comment anonymously
        var response = await anon.DeleteAsync($"articles/{slug}/comments/{commentId}");

        // Assert - the documented 401, and the comment is still listed
        var after = await asAuthor.GetAsync($"articles/{slug}/comments");
        var afterBody = await after.Content.ReadAsStringAsync();
        AllureReportUtils.LogAndAssert(response.StatusCode,
            s => s.Should().Be(HttpStatusCode.Unauthorized, "the spec marks DELETE comment secured and documents 401 without a token"),
            "Unauthenticated DELETE comment returns 401.");
        AllureReportUtils.LogAndAssert(FindComment(afterBody, commentId),
            c => c.Should().NotBeNull("a rejected delete must leave the comment in place"),
            "The comment is still listed.");
    }

    // ---------- helpers ----------

    private static Task<(string Username, string Email, string Password, string Token)> RegisterAsync()
        => TestUserRegistrar.RegisterUserAsync();

    /// <summary>
    /// Registers an author, creates an article and one comment by that author.
    /// Throws if any step fails - a precondition that did not happen is a test
    /// that never ran, not a failed expectation.
    /// </summary>
    private static async Task<(string Username, string Slug, string Text, int CommentId)> ArrangeArticleWithCommentAsync()
    {
        var (username, slug, text, id, _) = await ArrangeArticleWithCommentAndTokenAsync();
        return (username, slug, text, id);
    }

    /// <summary>Same as <see cref="ArrangeArticleWithCommentAsync"/>, also returning the author's token.</summary>
    private static async Task<(string Username, string Slug, string Text, int CommentId, string Token)> ArrangeArticleWithCommentAndTokenAsync()
    {
        var (username, _, _, token) = await RegisterAsync();
        var slug = await TestArticleSeeder.CreateArticleAsync(token);
        var text = TestDataFactory.NewComment();
        var id = await TestArticleSeeder.CreateCommentAsync(token, slug, text);
        return (username, slug, text, id, token);
    }

    /// <summary>Returns the listed comment with this id, or null when the list does not contain it.</summary>
    private static JsonElement? FindComment(string listBody, int id)
    {
        using var doc = JsonDocument.Parse(listBody);
        foreach (var c in doc.RootElement.GetProperty("comments").EnumerateArray())
        {
            if (c.GetProperty("id").GetInt32() == id)
            {
                return c.Clone();
            }
        }

        return null;
    }

    /// <summary>
    /// Posts three malformed bodies in order (no 'comment' wrapper, empty object,
    /// empty comment.body) and returns each label with its status.
    /// </summary>
    private static async Task<IReadOnlyList<(string Label, HttpStatusCode Status)>> PostMalformedBodiesAsync(HttpClient asUser, string slug)
    {
        var path = $"articles/{slug}/comments";
        var results = new List<(string, HttpStatusCode)>();

        using var noWrapper = new StringContent("{\"body\":\"no wrapper\"}", Encoding.UTF8, "application/json");
        results.Add(("a missing 'comment' wrapper", (await asUser.PostAsync(path, noWrapper)).StatusCode));

        using var empty = new StringContent("{}", Encoding.UTF8, "application/json");
        results.Add(("an empty object", (await asUser.PostAsync(path, empty)).StatusCode));

        results.Add(("an empty comment.body", (await asUser.PostAsJsonAsync(path, new { comment = new { body = "" } })).StatusCode));
        return results;
    }
}
