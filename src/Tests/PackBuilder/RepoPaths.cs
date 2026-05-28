using System.Xml.Linq;

static class RepoPaths
{
    static RepoPaths()
    {
        Root = Path.GetFullPath(Path.Combine(ProjectFiles.SolutionDirectory, "../"));
        Packs = Path.Combine(Root, "packs");
        Cache = Path.Combine(Root, ".cache");
        var propsPath = Path.Combine(Root, "Version.props");
        Version = ReadVersion(propsPath);
    }

    private static string ReadVersion(string propsPath)
    {
        var version = XDocument.Load(propsPath)
            .Descendants("IconifyBundleVersion")
            .FirstOrDefault()
            ?.Value
            .Trim();

        if (string.IsNullOrEmpty(version))
        {
            throw new InvalidOperationException($"No <IconifyBundleVersion> element found in {propsPath}.");
        }

        return version;
    }

    public static string Root { get; }

    public static string Packs { get; }

    public static string Cache { get; }

    /// <summary>
    /// The version stamped on generated pack packages, read from the shared root <c>Version.props</c>
    /// so packs stay aligned with the core IconifyBundle packages and the downstream consumers.
    /// </summary>
    public static string Version { get; }
}
