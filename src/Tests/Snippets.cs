// Code samples surfaced in the readme via MarkdownSnippets.
static class Snippets
{
    public static void RuntimeUsage()
    {
        #region RuntimeUsage
        // An Icon carries the pack prefix, the icon name, the inner SVG body, and intrinsic size.
        var icon = new Icon(
            "feather",
            "activity",
            """<path stroke="currentColor" d="M12 2v20"/>""",
            24,
            24);

        // full <svg> document
        var svg = icon.Svg;

        // UTF-8 stream of the SVG
        using var stream = icon.OpenStream();
        #endregion

        Console.WriteLine(svg);
        Console.WriteLine(stream.Length);
    }

    public static void IconifyJsonSerialise()
    {
        #region IconifyJsonSerialise
        // Pick the icons you want (strongly-typed members from any IconifyBundle.<Pack> work here,
        // e.g. Feather.Box, AntDesign.HomeOutlined - constructed inline for the snippet). Each icon
        // carries its pack prefix, so the prefix is derived from the icons - no need to pass it.
        var box = new Icon("sample", "box", """<path d="M3 3h18v18H3z"/>""", 24, 24);
        var ring = new Icon("sample", "ring", """<circle cx="12" cy="12" r="8"/>""", 24, 24);

        // As a JSON string...
        var json = IconifyJson.Serialize(box, ring);

        // ...or as a stream (handy for feeding into a consumer that takes iconify JSON, e.g.
        // Naiad's IconPack.Load).
        using var stream = IconifyJson.OpenReadStream(box, ring);

        // ...or write directly to a file (sync/async).
        IconifyJson.WriteToFile("sample.json", [box, ring]);
        #endregion

        Console.WriteLine(json.Length);
        Console.WriteLine(stream.Length);
    }

    public static void IconifyJsonRead()
    {
        #region IconifyJsonRead
        // Parse iconify-format JSON back into an IconifyPack (prefix + icons + optional info).
        const string source =
            """
            {"prefix":"sample","width":24,"height":24,"icons":{"box":{"body":"<rect/>"}}}
            """;
        var pack = IconifyJson.Parse(source);

        Console.WriteLine(pack.Prefix);                 // "sample"
        Console.WriteLine(pack.Icons.Count);            // 1
        foreach (var icon in pack.Icons)
        {
            Console.WriteLine($"{icon.Name}: {icon.Body} ({icon.Width}x{icon.Height})");
        }
        #endregion
    }

    public static void IconifyJsonUpstream(Type packClass)
    {
        #region IconifyJsonUpstream
        // Open the full upstream pack data embedded in any IconifyBundle.<Pack> assembly. Pass the
        // strongly-typed pack class (e.g. typeof(Feather)) - the result is the entire
        // @iconify-json/<pack> dataset, not just the icons your project has materialised.
        using var stream = IconifyJson.OpenPackStream(packClass);

        // Or get the parsed pack back directly.
        var pack = IconifyJson.ReadPack(packClass);
        Console.WriteLine($"{pack.Prefix}: {pack.Icons.Count} icons");
        #endregion

        Console.WriteLine(stream.Length);
    }
}
