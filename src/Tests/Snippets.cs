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
