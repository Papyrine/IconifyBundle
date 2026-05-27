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
        PurgeDirectory(RepoPaths.Packs, "IconifyBundle.*");
        PurgeDirectory(RepoPaths.Packs, "*.nupkg");
        Directory.CreateDirectory(RepoPaths.Cache);
        WritePacksScaffolding();

        await using var cache = new HttpCache(RepoPaths.Cache);
        var selection = await PackSelection.ResolveAsync(cache);
        var prefixes = selection.Prefixes;
        await Assert.That(prefixes.Count).IsGreaterThan(0);
        Log.Line($"Generating {prefixes.Count} pack projects ({selection.Excluded.Count} excluded by license)...");

        // Generate every pack project on disk first (downloads + writing tens of thousands of SVGs, so
        // run in parallel; each pack writes to its own directory).
        var generated = new ConcurrentBag<PackProjectWriter.PackProject>();
        var done = 0;
        await Parallel.ForEachAsync(
            prefixes,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            },
            async (prefix, token) =>
            {
                await using var json = await cache.StreamAsync(
                    $"https://raw.githubusercontent.com/iconify/icon-sets/master/json/{prefix}.json", cancel: token);
                var project = await PackProjectWriter.Write(prefix, json, RepoPaths.Packs);
                generated.Add(project);
                Log.Line($"[{Interlocked.Increment(ref done)}/{prefixes.Count}] generated {prefix} ({project.IconCount} icons)");
            });

        var projects = generated.ToList();

        // ...then pack them all in a single solution build (one restore, projects packed in parallel).
        var solutionPath = Path.Combine(RepoPaths.Packs, "Packs.slnx");
        await File.WriteAllTextAsync(solutionPath, BuildSolution(projects));

        Log.Line($"Packing solution ({projects.Count} projects)...");
        // echo: stream `dotnet pack` output live so the CI log shows steady progress through the long build.
        var result = await Dotnet.RunAsync(
            $"pack \"{solutionPath}\" -c Release -o \"{RepoPaths.Packs}\" --nologo -maxcpucount:7", echo: true);
        await Assert.That(result.ExitCode).IsEqualTo(0);

        // A successful `dotnet pack` already produced every nupkg; smoke-test the structure of just one
        // (opening all 200+ archives to assert contents is slow and redundant).
        var sample = projects[0];
        var nupkg = Path.Combine(RepoPaths.Packs, $"{sample.PackageId}.{RepoPaths.Version}.nupkg");
        Log.Line($"Verifying package contents of {sample.PackageId}...");
        await Assert.That(File.Exists(nupkg)).IsTrue();
        await AssertPackageContents(nupkg, sample);

        await WritePacksInclude(projects, selection.Excluded);

        Log.Line($"Done: {projects.Count} packs.");
    }

    static void PurgeDirectory(string path, string match)
    {
        Directory.CreateDirectory(path);
        foreach (var item in Directory.EnumerateDirectories(path, match))
        {
            Directory.Delete(item, true);
        }

        foreach (var item in Directory.EnumerateFiles(path, match))
        {
            File.Delete(item);
        }
    }

    // Explicit: builds a single pack (feather) for fast end-to-end validation of the consumer flow.
    //   Tests.exe --treenode-filter "/*/*/PackBuilderTests/BuildFeatherPack"
    [Test]
    [NotInParallel]
    [Explicit]
    public async Task BuildFeatherPack()
    {
        Directory.CreateDirectory(RepoPaths.Cache);
        Directory.CreateDirectory(RepoPaths.Packs);
        WritePacksScaffolding();

        await using var cache = new HttpCache(RepoPaths.Cache);
        await using var json = await cache.StreamAsync(
            "https://raw.githubusercontent.com/iconify/icon-sets/master/json/feather.json");
        var project = await PackProjectWriter.Write("feather", json, RepoPaths.Packs);

        var result = await Dotnet.RunAsync(
            $"pack \"{project.CsprojPath}\" -c Release -o \"{RepoPaths.Packs}\" --nologo -maxcpucount:7", echo: true);
        await Assert.That(result.ExitCode).IsEqualTo(0);

        var nupkg = Path.Combine(RepoPaths.Packs, $"{project.PackageId}.{RepoPaths.Version}.nupkg");
        await AssertPackageContents(nupkg, project);
        Log.Line($"Built {project.PackageId}.");
    }

    // Emits a markdown table (one row per produced pack) for inclusion in the readme via MarkdownSnippets,
    // prefixed with a note naming the packs excluded from publishing (grouped by why).
    static async Task WritePacksInclude(
        List<PackProjectWriter.PackProject> projects,
        List<PackSelection.ExcludedPack> excluded)
    {
        var builder = new StringBuilder();
        if (excluded.Count > 0)
        {
            builder.Append("> **Note:** some Iconify packs are not published because their license is incompatible with redistribution in a public, commercially-consumable NuGet:\n");
            AppendExcluded(builder, excluded, PackSelection.ExclusionReason.NonCommercial, "Non-commercial (CC BY-NC*)");
            AppendExcluded(builder, excluded, PackSelection.ExclusionReason.Copyleft, "Copyleft (GPL)");
            builder.Append('\n');
        }

        builder.Append("| Package | Iconify | License | NuGet size | Assembly size |\n");
        builder.Append("|---|---|---|--:|--:|\n");

        foreach (var project in projects.OrderBy(_ => _.PackageId, StringComparer.OrdinalIgnoreCase))
        {
            var nupkg = Path.Combine(RepoPaths.Packs, $"{project.PackageId}.{RepoPaths.Version}.nupkg");
            var nupkgSize = new FileInfo(nupkg).Length;
            var assemblySize = await AssemblySize(nupkg, project.PackageId);

            builder
                .Append("| [").Append(project.PackageId)
                .Append("](https://www.nuget.org/packages/").Append(project.PackageId)
                .Append(") | [").Append(project.Prefix)
                .Append("](https://icon-sets.iconify.design/").Append(project.Prefix)
                .Append("/) | ").Append(License(project))
                .Append(" | ").Append(FormatSize(nupkgSize))
                .Append(" | ").Append(FormatSize(assemblySize))
                .Append(" |\n");
        }

        var path = Path.Combine(RepoPaths.Root, "src", "packs.include.md");
        await File.WriteAllTextAsync(path, builder.ToString());
        Log.Line($"Wrote {path}");
    }

    // Uncompressed size of the shipped assembly (lib/net8.0/<PackageId>.dll) inside the nupkg.
    static async Task<long> AssemblySize(string nupkg, string packageId)
    {
        await using var archive = await ZipFile.OpenReadAsync(nupkg);
        var entry = archive.Entries.FirstOrDefault(
            _ => _.FullName.Replace('\\', '/').Equals($"lib/net8.0/{packageId}.dll", StringComparison.OrdinalIgnoreCase));
        return entry?.Length ?? 0;
    }

    static void AppendExcluded(
        StringBuilder builder,
        List<PackSelection.ExcludedPack> excluded,
        PackSelection.ExclusionReason reason,
        string label)
    {
        var packs = excluded.Where(_ => _.Reason == reason).ToList();
        if (packs.Count == 0)
        {
            return;
        }

        var links = string.Join(
            ", ",
            packs.Select(_ => $"[{_.Prefix}](https://icon-sets.iconify.design/{_.Prefix}/)"));
        builder.Append("> - **").Append(label).Append("**: ").Append(links).Append('\n');
    }

    static string License(PackProjectWriter.PackProject project)
    {
        if (project.LicenseTitle.Length == 0)
        {
            return "";
        }

        if (project.LicenseUrl is {Length: > 0} url)
        {
            return $"[{project.LicenseTitle}]({url})";
        }

        return project.LicenseTitle;
    }

    static string[] units = ["B", "KB", "MB", "GB"];

    static string FormatSize(long bytes)
    {
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#}{units[unit]}";
    }

    static string BuildSolution(IEnumerable<PackProjectWriter.PackProject> projects)
    {
        var builder = new StringBuilder("<Solution>\n");
        // Largest packs first (by icon count, a proxy for compile cost): MSBuild starts projects roughly
        // in listed order, so the few heavy packs (FluentEmoji, Noto, MaterialSymbols, ...) begin early and
        // compile alongside the many small ones, rather than serialising at the tail of the parallel build.
        foreach (var project in projects.OrderByDescending(_ => _.IconCount))
        {
            var relative = Path.GetRelativePath(RepoPaths.Packs, project.CsprojPath).Replace('\\', '/');
            builder.Append("  <Project Path=\"").Append(relative).Append("\" />\n");
        }

        builder.Append("</Solution>\n");
        return builder.ToString();
    }

    static async Task AssertPackageContents(string nupkg, PackProjectWriter.PackProject project)
    {
        await using var archive = await ZipFile.OpenReadAsync(nupkg);
        var entries = archive.Entries
            .Select(_ => _.FullName.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await Assert.That(entries).Contains($"build/{project.Prefix}.icondata");
        await Assert.That(entries).Contains($"build/{project.PackageId}.props");
        await Assert.That(entries).Contains($"build/{project.PackageId}.targets");
        await Assert.That(entries).Contains($"lib/net8.0/{project.PackageId}.dll");
        await Assert.That(entries).Contains("tasks/IconifyBundle.Build.dll");
        // The generator is shipped once by the IconifyBundle runtime package, not per pack.
        await Assert.That(entries.Any(_ => _.Contains("IconifyBundle.Generator.dll"))).IsFalse();
        // Icon data ships outside the assembly; it must NOT be embedded as a resource any more.
        await Assert.That(entries.Any(_ => _.EndsWith("iconifybundle.pack.json"))).IsFalse();
        // The icons are reconstructed at build from the single .icondata; no per-icon .svg is shipped.
        await Assert.That(entries.Any(_ => _.StartsWith("icons/"))).IsFalse();
    }

    static void WritePacksScaffolding()
    {
        // Halt Directory.Build.props / Directory.Packages.props traversal so generated pack projects
        // are self-contained and do not inherit the strict src/ build settings - but also embed each
        // pack's *.icondata into its compiled assembly as a uniform manifest resource so the runtime
        // (IconifyJson.OpenPackStream / ReadPack) can serve the full upstream pack data.
        File.WriteAllText(
            Path.Combine(RepoPaths.Packs, "Directory.Build.props"),
            """
            <Project>
              <ItemGroup>
                <EmbeddedResource Include="*.icondata">
                  <LogicalName>IconifyBundle.icondata</LogicalName>
                </EmbeddedResource>
              </ItemGroup>
            </Project>

            """);
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
                <!-- The locally-built IconifyBundle runtime that each pack references. -->
                <add key="local" value="../nugets" />
              </packageSources>
            </configuration>

            """);
    }
}
