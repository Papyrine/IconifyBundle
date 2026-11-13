namespace IconifyBundle;

/// <summary>
/// A single resolved icon: its <see cref="Prefix"/> (the iconify pack it belongs to, e.g. <c>feather</c>),
/// <see cref="Name"/>, the inner SVG <see cref="Body"/> markup (which uses <c>currentColor</c> so it
/// inherits the surrounding text color), and the icon's intrinsic <see cref="Width"/>/<see cref="Height"/>.
/// </summary>
public readonly struct Icon(
    string prefix,
    string name,
    [StringSyntax(StringSyntaxAttribute.Xml)] string body,
    double width,
    double height)
{
    /// <summary>The iconify pack prefix this icon belongs to, e.g. <c>feather</c>.</summary>
    public string Prefix { get; } = prefix;

    /// <summary>The icon name within its pack, e.g. <c>activity</c>.</summary>
    public string Name { get; } = name;

    /// <summary>The inner SVG markup (everything between the <c>&lt;svg&gt;</c> tags).</summary>
    [StringSyntax(StringSyntaxAttribute.Xml)]
    public string Body { get; } = body;

    /// <summary>The intrinsic width used for the <c>viewBox</c> (may be fractional, e.g. <c>25.9</c>).</summary>
    public double Width { get; } = width;

    /// <summary>The intrinsic height used for the <c>viewBox</c> (may be fractional).</summary>
    public double Height { get; } = height;

    /// <summary>
    /// Whether this is a real icon rather than a <c>default(Icon)</c>. <see cref="Icon"/> is a struct, so
    /// <c>default(Icon).Body</c> is null despite the non-null annotation; renderers use this to skip an unset icon.
    /// </summary>
    public bool HasBody =>
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        Body is not null;

    /// <summary>The full standalone <c>&lt;svg&gt;</c> document for this icon.</summary>
    public string Svg => SvgBuilder.Build(this);

    /// <summary>A new read-only stream over the UTF-8 encoded <see cref="Svg"/>.</summary>
    public Stream OpenStream() => new MemoryStream(Encoding.UTF8.GetBytes(Svg), writable: false);

    /// <summary>
    /// A read-only stream over the PNG rendered at build time at <paramref name="size"/> pixels square.
    /// Rasters exist only when the consuming project sets <c>IconifyBundleRasterSizes</c>, and only for the
    /// sizes listed there.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No PNG of that size was produced for this icon.</exception>
    public Stream OpenPng(int size) => new MemoryStream(IconRegistry.GetRaster(Prefix, Name, "png", size), writable: false);

    /// <summary>The bytes of the PNG rendered at build time at <paramref name="size"/> pixels square.</summary>
    /// <exception cref="KeyNotFoundException">No PNG of that size was produced for this icon.</exception>
    public ReadOnlyMemory<byte> Png(int size) => IconRegistry.GetRaster(Prefix, Name, "png", size);

    /// <summary>The sizes, ascending, of the PNGs produced for this icon (empty when there are none).</summary>
    public IReadOnlyList<int> PngSizes => IconRegistry.RasterSizes(Prefix, Name, "png");

    /// <summary>
    /// A read-only stream over the 32-bit BMP rendered at build time at <paramref name="size"/> pixels
    /// square. Produced only when <c>IconifyBundleRasterFormat</c> includes <c>bmp</c>.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No BMP of that size was produced for this icon.</exception>
    public Stream OpenBmp(int size) => new MemoryStream(IconRegistry.GetRaster(Prefix, Name, "bmp", size), writable: false);

    /// <summary>The bytes of the BMP rendered at build time at <paramref name="size"/> pixels square.</summary>
    /// <exception cref="KeyNotFoundException">No BMP of that size was produced for this icon.</exception>
    public ReadOnlyMemory<byte> Bmp(int size) => IconRegistry.GetRaster(Prefix, Name, "bmp", size);

    /// <summary>The sizes, ascending, of the BMPs produced for this icon (empty when there are none).</summary>
    public IReadOnlyList<int> BmpSizes => IconRegistry.RasterSizes(Prefix, Name, "bmp");

    public override string ToString() => Svg;
}
