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

        var iconsDir = Path.Combine(packDir, "icons");
        var buildDir = Path.Combine(packDir, "build");
        Directory.CreateDirectory(iconsDir);
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

            var icon = new Icon(name, body, iconWidth, iconHeight);
            await File.WriteAllTextAsync(Path.Combine(iconsDir, name + ".svg"), icon.Svg);
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
            licenseTitle = license.TryGetProperty("title", out var lt) ? lt.GetString() ?? "" : "";
            licenseUrl = license.TryGetProperty("url", out var lu) ? lu.GetString() : null;
            licenseSpdx = license.TryGetProperty("spdx", out var ls) ? ls.GetString() : null;
        }

        // The strongly-typed pack class is compiled into the pack assembly; it carries no icon data
        // (members resolve through the runtime registry the consumer's generator populates).
        await File.WriteAllTextAsync(Path.Combine(packDir, $"{pascal}.cs"), Emitter.EmitPackClass(Manifest.Parse(prefix, dataText)));
        await File.WriteAllTextAsync(Path.Combine(packDir, "readme.md"), BuildReadme(packageId, pascal, displayName, names.Count));
        await File.WriteAllTextAsync(Path.Combine(buildDir, $"{packageId}.props"), BuildProps(prefix));
        await File.WriteAllTextAsync(Path.Combine(buildDir, $"{packageId}.targets"), BuildTargets(prefix, pascal));

        var csprojPath = Path.Combine(packDir, $"{packageId}.csproj");
        await File.WriteAllTextAsync(csprojPath, BuildCsproj(packageId, prefix, displayName, names.Count, licenseSpdx));

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
    // After compilation, read that list and copy the matching SVGs from the package's icons/ folder to the
    // output directory (under iconifybundle/<prefix>/). Item/target names are pack-scoped so multiple
    // referenced packs don't collide.
    static string BuildTargets(string prefix, string pascal) =>
        $$"""
          <?xml version="1.0" encoding="utf-8"?>
          <Project>
            <!-- Imported after the project body, so $(IconifyBundleMode) has its final value. In Disk mode
                 write the generator's output (incl. the used-icon list) to disk for the copy target. -->
            <PropertyGroup Condition="'$(IconifyBundleMode)' == 'Disk'">
              <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
              <CompilerGeneratedFilesOutputPath Condition="'$(CompilerGeneratedFilesOutputPath)' == ''">$(IntermediateOutputPath)generated</CompilerGeneratedFilesOutputPath>
            </PropertyGroup>
            <Target Name="IconifyBundleCopyUsed_{{pascal}}"
                    AfterTargets="CoreCompile"
                    Condition="'$(IconifyBundleMode)' == 'Disk'">
              <ItemGroup>
                <_IconifyUsedFile_{{pascal}} Include="$(CompilerGeneratedFilesOutputPath)\**\{{pascal}}.Used.g.cs" />
              </ItemGroup>
              <ReadLinesFromFile File="@(_IconifyUsedFile_{{pascal}})" Condition="'@(_IconifyUsedFile_{{pascal}})' != ''">
                <Output TaskParameter="Lines" ItemName="_IconifyUsedLine_{{pascal}}" />
              </ReadLinesFromFile>
              <ItemGroup>
                <_IconifyUsedName_{{pascal}} Include="@(_IconifyUsedLine_{{pascal}})"
                  Condition="'%(Identity)' != '' and !$([System.String]::new('%(Identity)').StartsWith('/')) and !$([System.String]::new('%(Identity)').StartsWith('#'))" />
                <_IconifySvg_{{pascal}} Include="@(_IconifyUsedName_{{pascal}}->'$(MSBuildThisFileDirectory)../icons/%(Identity).svg')" />
              </ItemGroup>
              <Copy SourceFiles="@(_IconifySvg_{{pascal}})"
                    DestinationFiles="@(_IconifySvg_{{pascal}}->'$(OutDir)iconifybundle/{{prefix}}/%(Filename)%(Extension)')"
                    SkipUnchangedFiles="true"
                    Condition="'@(_IconifySvg_{{pascal}})' != ''" />
            </Target>
          </Project>

          """;

    static string BuildReadme(string packageId, string pascal, string displayName, int total) =>
        $"""
         # {packageId}

         {displayName} ({total} icons) for [IconifyBundle](https://github.com/SimonCropp/IconifyBundle) -
         strongly-typed [Iconify](https://iconify.design/) icons for .NET.

         ```csharp
         Icon icon = {pascal}.SomeIcon;
         string svg = icon.Svg;
         ```

         A single reference to this package gives the strongly-typed `{pascal}` class with a member per icon.

         """;

    static string BuildCsproj(string packageId, string prefix, string displayName, int total, string? licenseSpdx)
    {
        var description = $"{displayName} ({total} icons) for IconifyBundle.";
        // Stamp the pack's real license (SPDX) rather than assuming MIT. When Iconify supplies no SPDX,
        // omit the expression rather than declaring a license the pack may not actually carry.
        var licenseElement = string.IsNullOrEmpty(licenseSpdx)
            ? ""
            : $"\n    <PackageLicenseExpression>{licenseSpdx}</PackageLicenseExpression>";

        // Absolute path to the built generator dll; $(Configuration) resolves to the pack build's config.
        var generatorDll =
            $"{RepoPaths.Root.Replace('\\', '/')}/src/IconifyBundle.Generator/bin/$(Configuration)/netstandard2.0/IconifyBundle.Generator.dll";

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
                    <!-- CS0108: an icon named e.g. "equals"/"gethashcode" yields a member that hides an object member. -->
                    <NoWarn>$(NoWarn);NU5128;CS0108</NoWarn>
                  </PropertyGroup>
                  <ItemGroup>
                    <!-- The compiled pack class returns IconifyBundle.Icon and uses IconifyBundle.IconPack.
                         ExcludeAssets=analyzers: the pack doesn't need the generator running on itself. -->
                    <PackageReference Include="IconifyBundle" Version="{RepoPaths.Version}" ExcludeAssets="analyzers" />
                  </ItemGroup>
                  <ItemGroup>
                    <None Include="readme.md" Pack="true" PackagePath="\" />
                    <!-- Icon data (bodies + sizes) shipped for the generator to read at build, NOT embedded
                         in the assembly. -->
                    <None Include="{prefix}.icondata" Pack="true" PackagePath="build" />
                    <None Include="build/{packageId}.props" Pack="true" PackagePath="build" />
                    <None Include="build/{packageId}.targets" Pack="true" PackagePath="build" />
                    <None Include="icons/*.svg" Pack="true" PackagePath="icons" />
                    <!-- Ship the generator as an analyzer in each pack so a single pack reference makes it
                         run in the consumer (analyzers are not transitive through the runtime package). -->
                    <None Include="{generatorDll}" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
                  </ItemGroup>
                </Project>

                """;
    }

    static string Escape(string value) =>
        value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
}
