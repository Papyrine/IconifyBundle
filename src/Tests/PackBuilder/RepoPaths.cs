static class RepoPaths
{
    public static string Root { get; } = Find();

    public static string Nugets { get; } = Path.Combine(Root, "nugets");

    public static string Packs { get; } = Path.Combine(Root, "packs");

    public static string Cache { get; } = Path.Combine(Root, ".cache");

    /// <summary>The Iconistic package version that generated pack packages depend on.</summary>
    public const string Version = "1.0.0";

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
