namespace Iconistic;

/// <summary>
/// Helpers for rendering an <see cref="Icon"/> in Blazor.
/// </summary>
public static class IconExtensions
{
    /// <summary>Renders the icon as a <see cref="MarkupString"/> of its full SVG.</summary>
    public static MarkupString ToMarkup(this Icon icon) =>
        new(icon.Svg);

    /// <summary>
    /// Renders the icon as a <see cref="MarkupString"/> with an optional rendered size and CSS class.
    /// </summary>
    public static MarkupString ToMarkup(this Icon icon, int? width, int? height, string? cssClass = null) =>
        new(SvgBuilder.Build(icon, width, height, cssClass));
}
