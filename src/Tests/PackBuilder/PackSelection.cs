static class PackSelection
{
    // A small representative default: feather (286 icons) and lucide (1711 icons).
    static readonly string[] Default = ["feather", "lucide"];

    /// <summary>
    /// Resolves which packs to build. Override with the <c>ICONISTIC_PACKS</c> environment variable:
    /// a comma/semicolon separated list of prefixes, or <c>all</c> for every Iconify collection.
    /// </summary>
    public static async Task<IReadOnlyList<string>> ResolveAsync(HttpCache cache)
    {
        var env = Environment.GetEnvironmentVariable("ICONISTIC_PACKS");
        if (string.IsNullOrWhiteSpace(env))
        {
            return Default;
        }

        if (string.Equals(env, "all", StringComparison.OrdinalIgnoreCase))
        {
            var json = await cache.StringAsync("https://api.iconify.design/collections");
            using var document = JsonDocument.Parse(json);
            return document.RootElement
                .EnumerateObject()
                .Select(_ => _.Name)
                .ToList();
        }

        return env
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
