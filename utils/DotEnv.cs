namespace Integration.Tests.utils;

/// <summary>
/// Minimal .env loader (KEY=VALUE lines) so the target URL and any test-account
/// credentials come from an untracked env file, never from source.
/// </summary>
public static class DotEnv
{
    public static void Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"env file '{filePath}' not found - relying on process env vars.");
            return;
        }

        foreach (var line in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();
            if (!string.IsNullOrEmpty(value))
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    public static void LoadForCurrentEnvironment()
    {
        var env = Environment.GetEnvironmentVariable("ENV")?.ToLowerInvariant() switch
        {
            "qa" => "qa.env",
            _ => ".env",
        };
        Load(Path.Combine(AppContext.BaseDirectory, "env", env));
    }
}
