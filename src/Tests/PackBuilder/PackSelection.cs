static class PackSelection
{
    /// <summary>
    /// Resolves which packs to build: every collection listed by the Iconify API.
    /// </summary>
    public static async Task<List<string>> ResolveAsync(HttpCache cache)
    {
        var json = await cache.StringAsync("https://api.iconify.design/collections");
        using var document = JsonDocument.Parse(json);
        return document.RootElement
            .EnumerateObject()
            .Select(_ => _.Name)
            .ToList();
    }
}
