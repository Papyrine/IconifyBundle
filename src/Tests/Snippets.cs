// Code samples surfaced in the readme via MarkdownSnippets.
static class Snippets
{
    public static void RuntimeUsage()
    {
        #region RuntimeUsage
        // An Icon carries the inner SVG body and intrinsic size.
        var icon = new Icon("activity", "<path stroke=\"currentColor\" d=\"M12 2v20\"/>", 24, 24);

        var svg = icon.Svg;                   // full <svg> document
        using var stream = icon.OpenStream(); // UTF-8 stream of the SVG
        #endregion

        Console.WriteLine(svg);
        Console.WriteLine(stream.Length);
    }
}
