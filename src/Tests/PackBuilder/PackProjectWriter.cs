/// <summary>
/// Materialises a buildable <c>Iconistic.&lt;Pack&gt;</c> project from a downloaded Iconify pack JSON.
/// </summary>
static class PackProjectWriter
{
    public sealed record PackProject(string Prefix, string PackageId, string CsprojPath, int IconCount);

    public static PackProject Write(string prefix, Stream json, string packsDir)
    {
        var pascal = IdentifierNaming.ToPascalCase(prefix);
        var packageId = $"Iconistic.{pascal}";
        var packDir = Path.Combine(packsDir, packageId);

        if (Directory.Exists(packDir))
        {
            Directory.Delete(packDir, recursive: true);
        }

        var iconsDir = Path.Combine(packDir, "icons");
        var buildDir = Path.Combine(packDir, "build");
        Directory.CreateDirectory(iconsDir);
        Directory.CreateDirectory(buildDir);

        using var document = JsonDocument.Parse(json); // UTF-8 stream parsed directly - no intermediate string
        var root = document.RootElement;
        var defaultWidth = root.TryGetProperty("width", out var w) ? w.GetDouble() : 16;
        var defaultHeight = root.TryGetProperty("height", out var h) ? h.GetDouble() : 16;
        var iconsElement = root.GetProperty("icons");

        var names = new List<string>();
        var packJsonPath = Path.Combine(packDir, "iconistic.pack.json");
        using (var stream = File.Create(packJsonPath))
        using (var writer = new Utf8JsonWriter(stream))
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
                File.WriteAllText(Path.Combine(iconsDir, name + ".svg"), icon.Svg);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        var marker = $"IconisticPacks.{pascal}Pack";
        File.WriteAllText(Path.Combine(packDir, $"{prefix}.manifest"), BuildManifest(prefix, pascal, marker, names));
        File.WriteAllText(Path.Combine(packDir, "Pack.cs"), BuildMarker(pascal));
        File.WriteAllText(Path.Combine(buildDir, $"{packageId}.props"), BuildProps(prefix));
        File.WriteAllText(Path.Combine(buildDir, $"{packageId}.targets"), BuildTargets(prefix));

        var csprojPath = Path.Combine(packDir, $"{packageId}.csproj");
        File.WriteAllText(csprojPath, BuildCsproj(packageId, prefix, root, names.Count));

        return new(prefix, packageId, csprojPath, names.Count);
    }

    static string BuildManifest(string prefix, string pascal, string marker, List<string> names)
    {
        var builder = new StringBuilder(
            $"""
             prefix={prefix}
             class={pascal}
             marker={marker}
             """);
        builder.Append("\n\n");
        foreach (var name in names)
        {
            builder.Append(name).Append('\n');
        }

        return builder.ToString();
    }

    static string BuildMarker(string pascal) =>
        $$"""
          namespace IconisticPacks;

          /// <summary>Marker type used by Iconistic to locate this pack assembly.</summary>
          public static class {{pascal}}Pack
          {
          }
          """;

    static string BuildProps(string prefix) =>
        $"""
         <?xml version="1.0" encoding="utf-8"?>
         <Project>

           <ItemGroup>
             <AdditionalFiles Include="$(MSBuildThisFileDirectory){prefix}.manifest" IconisticPack="{prefix}" />
           </ItemGroup>

           <ItemGroup>
             <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="IconisticPack" />
           </ItemGroup>

         </Project>

         """;

    static string BuildTargets(string prefix)
    {
        var safe = IdentifierNaming.ToPascalCase(prefix);
        return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <Project>

                  <!-- In Disk mode, copy the pack's SVG files next to the build output. -->
                  <Target Name="IconisticCopy{safe}Icons" BeforeTargets="Build" Condition="'$(IconisticMode)' == 'Disk'">
                    <ItemGroup>
                      <_Iconistic{safe}Svg Include="$(MSBuildThisFileDirectory)..\icons\*.svg" />
                    </ItemGroup>
                    <Copy SourceFiles="@(_Iconistic{safe}Svg)"
                          DestinationFiles="@(_Iconistic{safe}Svg->'$(OutDir)iconistic\{prefix}\%(Filename)%(Extension)')"
                          SkipUnchangedFiles="true" />
                  </Target>

                </Project>

                """;
    }

    static string BuildCsproj(string packageId, string prefix, JsonElement root, int total)
    {
        var name = packageId;
        if (root.TryGetProperty("info", out var info))
        {
            if (info.TryGetProperty("name", out var n))
            {
                name = n.GetString() ?? packageId;
            }
        }

        var description = $"{name} ({total} icons) for Iconistic.";

        return $"""
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <TargetFramework>netstandard2.0</TargetFramework>
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
                    <Authors>$(RepositoryUrlEx)/graphs/contributors</Authors>
                    <PackageLicenseExpression>MIT</PackageLicenseExpression>
                    <GenerateDocumentationFile>false</GenerateDocumentationFile>
                    <NoWarn>$(NoWarn);NU5128</NoWarn>
                  </PropertyGroup>

                  <ItemGroup>
                    <EmbeddedResource Include="iconistic.pack.json" LogicalName="iconistic.pack.json" />
                    <None Include="{prefix}.manifest" Pack="true" PackagePath="build" />
                    <None Include="build\{packageId}.props" Pack="true" PackagePath="build" />
                    <None Include="build\{packageId}.targets" Pack="true" PackagePath="build" />
                    <None Include="icons\*.svg" Pack="true" PackagePath="icons" />
                  </ItemGroup>

                </Project>

                """;
    }

    static string Escape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
