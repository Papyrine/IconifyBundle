using System.Xml.Linq;

static class RepoPaths
{
    public static string Root { get; } = Find();

    public static string Nugets { get; } = Path.Combine(Root, "nugets");

    public static string Packs { get; } = Path.Combine(Root, "packs");

    public static string Cache { get; } = Path.Combine(Root, ".cache");

    /// <summary>
    /// The version stamped on generated pack packages, read from the shared root <c>Version.props</c>
    /// so packs stay aligned with the core Iconistic packages and the downstream consumers.
    /// </summary>
    public static string Version { get; } = ReadVersion();

    static string ReadVersion()
    {
        var propsPath = Path.Combine(Root, "Version.props");
        var version = XDocument.Load(propsPath)
            .Descendants("IconisticVersion")
            .FirstOrDefault()
            ?.Value
            .Trim();
        if (string.IsNullOrEmpty(version))
        {
            throw new InvalidOperationException($"No <IconisticVersion> element found in {propsPath}.");
        }

        return version;
    }

    static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "global.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repo root (no global.json found above the test output).");
    }
}
