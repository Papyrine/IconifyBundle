namespace IconifyBundle;

/// <summary>
/// The process-wide store of materialised icons. The IconifyBundle source generator emits a
/// <c>[ModuleInitializer]</c> in each consuming assembly that registers the icons that assembly
/// actually uses - inline icon bodies in Resource mode, or on-disk file paths in Disk mode.
/// <see cref="IconPack"/> resolves icons through this store.
/// </summary>
public static class IconRegistry
{
    readonly record struct Entry(string? Body, string? Path, double Width, double Height);

    static readonly ConcurrentDictionary<(string Prefix, string Name), Entry> entries = new();

    /// <summary>
    /// Registers an icon whose <paramref name="body"/> is carried inline (Resource mode). Called by
    /// generated module initializers; not intended for direct use.
    /// </summary>
    public static void Register(string prefix, string name, string body, double width, double height) =>
        entries[(prefix, name)] = new(body, null, width, height);

    /// <summary>
    /// Registers an icon whose body is read from the file at <paramref name="path"/> on first access
    /// (Disk mode). Called by generated module initializers; not intended for direct use.
    /// </summary>
    public static void RegisterPath(string prefix, string name, string path, double width, double height) =>
        entries[(prefix, name)] = new(null, path, width, height);

    internal static bool Contains(string prefix, string name) =>
        entries.ContainsKey((prefix, name));

    internal static IEnumerable<string> Names(string prefix) =>
        entries.Keys
            .Where(_ => _.Prefix == prefix)
            .Select(_ => _.Name);

    internal static Icon Get(string prefix, string name)
    {
        if (entries.TryGetValue((prefix, name), out var entry))
        {
            var body = entry.Body ?? ExtractBody(File.ReadAllText(entry.Path!));
            return new(prefix, name, body, entry.Width, entry.Height);
        }

        throw new KeyNotFoundException(
            $"""
             Icon '{name}' in pack '{prefix}' was not materialised.
             Only icons referenced through the strongly-typed API (e.g. a 'Pack.Member' access the source generator can see) are bundled.
             Reference the icon directly, or check the name.
             """);
    }

    // ------------------------------------------------------------------
    // Rasters: PNG/BMP renderings produced at build time when the consumer sets
    // IconifyBundleRasterSizes (see the readme).
    // ------------------------------------------------------------------

    sealed class RasterEntry(RasterData? data, int offset, int length)
    {
        byte[]? bytes;

        // Resource mode slices the generated data; Disk mode (no data) reads the file the build wrote.
        public byte[] Bytes(string prefix, string name, string format, int size)
        {
            if (bytes != null)
            {
                return bytes;
            }

            if (data == null)
            {
                return bytes = File.ReadAllBytes(IconPack.ForPrefix(prefix).RasterPathOf(name, format, size));
            }

            return bytes = data().Slice(offset, length).ToArray();
        }
    }

    static readonly ConcurrentDictionary<(string Prefix, string Name, string Format, int Size), RasterEntry> rasters = new();

    /// <summary>
    /// Registers a raster carried in the consuming assembly (Resource mode): <paramref name="length"/> bytes
    /// at <paramref name="offset"/> in the span <paramref name="data"/> returns. Called by generated module
    /// initializers; not intended for direct use.
    /// </summary>
    public static void RegisterRaster(string prefix, string name, string format, int size, RasterData data, int offset, int length) =>
        rasters[(prefix, name, format, size)] = new(data, offset, length);

    /// <summary>
    /// Registers a raster the build wrote to <c>iconifybundle/&lt;prefix&gt;/&lt;size&gt;/&lt;name&gt;.&lt;format&gt;</c>
    /// under the application base directory (Disk mode). Called by generated module initializers; not
    /// intended for direct use.
    /// </summary>
    public static void RegisterRasterFile(string prefix, string name, string format, int size) =>
        rasters[(prefix, name, format, size)] = new(null, 0, 0);

    internal static byte[] GetRaster(string prefix, string name, string format, int size)
    {
        if (rasters.TryGetValue((prefix, name, format, size), out var entry))
        {
            return entry.Bytes(prefix, name, format, size);
        }

        var sizes = RasterSizes(prefix, name, format);
        if (sizes.Count == 0)
        {
            throw new KeyNotFoundException(
                $"""
                 No {format.ToUpperInvariant()} was produced for icon '{name}' in pack '{prefix}'.
                 Rasters are produced at build time only when IconifyBundleRasterSizes is set (and IconifyBundleRasterFormat includes '{format}'), and only for icons referenced through the strongly-typed API.
                 """);
        }

        throw new KeyNotFoundException(
            $"""
             No {size}px {format.ToUpperInvariant()} was produced for icon '{name}' in pack '{prefix}'.
             Available sizes: {string.Join(", ", sizes)}. Add {size} to IconifyBundleRasterSizes to produce it.
             """);
    }

    internal static IReadOnlyList<int> RasterSizes(string prefix, string name, string format) =>
        rasters.Keys
            .Where(_ => _.Prefix == prefix && _.Name == name && _.Format == format)
            .Select(_ => _.Size)
            .Order()
            .ToList();

    // Disk-mode files are full standalone <svg> documents written by SvgBuilder; recover the inner body.
    // Attribute values never contain '>' (they are numbers/simple strings), so the first '>' ends the
    // opening tag.
    static string ExtractBody(string svg)
    {
        var open = svg.IndexOf('>');
        var close = svg.LastIndexOf("</svg>", StringComparison.Ordinal);
        if (open < 0 || close < open)
        {
            return svg;
        }

        return svg.Substring(open + 1, close - open - 1);
    }
}
