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

        var names = new List<string>();
        var packJsonPath = Path.Combine(packDir, "iconifybundle.pack.json");
        await using (var stream = File.Create(packJsonPath))
        await using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("prefix", prefix);
            writer.WriteNumber("width", defaultWidth);
            writer.WriteNumber("height", defaultHeight);
            writer.WriteStartObject("icons");

            foreach (var entry in iconsElement.EnumerateObject())
            {
                var name = entry.Name;
                var value = entry.Value;
                var body = value.GetProperty("body").GetString()!;
                var iconWidth = value.TryGetProperty("width", out var iw) ? iw.GetDouble() : defaultWidth;
                var iconHeight = value.TryGetProperty("height", out var ih) ? ih.GetDouble() : defaultHeight;

                names.Add(name);

                writer.WriteStartObject(name);
                writer.WriteString("body", body);
                if (iconWidth != defaultWidth)
                {
                    writer.WriteNumber("width", iconWidth);
                }

                if (iconHeight != defaultHeight)
                {
                    writer.WriteNumber("height", iconHeight);
                }

                writer.WriteEndObject();

                var icon = new Icon(name, body, iconWidth, iconHeight);
                await File.WriteAllTextAsync(Path.Combine(iconsDir, name + ".svg"), icon.Svg);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

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

        var manifestText = BuildManifest(prefix, pascal, names);
        await File.WriteAllTextAsync(Path.Combine(packDir, $"{prefix}.manifest"), manifestText);
        // The strongly-typed pack class is compiled into the pack assembly (was previously emitted by the
        // source generator in each consumer); the manifest is still shipped for the path-extension generator.
        await File.WriteAllTextAsync(Path.Combine(packDir, $"{pascal}.cs"), Emitter.EmitPackClass(Manifest.Parse(prefix, manifestText)));
        await File.WriteAllTextAsync(Path.Combine(packDir, "readme.md"), BuildReadme(packageId, pascal, displayName, names.Count));
        await File.WriteAllTextAsync(Path.Combine(buildDir, $"{packageId}.props"), BuildProps(prefix));
        await File.WriteAllTextAsync(Path.Combine(buildDir, $"{packageId}.targets"), BuildTargets(prefix));

        var csprojPath = Path.Combine(packDir, $"{packageId}.csproj");
        await File.WriteAllTextAsync(csprojPath, BuildCsproj(packageId, prefix, displayName, names.Count, licenseSpdx));

        return new(prefix, packageId, csprojPath, names.Count, licenseTitle, licenseUrl);
    }

    static string BuildManifest(string prefix, string pascal, List<string> names)
    {
        var builder = new StringBuilder(
            $"""
             prefix={prefix}
             class={pascal}
             """);
        builder.Append("\n\n");
        foreach (var name in names)
        {
            builder.Append(name).Append('\n');
        }

        return builder.ToString();
    }

    static string BuildProps(string prefix) =>
        $"""
         <?xml version="1.0" encoding="utf-8"?>
         <Project>
           <ItemGroup>
             <AdditionalFiles Include="$(MSBuildThisFileDirectory){prefix}.manifest" IconifyBundlePack="{prefix}" />
           </ItemGroup>
           <ItemGroup>
             <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="IconifyBundlePack" />
           </ItemGroup>
         </Project>

         """;

    static string BuildTargets(string prefix) =>
        $"""
         <?xml version="1.0" encoding="utf-8"?>
         <Project>
           <!-- When the consumer sets IconifyBundleExtractDisk, declare the pack's SVG files as
                copy-to-output build assets and let MSBuild place them under the output directory. -->
           <ItemGroup Condition="'$(IconifyBundleExtractDisk)' == 'true'">
             <None Include="$(MSBuildThisFileDirectory)../icons/*.svg"
                   Link="iconifybundle/{prefix}/%(Filename)%(Extension)"
                   CopyToOutputDirectory="PreserveNewest"
                   Visible="false"
                   Pack="false" />
           </ItemGroup>
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
                    <EmbeddedResource Include="iconifybundle.pack.json" LogicalName="iconifybundle.pack.json" />
                    <None Include="readme.md" Pack="true" PackagePath="\" />
                    <None Include="{prefix}.manifest" Pack="true" PackagePath="build" />
                    <None Include="build/{packageId}.props" Pack="true" PackagePath="build" />
                    <None Include="build/{packageId}.targets" Pack="true" PackagePath="build" />
                    <None Include="icons/*.svg" Pack="true" PackagePath="icons" />
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
