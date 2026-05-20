namespace Iconistic;

/// <summary>
/// Rendering options for <see cref="IconisticIcon.ToSvg(SvgOptions)"/>.
/// </summary>
public readonly record struct SvgOptions
{
    /// <summary>CSS color applied via <c>style="color:..."</c>. The icon body uses <c>currentColor</c>.</summary>
    public string? Color { get; init; }

    /// <summary>SVG width, e.g. <c>"1em"</c>, <c>"24"</c>, <c>"24px"</c>. Defaults to <c>"1em"</c>.</summary>
    public string? Width { get; init; }

    /// <summary>SVG height, e.g. <c>"1em"</c>, <c>"24"</c>, <c>"24px"</c>. Defaults to <c>"1em"</c>.</summary>
    public string? Height { get; init; }

    /// <summary>Additional clockwise rotation in quarter turns (0-3), applied on top of any baked-in rotation.</summary>
    public int Rotate { get; init; }

    /// <summary>Additional horizontal flip, applied on top of any baked-in flip.</summary>
    public bool HFlip { get; init; }

    /// <summary>Additional vertical flip, applied on top of any baked-in flip.</summary>
    public bool VFlip { get; init; }

    /// <summary>Value for the <c>class</c> attribute on the root <c>&lt;svg&gt;</c>.</summary>
    public string? CssClass { get; init; }

    /// <summary>Extra CSS appended to the <c>style</c> attribute on the root <c>&lt;svg&gt;</c>.</summary>
    public string? Style { get; init; }
}
