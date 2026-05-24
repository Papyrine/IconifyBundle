using System.Globalization;

namespace IconifyBundle.Generator;

/// <summary>The body markup and intrinsic size of a single icon.</summary>
public readonly record struct IconData(string Body, double Width, double Height);

/// <summary>
/// A parsed pack data file. Shipped by an <c>IconifyBundle.&lt;Pack&gt;</c> NuGet as an
/// <c>AdditionalFiles</c> entry (tagged with <c>IconifyBundlePack</c> metadata) so the generator can
/// discover which packs are referenced, the icons they contain, and (for the icons a consumer uses)
/// the body markup to materialise.
/// </summary>
public sealed class Manifest : IEquatable<Manifest>
{
    public string Prefix { get; }
    public string ClassName { get; }

    /// <summary>All icon names in the pack, in file order.</summary>
    public IReadOnlyList<string> IconNames { get; }

    /// <summary>
    /// Per-icon body/size data, keyed by icon name. Populated only for data lines that carry it
    /// (the shipped pack data file does; the bare header+names form used in tests does not).
    /// </summary>
    public IReadOnlyDictionary<string, IconData> Data { get; }

    Manifest(
        string prefix,
        string className,
        IReadOnlyList<string> iconNames,
        IReadOnlyDictionary<string, IconData> data)
    {
        Prefix = prefix;
        ClassName = className;
        IconNames = iconNames;
        Data = data;
    }

    /// <summary>
    /// Data-file format: <c>key=value</c> header lines (<c>prefix</c>, <c>class</c>), a blank line, then
    /// one icon per line. An icon line is either a bare <c>name</c> or the tab-separated form
    /// <c>name\twidth\theight\tbody</c> (body escaped via <see cref="Unescape"/>).
    /// </summary>
    public static Manifest Parse(string prefix, string content)
    {
        string? className = null;
        var names = new List<string>();
        var data = new Dictionary<string, IconData>(StringComparer.Ordinal);
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

                var equals = line.IndexOf('=');
                if (equals > 0)
                {
                    var key = line.Substring(0, equals).Trim();
                    var value = line.Substring(equals + 1).Trim();
                    switch (key)
                    {
                        case "prefix":
                            prefix = value;
                            break;
                        case "class":
                            className = value;
                            break;
                    }

                    continue;
                }

                // No headers present - treat the whole file as names.
                inHeader = false;
            }

            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var tab = line.IndexOf('\t');
            if (tab < 0)
            {
                names.Add(line);
                continue;
            }

            var parts = line.Split(separators, 4);
            var name = parts[0];
            names.Add(name);
            if (parts.Length == 4 &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
            {
                data[name] = new(Unescape(parts[3]), width, height);
            }
        }

        className ??= IdentifierNaming.ToPascalCase(prefix);
        return new(prefix, className, names, data);
    }

    static readonly char[] separators = ['\t'];

    /// <summary>Formats one <c>name\twidth\theight\tbody</c> data line (body escaped).</summary>
    public static string FormatDataLine(string name, double width, double height, string body) =>
        $"{name}\t{Num(width)}\t{Num(height)}\t{Escape(body)}";

    static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

    static string Escape(string value) =>
        value
            .Replace("\\", "\\\\")
            .Replace("\t", "\\t")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");

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
            if (c != '\\' || i + 1 >= value.Length)
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

    public bool Equals(Manifest? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Prefix != other.Prefix ||
            ClassName != other.ClassName ||
            IconNames.Count != other.IconNames.Count ||
            Data.Count != other.Data.Count)
        {
            return false;
        }

        for (var i = 0; i < IconNames.Count; i++)
        {
            if (IconNames[i] != other.IconNames[i])
            {
                return false;
            }
        }

        foreach (var pair in Data)
        {
            if (!other.Data.TryGetValue(pair.Key, out var value) ||
                !value.Equals(pair.Value))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as Manifest);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Prefix.GetHashCode();
            hash = (hash * 397) ^ ClassName.GetHashCode();
            hash = (hash * 397) ^ IconNames.Count;
            return hash;
        }
    }
}
