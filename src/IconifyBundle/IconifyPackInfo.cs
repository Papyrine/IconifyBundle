namespace IconifyBundle;

/// <summary>
/// Optional metadata about an iconify pack (the <c>info</c> block in iconify JSON). Each
/// member maps to its iconify-spec field; any not supplied is omitted from the output.
/// </summary>
public sealed record IconifyPackInfo(
    string? Name = null,
    string? Author = null,
    string? License = null);
