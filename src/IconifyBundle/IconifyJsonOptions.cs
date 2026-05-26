namespace IconifyBundle;

/// <summary>Options controlling how <see cref="IconifyJson"/> writes iconify-format JSON.</summary>
public sealed record IconifyJsonOptions
{
    /// <summary>Pretty-print the JSON. Default is <c>false</c>, matching the published <c>@iconify-json/*</c> packs.</summary>
    public bool Indented { get; init; }

    /// <summary>
    /// When every icon shares the same <see cref="Icon.Width"/> and <see cref="Icon.Height"/>, factor those
    /// out as top-level <c>width</c>/<c>height</c> and omit them per-icon - the canonical iconify shape. When
    /// icons have varying intrinsic sizes, the size is always written per-icon. Default is <c>true</c>.
    /// </summary>
    public bool HoistCommonSize { get; init; } = true;

    /// <summary>An optional <c>info</c> block to embed in the output (name, author, license).</summary>
    public IconifyPackInfo? Info { get; init; }
}
