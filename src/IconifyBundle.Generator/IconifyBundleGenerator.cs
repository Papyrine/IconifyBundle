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

        // Disk mode: strongly-typed per-icon path members (e.g. Feather.ActivityPath), emitted for every
        // icon so any can be referenced. (Only referenced icons are copied to output.)
        context.RegisterSourceOutput(
            manifests.Combine(disk),
            static (productionContext, pair) =>
            {
                var (manifest, disk) = pair;
                if (disk)
                {
                    productionContext.AddSource($"{manifest.ClassName}.Paths.g.cs", Emitter.EmitPathExtensions(manifest));
                }
            });

        // Both modes: the module initializer that materialises the used icons.
        var registration = manifests.Collect()
            .Combine(usages.Collect())
            .Combine(razorUsages.Collect())
            .Combine(disk);
        context.RegisterSourceOutput(
            registration,
            static (productionContext, data) =>
            {
                var (((manifests, usages), razorUsages), disk) = data;
                foreach (var manifest in manifests)
                {
                    var used = ResolveUsed(manifest, usages, razorUsages);
                    if (used.Count == 0)
                    {
                        continue;
                    }

                    if (disk)
                    {
                        productionContext.AddSource($"{manifest.ClassName}.Registration.g.cs", Emitter.EmitDiskRegistration(manifest, used));
                        // The pack's build target reads this list to copy the matching .svg files to output.
                        productionContext.AddSource($"{manifest.ClassName}.Used.g.cs", Emitter.EmitUsedList(manifest, used));
                    }
                    else
                    {
                        productionContext.AddSource($"{manifest.ClassName}.Registration.g.cs", Emitter.EmitResourceRegistration(manifest, used));
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

    static List<string> ResolveUsed(
        Manifest manifest,
        IEnumerable<(string Prefix, string Member)> usages,
        IEnumerable<(string ClassName, string Member)> razorUsages)
    {
        var memberToIcon = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (icon, member) in Emitter.Members(manifest))
        {
            memberToIcon[member] = icon;
        }

        // Path members (Disk mode) count as a use of their icon too.
        foreach (var (icon, member) in Emitter.PathMembers(manifest))
        {
            memberToIcon[member] = icon;
        }

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
