using System.Globalization;

namespace Iconistic;

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
        var builder = new StringBuilder();
        builder.Append("<svg xmlns=\"").Append(xmlns).Append('"');
        builder.Append(" width=\"").Append(Format(width ?? icon.Width)).Append('"');
        builder.Append(" height=\"").Append(Format(height ?? icon.Height)).Append('"');
        builder.Append(" viewBox=\"0 0 ").Append(Format(icon.Width)).Append(' ').Append(Format(icon.Height)).Append('"');
        if (!string.IsNullOrEmpty(cssClass))
        {
            builder.Append(" class=\"").Append(cssClass).Append('"');
        }

        builder.Append('>');
        builder.Append(icon.Body);
        builder.Append("</svg>");
        return builder.ToString();
    }

    // SVG numbers are always '.'-decimal and culture-independent.
    static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
