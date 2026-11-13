namespace IconifyBundle.Generator;

/// <summary>
/// Runs in the consuming compilation. It reads the pack data files shipped by the referenced
/// <c>IconifyBundle.&lt;Pack&gt;</c> packages, detects which icons the consumer actually uses (static
/// member accesses on <c>[IconifyPack]</c> types - in C# syntax, and textually in <c>.razor</c>/<c>.cshtml</c>
/// files whose generated code this generator cannot see), and materialises only those:
/// <list type="bullet">
/// <item>Resource mode (default): a <c>[ModuleInitializer]</c> registering the used icons' bodies inline.</item>
/// <item>Disk mode: a <c>[ModuleInitializer]</c> registering on-disk paths, the per-icon <c>...Path</c>
/// members, and a (compile-inert) used-icon list that the pack's build target reads to copy the matching
/// SVGs to output.</item>
/// </list>
/// </summary>
[Generator(LanguageNames.CSharp)]
public class IconifyBundleGenerator :
    IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var disk = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
            provider.GlobalOptions.TryGetValue("build_property.IconifyBundleMode", out var mode) &&
            string.Equals(mode, "Disk", StringComparison.OrdinalIgnoreCase));

        var manifests = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, token) =>
            {
                var (text, provider) = pair;
                var options = provider.GetOptions(text);
                if (!options.TryGetValue("build_metadata.AdditionalFiles.IconifyBundlePack", out var prefix) ||
                    string.IsNullOrWhiteSpace(prefix))
                {
                    return null;
                }

                var content = text.GetText(token)?.ToString();
                if (content is null)
                {
                    return null;
                }

                return Manifest.Parse(prefix, content);
            })
            .Where(static manifest => manifest is not null)
            .Select(static (manifest, _) => manifest!);

        var usages = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is MemberAccessExpressionSyntax,
                static (syntaxContext, _) => Usage(syntaxContext))
            .Where(static usage => usage is not null)
            .Select(static (usage, _) => usage!.Value);

        // Razor (.razor) and Razor Pages/MVC (.cshtml) markup compiles via the Razor source generator, whose
        // output this generator never sees (generators do not observe each other's generated syntax). The
        // Razor SDK does pass the source markup files to the compiler as AdditionalFiles, so detect
        // strongly-typed pack accesses there (e.g. Feather.Activity) by scanning the markup text.
        var razorUsages = context.AdditionalTextsProvider
            .Where(static text => IsRazor(text.Path))
            .SelectMany(static (text, token) => ScanRazor(text, token));

        // Optional PNG/BMP renderings (IconifyBundleRasterSizes etc.), handed over by IconifyBundle.targets.
        var raster = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) =>
        {
            var options = provider.GlobalOptions;
            options.TryGetValue("build_property._IconifyBundleRasterFormat", out var formats);
            options.TryGetValue("build_property._IconifyBundleRasterSizes", out var sizes);
            options.TryGetValue("build_property._IconifyBundleRasterColor", out var color);
            return RasterOptions.Parse(formats, sizes, color);
        });

        context.RegisterSourceOutput(
            raster,
            static (productionContext, options) =>
            {
                if (options.Error != null)
                {
                    productionContext.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidRasterOptions, Location.None, options.Error));
                }
            });

        // Disk mode: strongly-typed per-icon path members (e.g. Feather.ActivityPath, and with rasters
        // Feather.ActivityPngPath(size)), emitted for every icon so any can be referenced. (Only referenced
        // icons are copied to output.)
        context.RegisterSourceOutput(
            manifests.Combine(disk).Combine(raster),
            static (productionContext, data) =>
            {
                var ((manifest, disk), raster) = data;
                if (disk)
                {
                    productionContext.AddSource($"{manifest.ClassName}.Paths.g.cs", Emitter.EmitPathExtensions(manifest, raster.Formats));
                }
            });

        // The icons used from each pack. Compared by value, so the outputs below re-run only when the set of
        // used icons changes.
        var packUsages = manifests.Collect()
            .Combine(usages.Collect())
            .Combine(razorUsages.Collect())
            .SelectMany(static (data, _) =>
            {
                var ((manifests, usages), razorUsages) = data;
                var result = ImmutableArray.CreateBuilder<PackUsage>();
                foreach (var manifest in manifests)
                {
                    var used = ResolveUsed(manifest, usages, razorUsages);
                    if (used.Count > 0)
                    {
                        result.Add(new(manifest, used));
                    }
                }

                return result.ToImmutable();
            });

        // Both modes: the module initializer that materialises the used icons.
        context.RegisterSourceOutput(
            packUsages.Combine(disk),
            static (productionContext, data) =>
            {
                var (usage, disk) = data;
                var manifest = usage.Manifest;
                if (disk)
                {
                    productionContext.AddSource($"{manifest.ClassName}.Registration.g.cs", Emitter.EmitDiskRegistration(manifest, usage.Used));
                    // The pack's build target reads this list to copy the matching .svg files to output.
                    productionContext.AddSource($"{manifest.ClassName}.Used.g.cs", Emitter.EmitUsedList(manifest, usage.Used));
                    return;
                }

                productionContext.AddSource($"{manifest.ClassName}.Registration.g.cs", Emitter.EmitResourceRegistration(manifest, usage.Used));
            });

        // Disk mode rasters: the pack's build target renders the files; register them, and warn here about
        // SVG features the rasteriser skips (the task cannot report against the compilation).
        context.RegisterSourceOutput(
            packUsages.Combine(raster).Combine(disk),
            static (productionContext, data) =>
            {
                var ((usage, raster), disk) = data;
                if (!disk ||
                    !raster.Enabled)
                {
                    return;
                }

                var manifest = usage.Manifest;
                productionContext.AddSource(
                    $"{manifest.ClassName}.Rasters.g.cs",
                    Emitter.EmitDiskRasters(manifest, usage.Used, raster.Formats, raster.Sizes));
                foreach (var name in usage.Used)
                {
                    if (!manifest.Data.TryGetValue(name, out var icon))
                    {
                        continue;
                    }

                    IReadOnlyCollection<string> unsupported;
                    try
                    {
                        unsupported = SvgRenderer.Analyse(icon.Body);
                    }
                    catch
                    {
                        // The build task reports icons it cannot render (IB0003).
                        continue;
                    }

                    if (unsupported.Count > 0)
                    {
                        productionContext.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedSvgFeatures, Location.None, manifest.Prefix, name, string.Join(", ", unsupported)));
                    }
                }
            });

        // Resource mode rasters: render each used icon (cached per icon, so an unchanged icon is not rendered
        // again) and embed the bytes.
        var rendered = packUsages.Combine(raster).Combine(disk)
            .SelectMany(static (data, _) =>
            {
                var ((usage, raster), disk) = data;
                var jobs = ImmutableArray.CreateBuilder<RasterJob>();
                if (disk ||
                    !raster.Enabled)
                {
                    return jobs.ToImmutable();
                }

                var manifest = usage.Manifest;
                foreach (var name in usage.Used)
                {
                    if (manifest.Data.TryGetValue(name, out var icon))
                    {
                        jobs.Add(new(manifest.Prefix, manifest.ClassName, name, icon, raster));
                    }
                }

                return jobs.ToImmutable();
            })
            .Select(static (job, _) => job.Render());

        context.RegisterSourceOutput(
            rendered.Collect(),
            static (productionContext, icons) =>
            {
                foreach (var pack in icons.GroupBy(_ => (_.Job.Prefix, _.Job.ClassName)))
                {
                    var rasters = new List<RenderedRaster>();
                    foreach (var icon in pack)
                    {
                        var job = icon.Job;
                        if (icon.Error != null)
                        {
                            productionContext.ReportDiagnostic(Diagnostic.Create(Diagnostics.RasterFailed, Location.None, job.Prefix, job.Name, icon.Error));
                            continue;
                        }

                        if (icon.Unsupported.Count > 0)
                        {
                            productionContext.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedSvgFeatures, Location.None, job.Prefix, job.Name, string.Join(", ", icon.Unsupported)));
                        }

                        rasters.AddRange(icon.Rasters);
                    }

                    if (rasters.Count > 0)
                    {
                        productionContext.AddSource($"{pack.Key.ClassName}.Rasters.g.cs", Emitter.EmitResourceRasters(pack.Key.Prefix, pack.Key.ClassName, rasters));
                    }
                }
            });
    }

    static (string Prefix, string Member)? Usage(GeneratorSyntaxContext context)
    {
        var access = (MemberAccessExpressionSyntax)context.Node;

        // Static access (Feather.Activity): the left operand resolves to the pack type itself.
        if (context.SemanticModel.GetSymbolInfo(access.Expression).Symbol is INamedTypeSymbol type &&
            PackPrefix(type) is { } prefix)
        {
            return (prefix, access.Name.Identifier.ValueText);
        }

        return null;
    }

    static string? PackPrefix(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "IconifyBundle.IconifyPackAttribute" &&
                attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string prefix)
            {
                return prefix;
            }
        }

        return null;
    }

    // Member name -> icon name, built once per manifest (the used set is recomputed on every edit, and for
    // the largest packs the three name sets are tens of thousands of entries).
    static readonly ConditionalWeakTable<Manifest, Dictionary<string, string>> memberLookups = new();

    static Dictionary<string, string> MemberLookup(Manifest manifest) =>
        memberLookups.GetValue(manifest, static manifest =>
        {
            var memberToIcon = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (icon, member) in Emitter.Members(manifest))
            {
                memberToIcon[member] = icon;
            }

            // Path members (Disk mode) count as a use of their icon too, as do the raster path members.
            foreach (var (icon, member) in Emitter.PathMembers(manifest))
            {
                memberToIcon[member] = icon;
            }

            foreach (var (icon, png, bmp) in Emitter.RasterPathMembers(manifest))
            {
                memberToIcon[png] = icon;
                memberToIcon[bmp] = icon;
            }

            return memberToIcon;
        });

    static List<string> ResolveUsed(
        Manifest manifest,
        IEnumerable<(string Prefix, string Member)> usages,
        IEnumerable<(string ClassName, string Member)> razorUsages)
    {
        var memberToIcon = MemberLookup(manifest);
        var used = new SortedSet<string>(StringComparer.Ordinal);

        // C# usages are resolved semantically to the pack prefix; razor usages are textual, so they carry
        // the class-name token instead and match against the pack's class name.
        foreach (var (prefix, member) in usages)
        {
            if (prefix == manifest.Prefix &&
                memberToIcon.TryGetValue(member, out var icon))
            {
                used.Add(icon);
            }
        }

        foreach (var (className, member) in razorUsages)
        {
            if (className == manifest.ClassName &&
                memberToIcon.TryGetValue(member, out var icon))
            {
                used.Add(icon);
            }
        }

        return used.ToList();
    }

    static bool IsRazor(string path) =>
        path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);

    // Matches a 'Left.Member' access. The member is captured via look-ahead (not consumed) so that in a
    // qualified chain like 'IconifyBundle.Feather.Activity' both 'IconifyBundle.Feather' and 'Feather.Activity'
    // are found - the latter being the pack access we care about. Resolution discards anything whose left
    // token is not a referenced pack's class name, so non-icon matches are harmless.
    static readonly Regex memberAccess =
        new(@"([A-Za-z_][A-Za-z0-9_]*)\s*\.\s*(?=([A-Za-z_][A-Za-z0-9_]*))");

    static ImmutableArray<(string ClassName, string Member)> ScanRazor(AdditionalText text, Cancel token)
    {
        var content = text.GetText(token)?.ToString();
        if (string.IsNullOrEmpty(content))
        {
            return [];
        }

        var seen = new HashSet<(string, string)>();
        foreach (Match match in memberAccess.Matches(content))
        {
            seen.Add((match.Groups[1].Value, match.Groups[2].Value));
        }

        return seen.ToImmutableArray();
    }
}
