using Microsoft.Build.Framework;

namespace IconifyBundle.Build;

/// <summary>
/// Disk mode: writes the standalone <c>.svg</c> file for each referenced icon, reconstructed from the
/// pack's single <c>.icondata</c> file. Invoked from the pack's build/publish targets.
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

    public override bool Execute()
    {
        if (!File.Exists(IconData))
        {
            Log.LogError($"IconifyBundle: icon data file not found: {IconData}");
            return false;
        }

        var missing = IconWriter.Write(IconData, UsedNames.Select(_ => _.ItemSpec), OutputDir);
        foreach (var name in missing)
        {
            Log.LogWarning($"IconifyBundle: icon '{name}' not found in '{IconData}'; skipped.");
        }

        return !Log.HasLoggedErrors;
    }
}
