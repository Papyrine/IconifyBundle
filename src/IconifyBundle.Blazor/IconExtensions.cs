namespace IconifyBundle;

/// <summary>
/// Helpers for rendering an <see cref="Icon"/> in Blazor.
/// </summary>
public static class IconExtensions
{
    extension(Icon icon)
    {
        /// <summary>Renders the icon as a <see cref="MarkupString"/> of its full SVG.</summary>
        public MarkupString ToMarkup() =>
            new(icon.Svg);

        /// <summary>
        /// Renders the icon as a <see cref="MarkupString"/> with an optional rendered size and CSS class.
        /// </summary>
        public MarkupString ToMarkup(int? width, int? height, string? cssClass = null) =>
            new(SvgBuilder.Build(icon, width, height, cssClass));
    }
}
