namespace IconifyBundle;

/// <summary>
/// Runtime accessor for a single icon pack. Resolves icons from the <see cref="IconRegistry"/>, which
/// the source generator populates per consuming assembly with only the icons that assembly uses.
/// Instances are cached per prefix.
/// </summary>
public sealed class IconPack
{
    static readonly ConcurrentDictionary<string, IconPack> cache = new(StringComparer.Ordinal);

    IconPack(string prefix) => Prefix = prefix;

    /// <summary>The Iconify prefix for this pack, e.g. <c>feather</c>.</summary>
    public string Prefix { get; }

    /// <summary>The names of the icons that have been materialised for this pack in the current process.</summary>
    public IEnumerable<string> Names => IconRegistry.Names(Prefix);

    /// <summary>
    /// The icons that have been materialised for this pack in the current process, resolved lazily.
    /// To enumerate the entire upstream pack (not just what the consumer has referenced), use
    /// <see cref="IconifyJson.ReadPack"/> against the pack class's <see cref="Type"/>.
    /// </summary>
    public IEnumerable<Icon> Icons => Names.Select(name => this[name]);

    /// <summary>
    /// Gets (and caches) the pack accessor for the supplied <paramref name="prefix"/>. Called by the
    /// strongly-typed pack class.
    /// </summary>
    public static IconPack ForPrefix(string prefix) =>
        cache.GetOrAdd(prefix, static prefix => new(prefix));

    /// <summary>Resolves a single icon by name.</summary>
    /// <exception cref="KeyNotFoundException">
    /// The icon was not materialised. Only icons the generator saw referenced through the strongly-typed
    /// API are bundled.
    /// </exception>
    public Icon this[string name] => IconRegistry.Get(Prefix, name);

    /// <summary>Whether the named icon has been materialised for this pack.</summary>
    public bool Contains(string name) => IconRegistry.Contains(Prefix, name);

    /// <summary>
    /// The on-disk path to the icon's <c>.svg</c> file under the application base directory. The file is
    /// present only in Disk mode, and only for icons that were materialised.
    /// </summary>
    public string PathOf(string name) =>
        Path.Combine(AppContext.BaseDirectory, "iconifybundle", Prefix, name + ".svg");

    /// <summary>
    /// The on-disk path to the icon's PNG at <paramref name="size"/> pixels:
    /// <c>iconifybundle/&lt;prefix&gt;/&lt;size&gt;/&lt;name&gt;.png</c> under the application base directory.
    /// The file is present only in Disk mode with <c>IconifyBundleRasterSizes</c> set, and only for icons
    /// that were materialised.
    /// </summary>
    public string PngPathOf(string name, int size) =>
        RasterPathOf(name, "png", size);

    /// <summary>
    /// The on-disk path to the icon's BMP at <paramref name="size"/> pixels (see <see cref="PngPathOf"/>).
    /// </summary>
    public string BmpPathOf(string name, int size) =>
        RasterPathOf(name, "bmp", size);

    internal string RasterPathOf(string name, string format, int size) =>
        Path.Combine(AppContext.BaseDirectory, "iconifybundle", Prefix, size.ToString(CultureInfo.InvariantCulture), name + "." + format);
}
