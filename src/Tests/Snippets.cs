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
}
