using System.Text.Json;

namespace Iconistic;

/// <summary>
/// A lazily-parsed collection of icons, used by the <see cref="IconStorage.EmbeddedResource"/>
/// and <see cref="IconStorage.DeployedFile"/> storage modes. Parsing happens on first access.
/// </summary>
public sealed class IconPack
{
    readonly object gate = new();
    Dictionary<string, IconisticIcon>? icons;
    string? json;

    /// <summary>Creates a pack from an embedded compact JSON string.</summary>
    public IconPack(string json) => this.json = json;

    /// <summary>Creates an empty pack to be populated later via <see cref="Load"/> (deployed mode).</summary>
    public IconPack()
    {
    }

    /// <summary>Whether the pack has its JSON and can resolve icons.</summary>
    public bool IsLoaded => json is not null;

    /// <summary>Sets or replaces the pack JSON (deployed mode).</summary>
    public void Load(string packJson)
    {
        lock (gate)
        {
            json = packJson;
            icons = null;
        }
    }

    Dictionary<string, IconisticIcon> Map
    {
        get
        {
            if (icons is not null)
            {
                return icons;
            }

            lock (gate)
            {
                if (icons is not null)
                {
                    return icons;
                }

                if (json is null)
                {
                    throw new InvalidOperationException(
                        "Icon pack is not loaded. For DeployedFile packs, call the generated InitializeAsync at startup.");
                }

                return icons = Parse(json);
            }
        }
    }

    /// <summary>Gets an icon by name, throwing if it is not present.</summary>
    public IconisticIcon this[string name] =>
        Map.TryGetValue(name, out var icon)
            ? icon
            : throw new KeyNotFoundException($"Icon '{name}' was not found in the pack.");

    /// <summary>Tries to get an icon by name.</summary>
    public bool TryGet(string name, out IconisticIcon icon) =>
        Map.TryGetValue(name, out icon);

    /// <summary>All icon names in the pack.</summary>
    public IReadOnlyCollection<string> Names => Map.Keys;

    static Dictionary<string, IconisticIcon> Parse(string packJson)
    {
        using var document = JsonDocument.Parse(packJson);
        var root = document.RootElement;
        var defaultWidth = GetInt(root, "w", 16);
        var defaultHeight = GetInt(root, "h", 16);

        var result = new Dictionary<string, IconisticIcon>(StringComparer.Ordinal);
        foreach (var property in root.GetProperty("icons").EnumerateObject())
        {
            var element = property.Value;
            var body = element.GetProperty("b").GetString() ?? "";
            result[property.Name] = new(
                body,
                GetInt(element, "w", defaultWidth),
                GetInt(element, "h", defaultHeight),
                GetInt(element, "l", 0),
                GetInt(element, "t", 0),
                GetInt(element, "r", 0),
                GetBool(element, "hf"),
                GetBool(element, "vf"));
        }

        return result;
    }

    static int GetInt(JsonElement element, string name, int fallback) =>
        element.TryGetProperty(name, out var value) ? value.GetInt32() : fallback;

    static bool GetBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.GetBoolean();
}
