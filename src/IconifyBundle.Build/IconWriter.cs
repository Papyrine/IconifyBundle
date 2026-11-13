namespace IconifyBundle.Build;

/// <summary>
/// The reconstruction logic behind the <see cref="WriteUsedIcons"/> MSBuild task, factored out as plain
/// (MSBuild-free) static methods so it can be unit tested directly.
/// </summary>
public static class IconWriter
{
    /// <summary>
    /// Builds the standalone SVG document. Must stay byte-for-byte equal to
    /// <c>IconifyBundle.SvgBuilder.Build(Icon)</c> (asserted by a unit test).
    /// </summary>
    public static string BuildSvg(double width, double height, string body)
    {
        var w = width.ToString(CultureInfo.InvariantCulture);
        var h = height.ToString(CultureInfo.InvariantCulture);
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\">{body}</svg>";
    }

    /// <summary>
    /// Writes the standalone SVG for each used icon to <paramref name="outputDir"/> (skipping unchanged
    /// files). Returns any requested names not present in the data file.
    /// </summary>
    public static List<string> Write(string iconDataPath, IEnumerable<string> usedNames, string outputDir) =>
        Write(ParseIconData(File.ReadAllText(iconDataPath)), usedNames, outputDir);

    /// <summary>
    /// Writes the standalone SVG for each used icon in <paramref name="data"/> to
    /// <paramref name="outputDir"/> (skipping unchanged files). Returns any requested names not in the data.
    /// </summary>
    public static List<string> Write(IReadOnlyDictionary<string, IconEntry> data, IEnumerable<string> usedNames, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        var missing = new List<string>();
        foreach (var name in usedNames)
        {
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (!data.TryGetValue(name, out var icon))
            {
                missing.Add(name);
                continue;
            }

            var path = Path.Combine(outputDir, name + ".svg");
            var svg = BuildSvg(icon.Width, icon.Height, icon.Body);
            if (!File.Exists(path) ||
                File.ReadAllText(path) != svg)
            {
                File.WriteAllText(path, svg);
            }
        }

        return missing;
    }

    /// <summary>
    /// Renders each used icon at each requested size and format, from the raw MSBuild property values
    /// (<c>IconifyBundleRasterFormat</c>, <c>IconifyBundleRasterSizes</c>, <c>IconifyBundleRasterColor</c>),
    /// to <c>&lt;outputDir&gt;/&lt;size&gt;/&lt;name&gt;.&lt;format&gt;</c>, skipping unchanged files. The bytes
    /// match what the generator embeds in Resource mode. Returns a message per icon that could not be rendered.
    /// </summary>
    /// <exception cref="ArgumentException">The property values are not valid.</exception>
    public static List<string> WriteRasters(
        IReadOnlyDictionary<string, IconEntry> data,
        IEnumerable<string> usedNames,
        string outputDir,
        string? formats,
        string? sizes,
        string? color)
    {
        var options = RasterOptions.Parse(formats, sizes, color);
        if (options.Error != null)
        {
            throw new ArgumentException(options.Error);
        }

        return WriteRasters(data, usedNames, outputDir, options);
    }

    internal static List<string> WriteRasters(
        IReadOnlyDictionary<string, IconEntry> data,
        IEnumerable<string> usedNames,
        string outputDir,
        RasterOptions options)
    {
        var failures = new List<string>();
        if (!options.Enabled)
        {
            return failures;
        }

        foreach (var name in usedNames)
        {
            if (string.IsNullOrEmpty(name) ||
                !data.TryGetValue(name, out var icon))
            {
                continue;
            }

            try
            {
                foreach (var size in options.Sizes)
                {
                    var image = IconRaster.Render(icon.Body, icon.Width, icon.Height, size, options.Color);
                    var directory = Path.Combine(outputDir, size.ToString(CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(directory);
                    foreach (var format in options.Formats)
                    {
                        WriteIfChanged(Path.Combine(directory, $"{name}.{format}"), IconRaster.Encode(image, format));
                    }
                }
            }
            catch (Exception exception)
            {
                failures.Add($"icon '{name}' could not be rasterised: {exception.Message}");
            }
        }

        return failures;
    }

    static void WriteIfChanged(string path, byte[] bytes)
    {
        if (File.Exists(path) &&
            File.ReadAllBytes(path).SequenceEqual(bytes))
        {
            return;
        }

        File.WriteAllBytes(path, bytes);
    }

    /// <summary>
    /// Parses the pack data file (the format produced by <c>IconifyBundle.Generator.Manifest</c>):
    /// <c>key=value</c> header lines, a blank line, then <c>name\twidth\theight\tbody</c> per icon.
    /// </summary>
    public static Dictionary<string, IconEntry> ParseIconData(string content)
    {
        var result = new Dictionary<string, IconEntry>(StringComparer.Ordinal);
        var inHeader = true;
        foreach (var raw in content.Split('\n'))
        {
            var line = raw.Trim();
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

                if (line.IndexOf('=') > 0)
                {
                    continue;
                }

                inHeader = false;
            }

            if (line.Length == 0 ||
                line[0] == '#')
            {
                continue;
            }

            var parts = line.Split(separators, 4);
            if (parts.Length == 4 &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
            {
                result[parts[0]] = new(Unescape(parts[3]), width, height);
            }
        }

        return result;
    }

    static readonly char[] separators = ['\t'];

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
