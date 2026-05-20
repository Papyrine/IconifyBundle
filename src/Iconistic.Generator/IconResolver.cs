namespace Iconistic.Generator;

/// <summary>
/// Turns a parsed Iconify API response into fully resolved icons, following alias chains and
/// combining their transforms.
/// </summary>
static class IconResolver
{
    public static (List<KeyValuePair<string, NormIcon>> Found, List<string> NotFound, int DefaultWidth, int DefaultHeight) Resolve(
        Dictionary<string, object?> root,
        IEnumerable<string> requested)
    {
        var defaultWidth = Int(root, "width", 16);
        var defaultHeight = Int(root, "height", 16);
        var icons = Obj(root, "icons") ?? new();
        var aliases = Obj(root, "aliases") ?? new();

        var found = new List<KeyValuePair<string, NormIcon>>();
        var notFound = new List<string>();

        foreach (var name in requested)
        {
            var resolved = ResolveName(name, 0);
            if (resolved is { } icon)
            {
                found.Add(new(name, icon));
            }
            else
            {
                notFound.Add(name);
            }
        }

        return (found, notFound, defaultWidth, defaultHeight);

        NormIcon? ResolveName(string name, int depth)
        {
            if (depth > 50)
            {
                return null;
            }

            if (icons.TryGetValue(name, out var iconValue) &&
                iconValue is Dictionary<string, object?> iconObject)
            {
                return new(
                    Str(iconObject, "body") ?? "",
                    Int(iconObject, "width", defaultWidth),
                    Int(iconObject, "height", defaultHeight),
                    Int(iconObject, "left", 0),
                    Int(iconObject, "top", 0),
                    Int(iconObject, "rotate", 0),
                    Bool(iconObject, "hFlip"),
                    Bool(iconObject, "vFlip"));
            }

            if (aliases.TryGetValue(name, out var aliasValue) &&
                aliasValue is Dictionary<string, object?> aliasObject)
            {
                var parentName = Str(aliasObject, "parent");
                if (parentName is null)
                {
                    return null;
                }

                if (ResolveName(parentName, depth + 1) is not { } parent)
                {
                    return null;
                }

                return new(
                    parent.Body,
                    Int(aliasObject, "width", parent.Width),
                    Int(aliasObject, "height", parent.Height),
                    parent.Left,
                    parent.Top,
                    (parent.Rotate + Int(aliasObject, "rotate", 0)) & 3,
                    parent.HFlip ^ Bool(aliasObject, "hFlip"),
                    parent.VFlip ^ Bool(aliasObject, "vFlip"));
            }

            return null;
        }
    }

    static Dictionary<string, object?>? Obj(Dictionary<string, object?> parent, string key) =>
        parent.TryGetValue(key, out var value) ? value as Dictionary<string, object?> : null;

    static string? Str(Dictionary<string, object?> parent, string key) =>
        parent.TryGetValue(key, out var value) ? value as string : null;

    static int Int(Dictionary<string, object?> parent, string key, int fallback) =>
        parent.TryGetValue(key, out var value) && value is double number ? (int) number : fallback;

    static bool Bool(Dictionary<string, object?> parent, string key) =>
        parent.TryGetValue(key, out var value) && value is bool flag && flag;
}
