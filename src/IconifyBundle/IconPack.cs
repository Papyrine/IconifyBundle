namespace IconifyBundle;

/// <summary>
/// Runtime accessor for a single icon pack. Loads icon data from the <c>iconifybundle.pack.json</c>
/// resource embedded in the <c>IconifyBundle.&lt;Pack&gt;</c> assembly, and resolves on-disk SVG file
/// paths (for packs whose consumer set <c>IconifyBundleExtractDisk</c>). Instances are cached per pack assembly.
/// </summary>
public sealed class IconPack
{
    static readonly ConcurrentDictionary<Assembly, IconPack> cache = new();

    readonly Lazy<IReadOnlyDictionary<string, IconData>> icons;

    IconPack(Assembly assembly, string prefix)
    {
        Prefix = prefix;
        icons = new(() => Load(assembly));
    }

    /// <summary>The Iconify prefix for this pack, e.g. <c>feather</c>.</summary>
    public string Prefix { get; }

    /// <summary>All icon names available in the pack.</summary>
    public IEnumerable<string> Names => icons.Value.Keys;

    /// <summary>
    /// Gets (and caches) the pack accessor for the supplied pack <paramref name="assembly"/>.
    /// Called by the generated code via <c>typeof(...).Assembly</c>.
    /// </summary>
    public static IconPack ForAssembly(Assembly assembly, string prefix) =>
        cache.GetOrAdd(assembly, _ => new(assembly, prefix));

    /// <summary>Resolves a single icon by name.</summary>
    /// <exception cref="KeyNotFoundException">No icon with that name exists in the pack.</exception>
    public Icon this[string name]
    {
        get
        {
            if (icons.Value.TryGetValue(name, out var data))
            {
                return new(name, data.Body, data.Width, data.Height);
            }

            throw new KeyNotFoundException($"No icon named '{name}' in pack '{Prefix}'.");
        }
    }

    /// <summary>Whether the pack contains an icon with the supplied name.</summary>
    public bool Contains(string name) => icons.Value.ContainsKey(name);

    /// <summary>
    /// The on-disk path to the icon's <c>.svg</c> file under the application base directory. The file
    /// is present only when the consumer set <c>IconifyBundleExtractDisk</c> to copy the pack's SVGs to output.
    /// </summary>
    public string PathOf(string name) =>
        Path.Combine(AppContext.BaseDirectory, "iconifybundle", Prefix, name + ".svg");

    static IReadOnlyDictionary<string, IconData> Load(Assembly assembly)
    {
        using var stream = assembly.GetManifestResourceStream("iconifybundle.pack.json") ??
                           throw new InvalidOperationException(
                               $"Pack assembly '{assembly.GetName().Name}' does not contain the embedded 'iconifybundle.pack.json' resource.");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var defaultWidth = root.TryGetProperty("width", out var w) ? w.GetDouble() : 16;
        var defaultHeight = root.TryGetProperty("height", out var h) ? h.GetDouble() : 16;

        var result = new Dictionary<string, IconData>(StringComparer.Ordinal);
        foreach (var icon in root.GetProperty("icons").EnumerateObject())
        {
            var value = icon.Value;
            var body = value.GetProperty("body").GetString()!;
            var iconWidth = value.TryGetProperty("width", out var iw) ? iw.GetDouble() : defaultWidth;
            var iconHeight = value.TryGetProperty("height", out var ih) ? ih.GetDouble() : defaultHeight;
            result[icon.Name] = new(body, iconWidth, iconHeight);
        }

        return result;
    }

    readonly record struct IconData(string Body, double Width, double Height);
}
