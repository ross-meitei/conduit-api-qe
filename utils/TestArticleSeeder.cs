using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Integration.Tests.API;

namespace Integration.Tests.utils;

/// <summary>
/// Seeds articles and comments for a user and returns the identifiers tests need.
/// Every method throws when setup does not happen - a precondition that did not
/// happen is a test that never ran, not a failed expectation.
/// </summary>
public static class TestArticleSeeder
{
    /// <summary>Creates an article as the token's owner (POST articles, 201) and returns its slug.</summary>
    public static async Task<string> CreateArticleAsync(string token)
    {
        var (title, description, body, tags) = TestDataFactory.NewArticle();
        using var client = ApiClientFactory.CreateRawClientForToken(token);
        var response = await client.PostAsJsonAsync("articles",
            new { article = new { title, description, body, tagList = tags } });
        await UserApiTests.ThrowUnlessStatusAsync(response, HttpStatusCode.Created, "the author must be able to create an article");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("article").GetProperty("slug").GetString()
               ?? throw new InvalidOperationException("Created article returned no slug - this precondition did not happen.");
    }

    /// <summary>Comments on an article as the token's owner (POST articles/{slug}/comments, spec 200) and returns the comment id.</summary>
    public static async Task<int> CreateCommentAsync(string token, string slug, string body)
    {
        using var client = ApiClientFactory.CreateRawClientForToken(token);
        var response = await client.PostAsJsonAsync($"articles/{slug}/comments",
            new { comment = new { body } });
        await UserApiTests.ThrowUnlessStatusAsync(response, HttpStatusCode.OK, "the commenter must be able to comment on the article");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("comment").GetProperty("id").GetInt32();
    }
}
