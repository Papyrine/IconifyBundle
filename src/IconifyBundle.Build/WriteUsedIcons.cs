namespace IconifyBundle.Build;

/// <summary>
/// Disk mode: writes the standalone <c>.svg</c> file for each referenced icon, reconstructed from the
/// pack's single <c>.icondata</c> file, and (when <c>IconifyBundleRasterSizes</c> is set) its PNG/BMP
/// renderings. Invoked from the pack's build/publish targets.
/// </summary>
public class WriteUsedIcons :
    Microsoft.Build.Utilities.Task
{
    [Required]
    public string IconData { get; set; } = "";

    [Required]
    public ITaskItem[] UsedNames { get; set; } = [];

    [Required]
    public string OutputDir { get; set; } = "";

    /// <summary>The raw <c>IconifyBundleRasterFormat</c> value: png, bmp, or both.</summary>
    public string RasterFormats { get; set; } = "";

    /// <summary>The raw <c>IconifyBundleRasterSizes</c> value; no rasters are written when empty.</summary>
    public string RasterSizes { get; set; } = "";

    /// <summary>The raw <c>IconifyBundleRasterColor</c> value, used for <c>currentColor</c>.</summary>
    public string RasterColor { get; set; } = "";

    public override bool Execute()
    {
        if (!File.Exists(IconData))
        {
            Log.LogError($"IconifyBundle: icon data file not found: {IconData}");
            return false;
        }

        // The generator reports invalid raster properties (IB0001) and fails the compile first, so this
        // only guards a direct invocation.
        var options = RasterOptions.Parse(RasterFormats, RasterSizes, RasterColor);
        if (options.Error != null)
        {
            Log.LogError(null, "IB0001", null, null, 0, 0, 0, 0, $"IconifyBundle: {options.Error}");
            return false;
        }

        var data = IconWriter.ParseIconData(File.ReadAllText(IconData));
        var used = UsedNames.Select(_ => _.ItemSpec).ToList();
        var missing = IconWriter.Write(data, used, OutputDir);
        foreach (var name in missing)
        {
            Log.LogWarning($"IconifyBundle: icon '{name}' not found in '{IconData}'; skipped.");
        }

        foreach (var failure in IconWriter.WriteRasters(data, used, OutputDir, options))
        {
            Log.LogWarning(null, "IB0003", null, null, 0, 0, 0, 0, $"IconifyBundle: {failure}");
        }

        return !Log.HasLoggedErrors;
    }
}
