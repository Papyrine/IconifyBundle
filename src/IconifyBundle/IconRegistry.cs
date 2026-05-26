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
