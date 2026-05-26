using System.Diagnostics;
using IconifyBundle;

namespace DiskModeConsumer;

// In Disk mode the generator also emits file-path members (e.g. Feather.ActivityPath) and the pack's
// build targets copy the SVG files next to the build output.
public class DiskModeTests
{
    [Test]
    public async Task File_path_member_points_to_copied_svg()
    {
        var path = Feather.ActivityPath;

        await Assert.That(File.Exists(path)).IsTrue();
        var svg = await File.ReadAllTextAsync(path);
        await Assert.That(svg.Contains("<svg")).IsTrue();
    }

    [Test]
    public async Task Icon_surface_still_available()
    {
        await Assert.That(Feather.Activity.Svg.Contains("<svg")).IsTrue();
        await Assert.That(Feather.AlertCircle.Name).IsEqualTo("alert-circle");
    }

    [Test]
    public async Task IconifyJson_stream_for_picked_icons()
    {
        // Every Icon carries its pack Prefix, so IconifyJson derives "feather" from the icons
        // themselves - no separate prefix argument needed.
        using var stream = IconifyJson.OpenReadStream(Feather.Box, Feather.Database);

        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        await Assert.That(json).Contains("\"prefix\":\"feather\"");
        await Assert.That(json).Contains("\"box\"");
        await Assert.That(json).Contains("\"database\"");
    }

    // dotnet publish must carry the used SVGs into the publish output (not just the build output).
    // Uses --no-build to publish the already-built outputs (avoids rebuilding the running assembly).
    [Test]
    public async Task Publish_includes_used_svgs()
    {
        var (project, configuration) = LocateProject();
        var publishDir = Path.Combine(Path.GetTempPath(), "iconifybundle-publish-" + Guid.NewGuid().ToString("N"));

        var (exitCode, output) = await RunDotnet(
            $"publish \"{project}\" -c {configuration} --no-build --no-restore -o \"{publishDir}\"");
        await Assert.That(exitCode).IsEqualTo(0).Because(output);

        try
        {
            var activity = Path.Combine(publishDir, "iconifybundle", "feather", "activity.svg");
            await Assert.That(File.Exists(activity)).IsTrue();
            await Assert.That((await File.ReadAllTextAsync(activity)).Contains("<svg")).IsTrue();
        }
        finally
        {
            try
            {
                Directory.Delete(publishDir, recursive: true);
            }
            catch
            {
                // best effort cleanup
            }
        }
    }

    // Walks up from the running assembly (bin/<Configuration>/<tfm>/) to the project file, and reads the
    // build configuration from the output path so the --no-build publish matches what was built.
    static (string Project, string Configuration) LocateProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? configuration = null;
        while (dir is not null)
        {
            if (dir.Parent?.Name == "bin")
            {
                configuration = dir.Name;
            }

            var project = Path.Combine(dir.FullName, "DiskModeConsumer.csproj");
            if (File.Exists(project))
            {
                return (project, configuration ?? "Debug");
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not locate DiskModeConsumer.csproj above the test output.");
    }

    static async Task<(int ExitCode, string Output)> RunDotnet(string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, stdout + stderr);
    }
}
