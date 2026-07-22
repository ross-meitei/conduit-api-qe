using Bogus;

namespace Integration.Tests.utils;

/// <summary>
/// Deterministic-yet-varied test data via Bogus, mirroring the Keystone suite's
/// TestDataFactory. Keeps generated titles/tags unique so parallel runs do not
/// collide on the shared demo backend.
/// </summary>
public static class TestDataFactory
{
    /// <summary>Maximum title length the API accepts.</summary>
    /// <remarks>
    /// Enforced by the API but <b>not documented</b> in the spec: <c>title</c>
    /// carries no <c>maxLength</c>, yet a longer value is rejected with 422
    /// <c>"title is a string of less than 100 chars"</c>. Bogus phrases plus a
    /// uniqueness suffix routinely exceeded it, so every article-creating test
    /// failed on data, not on behaviour. Recorded in the discovery log as a spec
    /// gap (CONDUIT-303).
    /// </remarks>
    private const int MaxTitleLength = 99;

    public static (string Title, string Description, string Body, string[] Tags) NewArticle()
    {
        var faker = new Faker();
        var unique = Guid.NewGuid().ToString("N")[..6];
        // Truncate the phrase, never the suffix - uniqueness is what keeps
        // parallel runs from colliding on a duplicate slug.
        var phrase = faker.Hacker.Phrase();
        var room = MaxTitleLength - unique.Length - 1;
        if (phrase.Length > room)
        {
            phrase = phrase[..room].TrimEnd();
        }

        return (
            Title: $"{phrase} {unique}",
            Description: faker.Lorem.Sentence(),
            Body: faker.Lorem.Paragraphs(2),
            Tags: new[] { "qe", faker.Hacker.Adjective().ToLowerInvariant() }
        );
    }

    public static string NewComment() => new Faker().Lorem.Sentence();

    public static (string Username, string Email, string Password) NewUser()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return ($"qa_{suffix}", $"qa_{suffix}@conduit-qe.test", $"Pw!{suffix}");
    }
}
