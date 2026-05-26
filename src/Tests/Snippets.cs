// Code samples surfaced in the readme via MarkdownSnippets.
static class Snippets
{
    public static void RuntimeUsage()
    {
        #region RuntimeUsage
        // An Icon carries the inner SVG body and intrinsic size.
        var icon = new Icon(
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
        // e.g. Feather.Box, AntDesign.HomeOutlined - constructed inline for the snippet).
        var box = new Icon("box", """<path d="M3 3h18v18H3z"/>""", 24, 24);
        var ring = new Icon("ring", """<circle cx="12" cy="12" r="8"/>""", 24, 24);

        // As a JSON string under a chosen prefix...
        var json = IconifyJson.Serialize("sample", box, ring);

        // ...or as a stream (handy for feeding into a consumer that takes iconify JSON, e.g.
        // Naiad's IconPack.Load).
        using var stream = IconifyJson.OpenReadStream("sample", box, ring);

        // ...or write directly to a file (sync/async).
        IconifyJson.WriteToFile("sample.json", "sample", [box, ring]);
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
