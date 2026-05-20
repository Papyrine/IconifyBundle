using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Iconistic.Blazor;

/// <summary>
/// Renders an <see cref="IconisticIcon"/> inline as an SVG element.
/// </summary>
/// <example>
/// <code>&lt;Icon Value="Icons.Mdi.Home" Color="red" Size="32" /&gt;</code>
/// </example>
public sealed class Icon : ComponentBase
{
    /// <summary>The icon to render.</summary>
    [Parameter]
    [EditorRequired]
    public IconisticIcon Value { get; set; }

    /// <summary>CSS color, applied via <c>style="color:..."</c>.</summary>
    [Parameter]
    public string? Color { get; set; }

    /// <summary>Sets both width and height. Overridden by <see cref="Width"/>/<see cref="Height"/> when set.</summary>
    [Parameter]
    public string? Size { get; set; }

    /// <summary>SVG width. Defaults to <c>1em</c>.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>SVG height. Defaults to <c>1em</c>.</summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>Additional clockwise rotation in quarter turns (0-3).</summary>
    [Parameter]
    public int Rotate { get; set; }

    /// <summary>Flip horizontally.</summary>
    [Parameter]
    public bool FlipH { get; set; }

    /// <summary>Flip vertically.</summary>
    [Parameter]
    public bool FlipV { get; set; }

    /// <summary>Value for the <c>class</c> attribute on the root <c>&lt;svg&gt;</c>.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Extra CSS appended to the <c>style</c> attribute on the root <c>&lt;svg&gt;</c>.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var options = new SvgOptions
        {
            Color = Color,
            Width = Width ?? Size,
            Height = Height ?? Size,
            Rotate = Rotate,
            HFlip = FlipH,
            VFlip = FlipV,
            CssClass = Class,
            Style = Style
        };

        builder.AddMarkupContent(0, Value.ToSvg(options));
    }
}
