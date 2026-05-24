namespace IconifyBundle.Build;

/// <summary>The body markup and intrinsic size of a single icon, parsed from a pack <c>.icondata</c> file.</summary>
public readonly struct IconEntry(string body, double width, double height)
{
    public string Body { get; } = body;
    public double Width { get; } = width;
    public double Height { get; } = height;
}