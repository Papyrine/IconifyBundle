static class PackSelection
{
    public sealed record ExcludedPack(string Prefix, ExclusionReason Reason);

    public sealed record Result(List<string> Prefixes, List<ExcludedPack> Excluded);

    /// <summary>
    /// Resolves which packs to build: every collection listed by the Iconify API, minus any whose license
    /// is incompatible with redistribution in a public, commercially-consumable NuGet (non-commercial or
    /// copyleft). The excluded packs are reported so the generated readme can explain their absence.
    /// </summary>
    public static async Task<Result> ResolveAsync(HttpCache cache)
    {
        var json = await cache.StringAsync("https://api.iconify.design/collections");
        using var document = JsonDocument.Parse(json);

        var prefixes = new List<string>();
        var excluded = new List<ExcludedPack>();
        foreach (var collection in document.RootElement.EnumerateObject())
        {
            if (Exclusion(collection.Value) is { } reason)
            {
                excluded.Add(new(collection.Name, reason));
            }
            else
            {
                prefixes.Add(collection.Name);
            }
        }

        excluded.Sort((a, b) => string.Compare(a.Prefix, b.Prefix, StringComparison.OrdinalIgnoreCase));
        return new(prefixes, excluded);
    }

    static ExclusionReason? Exclusion(JsonElement collection)
    {
        if (!collection.TryGetProperty("license", out var license))
        {
            return null;
        }

        var spdx = license.TryGetProperty("spdx", out var s) ? s.GetString() ?? "" : "";
        var title = license.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";

        // Non-commercial Creative Commons (the NC token in the SPDX id, e.g. CC-BY-NC-4.0 / CC-BY-NC-SA-4.0):
        // the clause forbids commercial use, which a public NuGet cannot honour.
        if (spdx.Split('-').Contains("NC") ||
            title.Contains("-NC", StringComparison.OrdinalIgnoreCase))
        {
            return ExclusionReason.NonCommercial;
        }

        // Copyleft GPL family (GPL-2.0-*, GPL-3.0-*): its terms would flow onto every downstream consumer.
        if (spdx.Contains("GPL", StringComparison.OrdinalIgnoreCase) ||
            title.Contains("GPL", StringComparison.OrdinalIgnoreCase))
        {
            return ExclusionReason.Copyleft;
        }

        return null;
    }
}
