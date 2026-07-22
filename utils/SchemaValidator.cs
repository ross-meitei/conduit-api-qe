using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using NJsonSchema;

namespace Integration.Tests.utils;

/// <summary>
/// Validates a response body against a schema taken from
/// <c>specs/openapi.json</c> - the contract itself, not a hand-copy of it.
/// </summary>
/// <remarks>
/// This exists because deserialising a body into an NSwag-generated DTO is
/// <b>not</b> schema validation, though it reads like it. Generated DTOs are
/// permissive: a missing <c>required</c> field arrives as <c>null</c>, an
/// unexpected field is dropped silently, and a wrong-but-coercible type is
/// converted. A response can breach the contract in all three ways and still
/// deserialise without complaint, so a suite relying on typed deserialization is
/// asserting that the JSON parsed - nothing more.
/// <para>
/// Schemas are read from the spec at run time and cached, so the tests move in
/// lock-step with the contract: change the spec and these assertions change with
/// it, with no test edit required.
/// </para>
/// </remarks>
public static class SchemaValidator
{
    private static readonly ConcurrentDictionary<string, JsonSchema> Cache = new();
    private static readonly Lazy<JsonDocument> Spec = new(LoadSpec);

    /// <summary>
    /// Assert that <paramref name="json"/> satisfies the named schema from
    /// <c>components.schemas</c>. Throws with every violation listed.
    /// </summary>
    /// <param name="waiveRequired">
    /// Property names to treat as optional, for a <b>recorded</b> spec defect
    /// only. Every use must name the finding in a comment at the call site: this
    /// is how a known contract gap stays visible and greppable, instead of being
    /// dissolved into a vaguer assertion that hides it.
    /// </param>
    public static void AssertMatches(string json, string schemaName, string because,
                                     params string[] waiveRequired)
    {
        var key = waiveRequired.Length == 0
            ? schemaName
            : $"{schemaName}!{string.Join(",", waiveRequired.OrderBy(x => x))}";
        var schema = Cache.GetOrAdd(key, _ => ResolveSchema(schemaName, waiveRequired));
        var errors = schema.Validate(json);
        if (errors.Count == 0)
        {
            return;
        }

        var detail = string.Join("; ", errors.Select(e => $"{e.Path} {e.Kind}"));
        throw new InvalidOperationException(
            $"Response does not match the documented '{schemaName}' schema ({because}). " +
            $"EXPECTED: a body valid against components.schemas.{schemaName}. " +
            $"ACTUAL violations: {detail}.");
    }

    private static JsonSchema ResolveSchema(string schemaName, string[] waiveRequired)
    {
        var schemas = Spec.Value.RootElement
            .GetProperty("components")
            .GetProperty("schemas");

        if (!schemas.TryGetProperty(schemaName, out _))
        {
            throw new InvalidOperationException(
                $"No schema '{schemaName}' in components.schemas - the test names a " +
                "schema the spec does not define.");
        }

        // NJsonSchema resolves JSON Schema's "#/definitions/..." pointers, not
        // OpenAPI's "#/components/schemas/...". Present the same schemas under
        // "definitions" and repoint the refs, so nested ones - ProfileResponse ->
        // Profile, SingleArticleResponse -> Article -> Profile - resolve instead
        // of failing with "reference path has not been resolved".
        var definitions = schemas.GetRawText();
        if (waiveRequired.Length > 0)
        {
            definitions = StripRequired(definitions, waiveRequired);
        }

        var document = $$"""
            {
              "$ref": "#/definitions/{{schemaName}}",
              "definitions": {{definitions}}
            }
            """.Replace("#/components/schemas/", "#/definitions/", StringComparison.Ordinal);

        return JsonSchema.FromJsonAsync(document).GetAwaiter().GetResult();
    }

    /// <summary>Drop the named properties from every "required" array.</summary>
    private static string StripRequired(string definitionsJson, string[] waived)
    {
        using var document = JsonDocument.Parse(definitionsJson);
        var rewritten = new Dictionary<string, JsonNode?>();
        foreach (var schema in document.RootElement.EnumerateObject())
        {
            var node = JsonNode.Parse(schema.Value.GetRawText())!;
            if (node["required"] is JsonArray required)
            {
                var kept = new JsonArray();
                foreach (var item in required)
                {
                    if (item is not null && !waived.Contains(item.GetValue<string>()))
                    {
                        kept.Add(item.GetValue<string>());
                    }
                }

                node["required"] = kept;
            }

            rewritten[schema.Name] = node;
        }

        return JsonSerializer.Serialize(rewritten);
    }

    private static JsonDocument LoadSpec()
    {
        var path = FindSpec()
                   ?? throw new InvalidOperationException(
                       "specs/openapi.json not found - schema validation needs the spec.");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    /// <summary>Walk up from the test binaries to the repo's spec.</summary>
    private static string? FindSpec()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "specs", "openapi.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
