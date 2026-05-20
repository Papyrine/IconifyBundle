namespace Iconistic;

/// <summary>
/// An immutable, renderable Iconify icon: an SVG body plus its viewBox and any baked-in transform.
/// </summary>
/// <param name="Body">The inner SVG markup (paths etc.), using <c>currentColor</c> for fills.</param>
/// <param name="Width">The viewBox width.</param>
/// <param name="Height">The viewBox height.</param>
/// <param name="Left">The viewBox left offset. Usually 0.</param>
/// <param name="Top">The viewBox top offset. Usually 0.</param>
/// <param name="Rotate">Baked-in clockwise rotation, in quarter turns (0-3).</param>
/// <param name="HFlip">Whether the icon is flipped horizontally.</param>
/// <param name="VFlip">Whether the icon is flipped vertically.</param>
public readonly record struct IconisticIcon(
    string Body,
    int Width,
    int Height,
    int Left = 0,
    int Top = 0,
    int Rotate = 0,
    bool HFlip = false,
    bool VFlip = false)
{
    /// <summary>Renders the icon to a complete <c>&lt;svg&gt;</c> string.</summary>
    public string ToSvg(SvgOptions options = default) =>
        SvgComposer.Compose(this, options);

    /// <summary>Renders the icon, overriding color and optionally size (applied to both width and height).</summary>
    public string ToSvg(string? color, string? size = null) =>
        ToSvg(new()
        {
            Color = color,
            Width = size,
            Height = size
        });

    /// <inheritdoc />
    public override string ToString() => ToSvg(default(SvgOptions));
}
