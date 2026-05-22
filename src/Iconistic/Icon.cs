using System.Text;

namespace Iconistic;

/// <summary>
/// A single resolved icon: its <see cref="Name"/>, the inner SVG <see cref="Body"/> markup
/// (which uses <c>currentColor</c> so it inherits the surrounding text color), and the icon's
/// intrinsic <see cref="Width"/>/<see cref="Height"/>.
/// </summary>
public readonly struct Icon
{
    public Icon(string name, string body, int width, int height)
    {
        Name = name;
        Body = body;
        Width = width;
        Height = height;
    }

    /// <summary>The icon name within its pack, e.g. <c>activity</c>.</summary>
    public string Name { get; }

    /// <summary>The inner SVG markup (everything between the <c>&lt;svg&gt;</c> tags).</summary>
    public string Body { get; }

    /// <summary>The intrinsic width used for the <c>viewBox</c>.</summary>
    public int Width { get; }

    /// <summary>The intrinsic height used for the <c>viewBox</c>.</summary>
    public int Height { get; }

    /// <summary>The full standalone <c>&lt;svg&gt;</c> document for this icon.</summary>
    public string Svg => SvgBuilder.Build(this);

    /// <summary>A new read-only stream over the UTF-8 encoded <see cref="Svg"/>.</summary>
    public Stream OpenStream() => new MemoryStream(Encoding.UTF8.GetBytes(Svg), writable: false);

    public override string ToString() => Svg;
}
