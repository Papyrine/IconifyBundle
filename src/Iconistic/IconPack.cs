using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Iconistic;

/// <summary>
/// Runtime accessor for a single icon pack. Loads icon data from the <c>iconistic.pack.json</c>
/// resource embedded in the <c>Iconistic.&lt;Pack&gt;</c> assembly, and (in <see cref="IconisticMode.Disk"/>)
/// resolves on-disk SVG file paths. Instances are cached per pack assembly.
/// </summary>
public sealed class IconPack
{
    static readonly ConcurrentDictionary<Assembly, IconPack> cache = new();

    readonly Assembly assembly;
    readonly Lazy<IReadOnlyDictionary<string, IconData>> icons;

    IconPack(Assembly assembly, string prefix, IconisticMode mode)
    {
        this.assembly = assembly;
        Prefix = prefix;
        Mode = mode;
        icons = new(() => Load(assembly));
    }

    /// <summary>The Iconify prefix for this pack, e.g. <c>feather</c>.</summary>
    public string Prefix { get; }

    /// <summary>The delivery mode the pack was generated for.</summary>
    public IconisticMode Mode { get; }

    /// <summary>All icon names available in the pack.</summary>
    public IEnumerable<string> Names => icons.Value.Keys;

    /// <summary>
    /// Gets (and caches) the pack accessor for the supplied pack <paramref name="assembly"/>.
    /// Called by the generated code via <c>typeof(...).Assembly</c>.
    /// </summary>
    public static IconPack ForAssembly(Assembly assembly, string prefix, IconisticMode mode) =>
        cache.GetOrAdd(assembly, _ => new(assembly, prefix, mode));

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
    /// The on-disk path to the icon's <c>.svg</c> file, relative to the application base directory.
    /// Only populated when the pack is consumed in <see cref="IconisticMode.Disk"/>.
    /// </summary>
    public string PathOf(string name) =>
        Path.Combine(AppContext.BaseDirectory, "iconistic", Prefix, name + ".svg");

    static IReadOnlyDictionary<string, IconData> Load(Assembly assembly)
    {
        using var stream = assembly.GetManifestResourceStream("iconistic.pack.json") ??
                           throw new InvalidOperationException(
                               $"Pack assembly '{assembly.GetName().Name}' does not contain the embedded 'iconistic.pack.json' resource.");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var defaultWidth = root.TryGetProperty("width", out var w) ? w.GetInt32() : 16;
        var defaultHeight = root.TryGetProperty("height", out var h) ? h.GetInt32() : 16;

        var result = new Dictionary<string, IconData>(StringComparer.Ordinal);
        foreach (var icon in root.GetProperty("icons").EnumerateObject())
        {
            var value = icon.Value;
            var body = value.GetProperty("body").GetString()!;
            var iconWidth = value.TryGetProperty("width", out var iw) ? iw.GetInt32() : defaultWidth;
            var iconHeight = value.TryGetProperty("height", out var ih) ? ih.GetInt32() : defaultHeight;
            result[icon.Name] = new(body, iconWidth, iconHeight);
        }

        return result;
    }

    readonly record struct IconData(string Body, int Width, int Height);
}
