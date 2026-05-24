using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IntegrationTests;

// Heavy end-to-end: a trimmed, self-contained publish of TrimmedConsumer, then run it. Explicit so the
// normal test pass stays fast; run on demand / in a dedicated CI job:
//   IntegrationTests.exe --treenode-filter "/*/*/TrimmingTests/*"
public class TrimmingTests
{
    [Test]
    [Explicit]
    public async Task Materialised_icons_survive_trimming_and_dynamic_misses_throw()
    {
        var project = LocateProject("TrimmedConsumer", "TrimmedConsumer.csproj");
        var rid = RuntimeInformation.RuntimeIdentifier;
        var outDir = Path.Combine(Path.GetTempPath(), "iconifybundle-trim-" + Guid.NewGuid().ToString("N"));

        var publish = await Run(
            "dotnet",
            $"publish \"{project}\" -c Release -r {rid} --self-contained -p:PublishTrimmed=true -o \"{outDir}\"");
        await Assert.That(publish.ExitCode).IsEqualTo(0).Because(publish.Output);

        try
        {
            var exe = Path.Combine(outDir, OperatingSystem.IsWindows() ? "TrimmedConsumer.exe" : "TrimmedConsumer");
            await Assert.That(File.Exists(exe)).IsTrue();

            var run = await Run(exe, "");
            await Assert.That(run.ExitCode).IsEqualTo(0).Because(run.Output);
            // A statically-referenced icon still resolves after trimming (the module initializer survived).
            await Assert.That(run.Output).Contains("static-activity-len=");
            await Assert.That(run.Output).Contains("dynamic-activity-name=activity");
            // A dynamic lookup of an icon never referenced statically throws - trimming did not change that.
            await Assert.That(run.Output).Contains("dynamic-zap=THREW");
        }
        finally
        {
            try
            {
                Directory.Delete(outDir, recursive: true);
            }
            catch
            {
                // best effort cleanup
            }
        }
    }

    static string LocateProject(string folder, string file)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var project = Path.Combine(dir.FullName, folder, file);
            if (File.Exists(project))
            {
                return project;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate {folder}/{file} above the test output.");
    }

    static async Task<(int ExitCode, string Output)> Run(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
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
