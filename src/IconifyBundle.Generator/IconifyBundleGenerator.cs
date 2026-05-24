namespace IconifyBundle.Generator;

/// <summary>
/// Runs in the consuming compilation. It reads the pack data files shipped by the referenced
/// <c>IconifyBundle.&lt;Pack&gt;</c> packages, detects which icons the consumer actually uses (static
/// member accesses on <c>[IconifyPack]</c> types), and materialises only those:
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
            .Combine(disk);
        context.RegisterSourceOutput(
            registration,
            static (productionContext, data) =>
            {
                var ((manifests, usages), disk) = data;
                foreach (var manifest in manifests)
                {
                    var used = ResolveUsed(manifest, usages);
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

    static List<string> ResolveUsed(Manifest manifest, IEnumerable<(string Prefix, string Member)> usages)
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
        foreach (var (prefix, member) in usages)
        {
            if (prefix == manifest.Prefix &&
                memberToIcon.TryGetValue(member, out var icon))
            {
                used.Add(icon);
            }
        }

        return used.ToList();
    }
}
