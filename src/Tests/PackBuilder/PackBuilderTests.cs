public class PackBuilderTests
{
    // Explicit: builds every Iconify collection (slow, network-heavy). Run on demand / in CI via
    //   Tests.exe --treenode-filter "/*/*/PackBuilderTests/*"
    // rather than as part of the normal unit-test pass.
    [Test]
    [NotInParallel]
    [Explicit]
    public async Task BuildPacks()
    {
        Directory.CreateDirectory(RepoPaths.Nugets);
        Directory.CreateDirectory(RepoPaths.Cache);
        Directory.CreateDirectory(RepoPaths.Packs);
        WritePacksScaffolding();

        await using var cache = new HttpCache(RepoPaths.Cache);
        var prefixes = await PackSelection.ResolveAsync(cache);
        await Assert.That(prefixes.Count).IsGreaterThan(0);

        foreach (var prefix in prefixes)
        {
            Console.WriteLine(prefix);
            await using var json = await cache.StreamAsync(
                $"https://raw.githubusercontent.com/iconify/icon-sets/master/json/{prefix}.json");
            var project = PackProjectWriter.Write(prefix, json, RepoPaths.Packs);

            var result = await Dotnet.RunAsync(
                $"pack \"{project.CsprojPath}\" -c Release -o \"{RepoPaths.Nugets}\" --nologo");
            if (result.ExitCode != 0)
            {
                Console.WriteLine(result.Output);
            }

            await Assert.That(result.ExitCode).IsEqualTo(0);

            var nupkg = Path.Combine(RepoPaths.Nugets, $"{project.PackageId}.{RepoPaths.Version}.nupkg");
            await Assert.That(File.Exists(nupkg)).IsTrue();
            await AssertPackageContents(nupkg, prefix, project);
        }
    }

    static async Task AssertPackageContents(string nupkg, string prefix, PackProjectWriter.PackProject project)
    {
        await using var archive = await ZipFile.OpenReadAsync(nupkg);
        var entries = archive.Entries
            .Select(_ => _.FullName.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await Assert.That(entries).Contains($"build/{prefix}.manifest");
        await Assert.That(entries).Contains($"build/{project.PackageId}.props");
        await Assert.That(entries).Contains($"build/{project.PackageId}.targets");
        await Assert.That(entries).Contains($"lib/netstandard2.0/{project.PackageId}.dll");
        await Assert.That(entries.Any(_ => _.StartsWith("icons/") && _.EndsWith(".svg"))).IsTrue();
    }

    static void WritePacksScaffolding()
    {
        // Halt Directory.Build.props / Directory.Packages.props traversal so generated pack projects
        // are self-contained and do not inherit the strict src/ build settings.
        File.WriteAllText(
            Path.Combine(RepoPaths.Packs, "Directory.Build.props"),
            "<Project />\n");
        File.WriteAllText(
            Path.Combine(RepoPaths.Packs, "Directory.Packages.props"),
            """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
              </PropertyGroup>
            </Project>

            """);
        File.WriteAllText(
            Path.Combine(RepoPaths.Packs, "nuget.config"),
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
              </packageSources>
            </configuration>

            """);
    }
}
