using System.Text.Json;

namespace Iconistic.Api;

/// <summary>
/// Parses an Iconify icon-data response into <see cref="IconisticIcon"/> values, following alias
/// chains and combining their transforms.
/// </summary>
static class IconDataParser
{
    public static IconDataResult Parse(string json, IReadOnlyList<string> requested)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var defaultWidth = GetInt(root, "width", 16);
        var defaultHeight = GetInt(root, "height", 16);

        root.TryGetProperty("icons", out var icons);
        root.TryGetProperty("aliases", out var aliases);

        var resolved = new Dictionary<string, IconisticIcon>(StringComparer.Ordinal);
        var notFound = new List<string>();

        foreach (var name in requested)
        {
            if (Resolve(name, 0) is { } icon)
            {
                resolved[name] = icon;
            }
            else
            {
                notFound.Add(name);
            }
        }

        return new()
        {
            Icons = resolved,
            NotFound = notFound
        };

        IconisticIcon? Resolve(string name, int depth)
        {
            if (depth > 50)
            {
                return null;
            }

            if (icons.ValueKind == JsonValueKind.Object &&
                icons.TryGetProperty(name, out var iconElement))
            {
                return new(
                    GetString(iconElement, "body") ?? "",
                    GetInt(iconElement, "width", defaultWidth),
                    GetInt(iconElement, "height", defaultHeight),
                    GetInt(iconElement, "left", 0),
                    GetInt(iconElement, "top", 0),
                    GetInt(iconElement, "rotate", 0),
                    GetBool(iconElement, "hFlip"),
                    GetBool(iconElement, "vFlip"));
            }

            if (aliases.ValueKind == JsonValueKind.Object &&
                aliases.TryGetProperty(name, out var aliasElement))
            {
                var parentName = GetString(aliasElement, "parent");
                if (parentName is null || Resolve(parentName, depth + 1) is not { } parent)
                {
                    return null;
                }

                return new(
                    parent.Body,
                    GetInt(aliasElement, "width", parent.Width),
                    GetInt(aliasElement, "height", parent.Height),
                    parent.Left,
                    parent.Top,
                    (parent.Rotate + GetInt(aliasElement, "rotate", 0)) & 3,
                    parent.HFlip ^ GetBool(aliasElement, "hFlip"),
                    parent.VFlip ^ GetBool(aliasElement, "vFlip"));
            }

            return null;
        }
    }

    static int GetInt(JsonElement element, string name, int fallback) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : fallback;

    static bool GetBool(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
