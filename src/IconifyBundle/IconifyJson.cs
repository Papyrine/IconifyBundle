namespace IconifyBundle;

/// <summary>
/// Reads and writes the iconify-format JSON used by Iconify (the same format published as
/// <c>@iconify-json/*</c> packages, and consumed by tools like Mermaid and Naiad). Supports three flows:
/// <list type="bullet">
/// <item><description>Serialise an arbitrary set of <see cref="Icon"/>s (e.g. <c>Feather.Box</c>,
/// <c>Feather.Database</c>) into iconify JSON.</description></item>
/// <item><description>Parse iconify JSON back into an <see cref="IconifyPack"/>.</description></item>
/// <item><description>Open the full upstream pack data embedded in a generated pack assembly
/// (e.g. <c>IconifyJson.OpenPackStream(typeof(Feather))</c>) - the entire <c>@iconify-json/&lt;pack&gt;</c>
/// dataset, not just the icons the consumer has referenced.</description></item>
/// </list>
/// </summary>
public static class IconifyJson
{
    // Resource name applied to every pack assembly's embedded .icondata by packs/Directory.Build.props.
    // Uniform across all pack assemblies so a single lookup works for any pack.
    const string icondataResourceName = "IconifyBundle.icondata";

    static JsonWriterOptions defaultWriterOptions = new()
    {
        // SVG bodies use literal '<', '>', '&', '"'; relaxed escaping keeps them readable and matches
        // how published @iconify-json/* packs look on disk.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Serialises <paramref name="icons"/> under <paramref name="prefix"/> as iconify-format JSON.</summary>
    public static string Serialize(string prefix, IEnumerable<Icon> icons, IconifyJsonOptions? options = null)
    {
        using var buffer = new MemoryStream();
        WriteTo(buffer, prefix, icons, options);
        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    /// <summary>Serialises <paramref name="icons"/> under <paramref name="prefix"/> as iconify-format JSON.</summary>
    public static string Serialize(string prefix, params Icon[] icons) =>
        Serialize(prefix, (IEnumerable<Icon>)icons);

    /// <summary>
    /// Returns a readable, seekable UTF-8 stream over the iconify-format JSON. Position is 0; caller disposes.
    /// </summary>
    public static Stream OpenReadStream(string prefix, IEnumerable<Icon> icons, IconifyJsonOptions? options = null)
    {
        var buffer = new MemoryStream();
        WriteTo(buffer, prefix, icons, options);
        buffer.Position = 0;
        return buffer;
    }

    /// <summary>
    /// Returns a readable, seekable UTF-8 stream over the iconify-format JSON. Position is 0; caller disposes.
    /// </summary>
    public static Stream OpenReadStream(string prefix, params Icon[] icons) =>
        OpenReadStream(prefix, (IEnumerable<Icon>)icons);

    /// <summary>
    /// Writes iconify-format JSON for <paramref name="icons"/> under <paramref name="prefix"/> to
    /// <paramref name="destination"/>. Does not close the stream.
    /// </summary>
    public static void WriteTo(
        Stream destination,
        string prefix,
        IEnumerable<Icon> icons,
        IconifyJsonOptions? options = null)
    {
        options ??= new();
        var iconList = ValidateAndMaterialise(prefix, icons);
        using var writer = new Utf8JsonWriter(destination, WriterOptions(options));
        WriteRoot(writer, prefix, iconList, options);
    }

    /// <summary>Async overload of <see cref="WriteTo(Stream,string,IEnumerable{Icon},IconifyJsonOptions)"/>.</summary>
    public static async Task WriteToAsync(
        Stream destination,
        string prefix,
        IEnumerable<Icon> icons,
        IconifyJsonOptions? options = null,
        Cancel cancel = default)
    {
        options ??= new();
        var iconList = ValidateAndMaterialise(prefix, icons);
        await using var writer = new Utf8JsonWriter(destination, WriterOptions(options));
        WriteRoot(writer, prefix, iconList, options);
        await writer.FlushAsync(cancel);
    }

    /// <summary>Writes the iconify-format JSON to a file (overwriting any existing file).</summary>
    public static void WriteToFile(
        string path,
        string prefix,
        IEnumerable<Icon> icons,
        IconifyJsonOptions? options = null)
    {
        using var stream = File.Create(path);
        WriteTo(stream, prefix, icons, options);
    }

    /// <summary>Async overload of <see cref="WriteToFile(string,string,IEnumerable{Icon},IconifyJsonOptions)"/>.</summary>
    public static async Task WriteToFileAsync(
        string path,
        string prefix,
        IEnumerable<Icon> icons,
        IconifyJsonOptions? options = null,
        Cancel cancel = default)
    {
        await using var stream = File.Create(path);
        await WriteToAsync(stream, prefix, icons, options, cancel);
    }

    /// <summary>Serialises the materialised icons of <paramref name="pack"/> as iconify-format JSON.</summary>
    public static string Serialize(IconPack pack, IconifyJsonOptions? options = null) =>
        Serialize(pack.Prefix, pack.Icons, options);

    /// <summary>Returns a readable, seekable UTF-8 stream over the iconify JSON for <paramref name="pack"/>.</summary>
    public static Stream OpenReadStream(IconPack pack, IconifyJsonOptions? options = null) =>
        OpenReadStream(pack.Prefix, pack.Icons, options);

    /// <summary>Writes iconify-format JSON for <paramref name="pack"/> to <paramref name="destination"/>.</summary>
    public static void WriteTo(Stream destination, IconPack pack, IconifyJsonOptions? options = null) =>
        WriteTo(destination, pack.Prefix, pack.Icons, options);

    /// <summary>Async overload of <see cref="WriteTo(Stream,IconPack,IconifyJsonOptions)"/>.</summary>
    public static Task WriteToAsync(
        Stream destination,
        IconPack pack,
        IconifyJsonOptions? options = null,
        Cancel cancel = default) =>
        WriteToAsync(destination, pack.Prefix, pack.Icons, options, cancel);

    /// <summary>Writes the iconify-format JSON for <paramref name="pack"/> to a file (overwriting).</summary>
    public static void WriteToFile(string path, IconPack pack, IconifyJsonOptions? options = null) =>
        WriteToFile(path, pack.Prefix, pack.Icons, options);

    /// <summary>Async overload of <see cref="WriteToFile(string,IconPack,IconifyJsonOptions)"/>.</summary>
    public static Task WriteToFileAsync(
        string path,
        IconPack pack,
        IconifyJsonOptions? options = null,
        Cancel cancel = default) =>
        WriteToFileAsync(path, pack.Prefix, pack.Icons, options, cancel);

    /// <summary>Parses iconify-format JSON from <paramref name="source"/>.</summary>
    public static IconifyPack Read(Stream source)
    {
        using var doc = JsonDocument.Parse(source);
        return Parse(doc.RootElement);
    }

    /// <summary>Async overload of <see cref="Read(Stream)"/>.</summary>
    public static async Task<IconifyPack> ReadAsync(Stream source, Cancel cancel = default)
    {
        using var doc = await JsonDocument.ParseAsync(source, cancellationToken: cancel);
        return Parse(doc.RootElement);
    }

    /// <summary>Parses iconify-format JSON from a string.</summary>
    public static IconifyPack Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Parse(doc.RootElement);
    }

    // ------------------------------------------------------------------
    // Upstream pack passthrough (Option B): read the full @iconify-json/<pack>
    // dataset embedded in a generated pack assembly.
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns a readable, seekable UTF-8 stream of iconify-format JSON for the full upstream pack
    /// embedded in the assembly that defines <paramref name="packClass"/>. The result is the entire
    /// <c>@iconify-json/&lt;pack&gt;</c> dataset, not just the icons the current consumer has materialised.
    /// </summary>
    /// <param name="packClass">A pack class such as <c>typeof(IconifyBundle.Feather)</c>.</param>
    /// <exception cref="InvalidOperationException">
    /// The assembly does not embed pack data - typically because it was built before the
    /// <c>packs/Directory.Build.props</c> resource entry was added.
    /// </exception>
    public static Stream OpenPackStream(Type packClass)
    {
        var pack = ReadPack(packClass);
        return OpenReadStream(pack.Prefix, pack.Icons);
    }

    /// <summary>
    /// Parses the full upstream pack data embedded in the assembly that defines <paramref name="packClass"/>.
    /// </summary>
    /// <param name="packClass">A pack class such as <c>typeof(IconifyBundle.Feather)</c>.</param>
    /// <exception cref="InvalidOperationException">The assembly does not embed pack data.</exception>
    public static IconifyPack ReadPack(Type packClass)
    {
        var assembly = packClass.Assembly;
        using var stream = assembly.GetManifestResourceStream(icondataResourceName)
                           ?? throw new InvalidOperationException(
                               $"Assembly '{assembly.GetName().Name}' does not embed iconify pack data. " +
                               $"Rebuild the pack against an IconifyBundle that ships packs/Directory.Build.props.");
        return ReadIcondata(stream);
    }

    static JsonWriterOptions WriterOptions(IconifyJsonOptions options) =>
        options.Indented
            ? defaultWriterOptions with { Indented = true }
            : defaultWriterOptions;

    static List<Icon> ValidateAndMaterialise(string prefix, IEnumerable<Icon> icons)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix must not be empty.", nameof(prefix));
        }

        var list = icons.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("At least one icon is required.", nameof(icons));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var icon in list)
        {
            if (!icon.HasBody)
            {
                throw new ArgumentException(
                    $"Icon '{icon.Name}' has no body (likely a default(Icon)).",
                    nameof(icons));
            }

            if (!seen.Add(icon.Name))
            {
                throw new ArgumentException(
                    $"Duplicate icon name '{icon.Name}' in the pack.",
                    nameof(icons));
            }
        }

        return list;
    }

    static void WriteRoot(Utf8JsonWriter writer, string prefix, List<Icon> icons, IconifyJsonOptions options)
    {
        // If every icon shares the same intrinsic size, hoist it to the top level and omit per-icon -
        // the canonical shape used by @iconify-json/* packs.
        var hoist = options.HoistCommonSize &&
                    icons.All(i => i.Width == icons[0].Width && i.Height == icons[0].Height);

        writer.WriteStartObject();
        writer.WriteString("prefix", prefix);

        if (options.Info is { } info)
        {
            writer.WritePropertyName("info");
            writer.WriteStartObject();
            if (info.Name is { } n)
            {
                writer.WriteString("name", n);
            }
            if (info.Author is { } a)
            {
                writer.WriteString("author", a);
            }
            if (info.License is { } l)
            {
                writer.WriteString("license", l);
            }
            writer.WriteEndObject();
        }

        if (hoist)
        {
            writer.WriteNumber("width", icons[0].Width);
            writer.WriteNumber("height", icons[0].Height);
        }

        writer.WritePropertyName("icons");
        writer.WriteStartObject();
        foreach (var icon in icons)
        {
            writer.WritePropertyName(icon.Name);
            writer.WriteStartObject();
            writer.WriteString("body", icon.Body);
            if (!hoist)
            {
                writer.WriteNumber("width", icon.Width);
                writer.WriteNumber("height", icon.Height);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    static IconifyPack Parse(JsonElement root)
    {
        if (!root.TryGetProperty("prefix", out var prefixElement) ||
            prefixElement.GetString() is not { Length: > 0 } prefix)
        {
            throw new FormatException("Iconify JSON is missing a non-empty 'prefix'.");
        }

        var defaultWidth = root.TryGetProperty("width", out var w) ? w.GetDouble() : 16d;
        var defaultHeight = root.TryGetProperty("height", out var h) ? h.GetDouble() : 16d;

        IconifyPackInfo? info = null;
        if (root.TryGetProperty("info", out var infoElement) &&
            infoElement.ValueKind == JsonValueKind.Object)
        {
            info = new(
                Name: TryGetString(infoElement, "name"),
                Author: TryGetString(infoElement, "author"),
                License: TryGetString(infoElement, "license"));
        }

        var icons = new List<Icon>();
        if (root.TryGetProperty("icons", out var iconsElement) &&
            iconsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in iconsElement.EnumerateObject())
            {
                var iconObj = entry.Value;
                var body = iconObj.TryGetProperty("body", out var bodyElement)
                    ? bodyElement.GetString() ?? ""
                    : "";
                var iconWidth = iconObj.TryGetProperty("width", out var iw) ? iw.GetDouble() : defaultWidth;
                var iconHeight = iconObj.TryGetProperty("height", out var ih) ? ih.GetDouble() : defaultHeight;
                icons.Add(new(entry.Name, body, iconWidth, iconHeight));
            }
        }

        return new(prefix, icons, info);
    }

    static string? TryGetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    // Parses the .icondata pack format produced by IconifyBundle.Generator.Manifest:
    // 'key=value' header lines, a blank line, then 'name\twidth\theight\tbody' per icon.
    // Bodies are escaped (\\ \t \r \n) so they fit on one line.
    static IconifyPack ReadIcondata(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? prefix = null;
        var inHeader = true;
        var icons = new List<Icon>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (inHeader)
            {
                if (line.Length == 0)
                {
                    inHeader = false;
                    continue;
                }
                if (line[0] == '#')
                {
                    continue;
                }
                var equals = line.IndexOf('=');
                if (equals > 0)
                {
                    if (line[..equals] == "prefix")
                    {
                        prefix = line[(equals + 1)..];
                    }
                    continue;
                }
                inHeader = false; // fall through and parse as an icon line
            }

            if (line.Length == 0 ||
                line[0] == '#')
            {
                continue;
            }

            var parts = line.Split('\t', 4);
            if (parts.Length != 4)
            {
                continue;
            }

            if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
                !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
            {
                continue;
            }

            icons.Add(new(parts[0], Unescape(parts[3]), width, height));
        }

        if (prefix is null)
        {
            throw new FormatException("Embedded icondata is missing a 'prefix' header.");
        }

        return new(prefix, icons);
    }

    static string Unescape(string value)
    {
        if (value.IndexOf('\\') < 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c != '\\' ||
                i + 1 >= value.Length)
            {
                builder.Append(c);
                continue;
            }

            i++;
            builder.Append(value[i] switch
            {
                't' => '\t',
                'r' => '\r',
                'n' => '\n',
                var other => other
            });
        }

        return builder.ToString();
    }
}
