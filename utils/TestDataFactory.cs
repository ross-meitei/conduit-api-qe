using Bogus;

namespace Integration.Tests.utils;

/// <summary>
/// Deterministic-yet-varied test data via Bogus, mirroring the Keystone suite's
/// TestDataFactory. Keeps generated titles/tags unique so parallel runs do not
/// collide on the shared demo backend.
/// </summary>
public static class TestDataFactory
{
    public static (string Title, string Description, string Body, string[] Tags) NewArticle()
    {
        var faker = new Faker();
        var unique = Guid.NewGuid().ToString("N")[..6];
        return (
            Title: $"{faker.Hacker.Phrase()} {unique}",
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
