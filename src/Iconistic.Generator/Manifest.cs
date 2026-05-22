namespace Iconistic.Generator;

/// <summary>
/// A parsed pack manifest. The manifest is shipped by an <c>Iconistic.&lt;Pack&gt;</c> NuGet as an
/// <c>AdditionalFiles</c> entry (tagged with <c>IconisticPack</c> metadata) so the generator can
/// discover which packs are referenced and the icons they contain.
/// </summary>
public sealed class Manifest : IEquatable<Manifest>
{
    public string Prefix { get; }
    public string ClassName { get; }
    public IReadOnlyList<string> IconNames { get; }

    Manifest(string prefix, string className, IReadOnlyList<string> iconNames)
    {
        Prefix = prefix;
        ClassName = className;
        IconNames = iconNames;
    }

    /// <summary>
    /// Manifest format: <c>key=value</c> header lines, a blank line, then one icon name per line.
    /// Supported headers: <c>prefix</c>, <c>class</c>.
    /// </summary>
    public static Manifest Parse(string prefix, string content)
    {
        string? className = null;
        var names = new List<string>();
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

            names.Add(line);
        }

        className ??= IdentifierNaming.ToPascalCase(prefix);
        return new(prefix, className, names);
    }

    public bool Equals(Manifest? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Prefix != other.Prefix ||
            ClassName != other.ClassName ||
            IconNames.Count != other.IconNames.Count)
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
