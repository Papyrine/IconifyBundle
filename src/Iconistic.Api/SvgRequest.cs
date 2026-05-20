using System.Text;

namespace Iconistic.Api;

/// <summary>
/// Query options for the Iconify server-side SVG endpoint
/// (<c>/{prefix}/{name}.svg</c>).
/// </summary>
public readonly record struct SvgRequest
{
    /// <summary>Color for monotone icons, e.g. <c>"red"</c> or <c>"%23ff8800"</c>.</summary>
    public string? Color { get; init; }

    /// <summary>Width, e.g. <c>"24"</c>, <c>"1em"</c>, <c>"auto"</c>.</summary>
    public string? Width { get; init; }

    /// <summary>Height, e.g. <c>"24"</c>, <c>"1em"</c>, <c>"auto"</c>.</summary>
    public string? Height { get; init; }

    /// <summary>Flip horizontally.</summary>
    public bool FlipHorizontal { get; init; }

    /// <summary>Flip vertically.</summary>
    public bool FlipVertical { get; init; }

    /// <summary>Clockwise rotation in quarter turns (0-3).</summary>
    public int Rotate { get; init; }

    /// <summary>Add a transparent bounding box to the icon.</summary>
    public bool Box { get; init; }

    internal string ToQueryString()
    {
        var parts = new List<string>();

        if (Color is { Length: > 0 })
        {
            parts.Add("color=" + Uri.EscapeDataString(Color));
        }

        if (Width is { Length: > 0 })
        {
            parts.Add("width=" + Uri.EscapeDataString(Width));
        }

        if (Height is { Length: > 0 })
        {
            parts.Add("height=" + Uri.EscapeDataString(Height));
        }

        var flip = (FlipHorizontal, FlipVertical) switch
        {
            (true, true) => "horizontal,vertical",
            (true, false) => "horizontal",
            (false, true) => "vertical",
            _ => null
        };
        if (flip is not null)
        {
            parts.Add("flip=" + flip);
        }

        if (Rotate % 4 != 0)
        {
            parts.Add("rotate=" + (((Rotate % 4) + 4) % 4) * 90 + "deg");
        }

        if (Box)
        {
            parts.Add("box=true");
        }

        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }
}
