namespace Iconistic;

/// <summary>
/// Declares an Iconify icon pack to download and generate a strongly typed API for.
/// Apply at the assembly level, once per pack.
/// </summary>
/// <example>
/// <code>
/// [assembly: IconPack("mdi", "home", "account", "cog")]
/// [assembly: IconPack("lucide", "house", Storage = IconStorage.EmbeddedResource)]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class IconPackAttribute : Attribute
{
    /// <param name="prefix">The Iconify collection prefix, e.g. <c>mdi</c>, <c>lucide</c>, <c>material-symbols</c>.</param>
    /// <param name="icons">The icon names to include from the collection, e.g. <c>home</c>, <c>account-outline</c>.</param>
    public IconPackAttribute(string prefix, params string[] icons)
    {
        Prefix = prefix;
        Icons = icons;
    }

    /// <summary>The Iconify collection prefix.</summary>
    public string Prefix { get; }

    /// <summary>The icon names to include from the collection.</summary>
    public string[] Icons { get; }

    /// <summary>How the icon data is stored in the assembly. Defaults to <see cref="IconStorage.BakedIn"/>.</summary>
    public IconStorage Storage { get; set; } = IconStorage.BakedIn;
}
