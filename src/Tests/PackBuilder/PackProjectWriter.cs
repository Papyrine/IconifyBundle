/// <summary>
/// Materialises a buildable <c>IconifyBundle.&lt;Pack&gt;</c> project from a downloaded Iconify pack JSON.
/// </summary>
static class PackProjectWriter
{
    public sealed record PackProject(
        string Prefix,
        string PackageId,
        string CsprojPath,
        int IconCount,
        string LicenseTitle,
        string? LicenseUrl);

    public static async Task<PackProject> Write(string prefix, Stream json, string packsDir)
    {
        var pascal = IdentifierNaming.ToPascalCase(prefix);
        var packageId = $"IconifyBundle.{pascal}";
        var packDir = Path.Combine(packsDir, packageId);

        if (Directory.Exists(packDir))
        {
            Directory.Delete(packDir, recursive: true);
        }

        var buildDir = Path.Combine(packDir, "build");
        Directory.CreateDirectory(buildDir);

        using var document = await JsonDocument.ParseAsync(json);
        var root = document.RootElement;
        var defaultWidth = root.TryGetProperty("width", out var w) ? w.GetDouble() : 16;
        var defaultHeight = root.TryGetProperty("height", out var h) ? h.GetDouble() : 16;
        var iconsElement = root.GetProperty("icons");

        // The pack data file: header + one "name\twidth\theight\tbody" line per icon. Shipped as an
        // AdditionalFile (not embedded in the assembly) for the generator to read at build time, so it can
        // materialise only the icons a consumer uses.
        var names = new List<string>();
        var data = new StringBuilder()
            .Append("prefix=").Append(prefix).Append('\n')
            .Append("class=").Append(pascal).Append("\n\n");

        foreach (var entry in iconsElement.EnumerateObject())
        {
            var name = entry.Name;
            var value = entry.Value;
            var body = value.GetProperty("body").GetString()!;
            var iconWidth = value.TryGetProperty("width", out var iw) ? iw.GetDouble() : defaultWidth;
            var iconHeight = value.TryGetProperty("height", out var ih) ? ih.GetDouble() : defaultHeight;

            names.Add(name);
            data.Append(Manifest.FormatDataLine(name, iconWidth, iconHeight, body)).Append('\n');
        }

        var dataText = data.ToString();
        await File.WriteAllTextAsync(Path.Combine(packDir, $"{prefix}.icondata"), dataText);

        var hasInfo = root.TryGetProperty("info", out var info);
        var displayName = hasInfo && info.TryGetProperty("name", out var infoName)
            ? infoName.GetString() ?? packageId
            : packageId;

        // Iconify ships per-pack license metadata (title + spdx + url) in info.license; surface it so
        // consumers can see each pack's terms (some packs are copyleft - see readme), and stamp the real
        // SPDX expression on the package instead of assuming MIT.
        var licenseTitle = "";
        string? licenseUrl = null;
        string? licenseSpdx = null;
        if (hasInfo && info.TryGetProperty("license", out var license))
        {
            licenseTitle = ShortLicenseTitle(license.TryGetProperty("title", out var lt) ? lt.GetString() ?? "" : "");
            licenseUrl = license.TryGetProperty("url", out var lu) ? lu.GetString() : null;
            licenseSpdx = license.TryGetProperty("spdx", out var ls) ? ls.GetString() : null;
        }

        // Iconify sometimes omits the license url even when it ships an spdx id (e.g. carbon/unjs/vaadin are
        // Apache-2.0 with no url). Fall back to the license's canonical url so the title still renders as a
        // link in the table/readme rather than bare text.
        if (string.IsNullOrEmpty(licenseUrl))
        {
            licenseUrl = CanonicalLicenseUrl(licenseSpdx);
        }

        // The strongly-typed pack class is compiled into the pack assembly; it carries no icon data
        // (members resolve through the runtime registry the consumer's generator populates).
        await File.WriteAllTextAsync(Path.Combine(packDir, $"{pascal}.cs"), Emitter.EmitPackClass(Manifest.Parse(prefix, dataText)));
        await File.WriteAllTextAsync(Path.Combine(packDir, "readme.md"), BuildReadme(packageId, pascal, displayName, names.Count, licenseTitle, licenseUrl));
        await File.WriteAllTextAsync(Path.Combine(buildDir, $"{packageId}.props"), BuildProps(prefix));
        await File.WriteAllTextAsync(Path.Combine(buildDir, $"{packageId}.targets"), BuildTargets(prefix, pascal));

        var csprojPath = Path.Combine(packDir, $"{packageId}.csproj");
        await File.WriteAllTextAsync(csprojPath, BuildCsproj(packageId, prefix, displayName, names.Count, licenseSpdx, licenseUrl));

        return new(prefix, packageId, csprojPath, names.Count, licenseTitle, licenseUrl);
    }

    static string BuildProps(string prefix) =>
        $"""
         <?xml version="1.0" encoding="utf-8"?>
         <Project>
           <ItemGroup>
             <AdditionalFiles Include="$(MSBuildThisFileDirectory){prefix}.icondata" IconifyBundlePack="{prefix}" />
           </ItemGroup>
           <ItemGroup>
             <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="IconifyBundlePack" />
           </ItemGroup>
         </Project>

         """;

    // Disk mode: the generator emits a (compile-inert) "<Pascal>.Used.g.cs" listing the referenced icons.
    // After compilation, reconstruct just those icons' .svg files from the pack's single .icondata (via the
    // shipped WriteUsedIcons task) into the output (under iconifybundle/<prefix>/), then mirror them into the
    // publish output. Item/target names are pack-scoped so multiple referenced packs don't collide.
    static string BuildTargets(string prefix, string pascal) =>
        $"""
          <?xml version="1.0" encoding="utf-8"?>
          <Project>
            <UsingTask TaskName="IconifyBundle.Build.WriteUsedIcons"
                       AssemblyFile="$(MSBuildThisFileDirectory)../tasks/IconifyBundle.Build.dll" />
            <!-- Imported after the project body, so $(IconifyBundleMode) has its final value. In Disk mode
                 write the generator's output (incl. the used-icon list) to disk for the target below. -->
            <PropertyGroup Condition="'$(IconifyBundleMode)' == 'Disk'">
              <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
              <CompilerGeneratedFilesOutputPath Condition="'$(CompilerGeneratedFilesOutputPath)' == ''">$(IntermediateOutputPath)generated</CompilerGeneratedFilesOutputPath>
            </PropertyGroup>
            <Target Name="IconifyBundleWriteUsed_{pascal}"
                    AfterTargets="CoreCompile"
                    Condition="'$(IconifyBundleMode)' == 'Disk'">
              <ItemGroup>
                <_IconifyUsedFile_{pascal} Include="$(CompilerGeneratedFilesOutputPath)\**\{pascal}.Used.g.cs" />
              </ItemGroup>
              <ReadLinesFromFile File="@(_IconifyUsedFile_{pascal})" Condition="'@(_IconifyUsedFile_{pascal})' != ''">
                <Output TaskParameter="Lines" ItemName="_IconifyUsedLine_{pascal}" />
              </ReadLinesFromFile>
              <ItemGroup>
                <_IconifyUsedName_{pascal} Include="@(_IconifyUsedLine_{pascal})"
                  Condition="'%(Identity)' != '' and !$([System.String]::new('%(Identity)').StartsWith('/')) and !$([System.String]::new('%(Identity)').StartsWith('#'))" />
              </ItemGroup>
              <WriteUsedIcons Condition="'@(_IconifyUsedName_{pascal})' != ''"
                              IconData="$(MSBuildThisFileDirectory){prefix}.icondata"
                              UsedNames="@(_IconifyUsedName_{pascal})"
                              OutputDir="$(OutDir)iconifybundle/{prefix}" />
            </Target>
            <!-- The above writes to the build output; dotnet publish needs the same files under PublishDir.
                 Build runs before publish, so the OutDir files already exist by here. -->
            <Target Name="IconifyBundlePublishUsed_{pascal}"
                    BeforeTargets="CopyFilesToPublishDirectory"
                    Condition="'$(IconifyBundleMode)' == 'Disk'">
              <ItemGroup>
                <_IconifyPublishSvg_{pascal} Include="$(OutDir)iconifybundle/{prefix}/*.svg" />
              </ItemGroup>
              <Copy SourceFiles="@(_IconifyPublishSvg_{pascal})"
                    DestinationFiles="@(_IconifyPublishSvg_{pascal}->'$(PublishDir)iconifybundle/{prefix}/%(Filename)%(Extension)')"
                    SkipUnchangedFiles="true"
                    Condition="'@(_IconifyPublishSvg_{pascal})' != ''" />
            </Target>
          </Project>

          """;

    static string BuildReadme(string packageId, string pascal, string displayName, int total, string licenseTitle, string? licenseUrl)
    {
        // Surface the pack's own license on its package page (some packs are share-alike / attribution -
        // see the repo readme), not just in the aggregate table. Omit the section when Iconify gives no license.
        var license = licenseTitle.Length == 0
            ? ""
            : licenseUrl is {Length: > 0} url
                ? $"\n## License\n\n[{licenseTitle}]({url})\n"
                : $"\n## License\n\n{licenseTitle}\n";

        return $"""
                # {packageId}

                {displayName} ({total} icons) for [IconifyBundle](https://github.com/SimonCropp/IconifyBundle) -
                strongly-typed [Iconify](https://iconify.design/) icons for .NET.

                ```csharp
                Icon icon = {pascal}.SomeIcon;
                string svg = icon.Svg;
                ```

                A single reference to this package gives the strongly-typed `{pascal}` class with a member per icon.
                {license}
                """;
    }

    static string BuildCsproj(string packageId, string prefix, string displayName, int total, string? licenseSpdx, string? licenseUrl)
    {
        var description = $"{displayName} ({total} icons) for IconifyBundle.";
        // Stamp the pack's real license rather than assuming MIT. NuGet's <PackageLicenseExpression> only
        // accepts OSI-approved / FSF-libre SPDX ids; pushing anything else is rejected (HTTP 400).
        // PackSelection already drops GPL and non-commercial packs, so the only redistributable-but-unapproved
        // licenses that reach here are the Creative Commons attribution family (CC-BY*, CC-BY-SA*) - CC0 is
        // FSF-libre and stays an expression. For those, fall back to the (deprecated but still publishable)
        // <PackageLicenseUrl> so the pack still declares its terms. When Iconify supplies neither, omit it
        // rather than declaring a license the pack may not actually carry.
        string licenseElement;
        if (!string.IsNullOrEmpty(licenseSpdx) && IsNuGetLicenseExpression(licenseSpdx))
        {
            licenseElement = $"\n    <PackageLicenseExpression>{licenseSpdx}</PackageLicenseExpression>";
        }
        else if (!string.IsNullOrEmpty(licenseUrl))
        {
            licenseElement = $"\n    <PackageLicenseUrl>{Escape(licenseUrl)}</PackageLicenseUrl>";
        }
        else
        {
            licenseElement = "";
        }

        // Absolute path to the built build-task dll; $(Configuration) resolves to the pack build's config.
        // (The generator is shipped by the IconifyBundle runtime package, not per pack.)
        var srcRoot = RepoPaths.Root.Replace('\\', '/') + "/src";
        var buildTaskDll = $"{srcRoot}/IconifyBundle.Build/bin/$(Configuration)/netstandard2.0/IconifyBundle.Build.dll";

        return $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <LangVersion>latest</LangVersion>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <IsPackable>true</IsPackable>
                    <IncludeBuildOutput>true</IncludeBuildOutput>
                    <PackageId>{packageId}</PackageId>
                    <AssemblyName>{packageId}</AssemblyName>
                    <Version>{RepoPaths.Version}</Version>
                    <Description>{Escape(description)}</Description>
                    <PackageProjectUrl>https://iconify.design/</PackageProjectUrl>
                    <PackageTags>iconify;icons;svg;{prefix}</PackageTags>
                    <Authors>$(RepositoryUrlEx)/graphs/contributors</Authors>{licenseElement}
                    <PackageReadmeFile>readme.md</PackageReadmeFile>
                    <GenerateDocumentationFile>false</GenerateDocumentationFile>
                    <!-- CS0108: an icon named e.g. "equals"/"gethashcode" yields a member that hides an object member.
                         NU5100: the build task ships in tasks/ (not lib/) on purpose - it is an MSBuild task, not a reference.
                         NU5125: CC-BY packs declare their license via the deprecated <PackageLicenseUrl> because NuGet
                                 rejects those licenses in <PackageLicenseExpression> (see BuildCsproj). -->
                    <NoWarn>$(NoWarn);NU5100;NU5125;NU5128;CS0108</NoWarn>
                  </PropertyGroup>
                  <ItemGroup>
                    <!-- The compiled pack class returns IconifyBundle.Icon and uses IconifyBundle.IconPack.
                         IconifyBundle also injects the source generator into the consumer (via its
                         build/buildTransitive props), so a single reference to this pack runs it - the pack
                         itself ships no generator. -->
                    <PackageReference Include="IconifyBundle" Version="{RepoPaths.Version}" />
                  </ItemGroup>
                  <ItemGroup>
                    <None Include="readme.md" Pack="true" PackagePath="\" />
                    <!-- Icon data (bodies + sizes), the single source of icons. Read by the generator at
                         build; in Disk mode the build task reconstructs the used .svg files from it. NOT
                         embedded in the assembly, and shipped once (no separate icons/*.svg). -->
                    <None Include="{prefix}.icondata" Pack="true" PackagePath="build" />
                    <None Include="build/{packageId}.props" Pack="true" PackagePath="build" />
                    <None Include="build/{packageId}.targets" Pack="true" PackagePath="build" />
                    <!-- Ship the Disk-mode build task (reconstructs used .svg files from the .icondata). -->
                    <None Include="{buildTaskDll}" Pack="true" PackagePath="tasks" Visible="false" />
                  </ItemGroup>
                </Project>

                """;
    }

    // True when NuGet accepts the SPDX id as a <PackageLicenseExpression>. The Creative Commons attribution
    // licenses (CC-BY*, CC-BY-SA*) are neither OSI-approved nor FSF-libre, so NuGet rejects them; CC0 (a
    // public-domain dedication) is FSF-libre and does not start with "CC-BY", so it stays an expression.
    static bool IsNuGetLicenseExpression(string spdx) =>
        !spdx.StartsWith("CC-BY", StringComparison.OrdinalIgnoreCase);

    // Shorten Iconify's verbose license titles to their common abbreviations for the table/readme.
    static string ShortLicenseTitle(string title) =>
        title switch
        {
            "Mozilla Public License 2.0" => "MPL 2.0",
            "Open Font License" => "OFL",
            _ => title
        };

    // Canonical url for a license spdx id, used when Iconify ships the id but no url so the title still renders
    // as a link rather than bare text. Each maps to the license's authoritative reference text.
    static string? CanonicalLicenseUrl(string? spdx) =>
        spdx is null
            ? null
            : spdx.ToUpperInvariant() switch
            {
                "APACHE-2.0" => "https://www.apache.org/licenses/LICENSE-2.0",
                "MPL-2.0" => "https://www.mozilla.org/en-US/MPL/2.0/",
                "MIT" => "https://opensource.org/license/mit",
                "OFL-1.1" => "https://openfontlicense.org/",
                _ => null
            };

    static string Escape(string value) =>
        value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
}
