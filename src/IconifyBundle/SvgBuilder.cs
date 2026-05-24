namespace IconifyBundle;

/// <summary>
/// Wraps an icon <see cref="Icon.Body"/> into a complete, standalone <c>&lt;svg&gt;</c> document.
/// </summary>
public static class SvgBuilder
{
    const string xmlns = "http://www.w3.org/2000/svg";

    /// <summary>Builds the SVG using the icon's intrinsic dimensions.</summary>
    public static string Build(Icon icon) =>
        Build(icon, null, null, null);

    /// <summary>
    /// Builds the SVG with an optional rendered <paramref name="width"/>/<paramref name="height"/>
    /// (the <c>viewBox</c> always uses the intrinsic dimensions) and an optional CSS
    /// <paramref name="cssClass"/>.
    /// </summary>
    public static string Build(Icon icon, int? width, int? height, string? cssClass)
    {
        // A default(Icon) has no body; render nothing rather than an empty <svg> shell.
        if (!icon.HasBody)
        {
            return "";
        }

        var renderWidth = Format(width ?? icon.Width);
        var renderHeight = Format(height ?? icon.Height);
        var viewBox = $"0 0 {Format(icon.Width)} {Format(icon.Height)}";
        var cssAttribute = string.IsNullOrEmpty(cssClass) ? "" : $" class=\"{cssClass}\"";

        return $"""
                <svg xmlns="{xmlns}" width="{renderWidth}" height="{renderHeight}" viewBox="{viewBox}"{cssAttribute}>{icon.Body}</svg>
                """;
    }

    // SVG numbers are always '.'-decimal and culture-independent.
    static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
