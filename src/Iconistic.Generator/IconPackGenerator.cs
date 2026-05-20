using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Iconistic.Generator;

/// <summary>
/// Reads <c>[assembly: IconPack(...)]</c> declarations, downloads the icons from the Iconify API
/// (cached on disk), and generates a strongly typed <c>Icons</c> API.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class IconPackGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var packs = context.CompilationProvider.Select(static (compilation, _) => ReadPacks(compilation));

        var settings = context.AnalyzerConfigOptionsProvider
            .Combine(context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName ?? ""))
            .Select(static (pair, _) => ReadSettings(pair.Left, pair.Right));

        var combined = packs.Combine(settings);

        context.RegisterSourceOutput(combined, static (productionContext, data) =>
            Execute(productionContext, data.Left, data.Right));
    }

    static void Execute(SourceProductionContext context, EquatableArray<PackSpec> packs, Settings settings)
    {
        if (packs.Count == 0)
        {
            return;
        }

        var diagnostics = new List<Diagnostic>();
        var source = SourceEmitter.Emit([.. packs], settings, diagnostics);

        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        context.AddSource("Icons.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    static EquatableArray<PackSpec> ReadPacks(Compilation compilation)
    {
        var packs = new List<PackSpec>();

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass is null ||
                attributeClass.Name != "IconPackAttribute" ||
                attributeClass.ContainingNamespace?.ToDisplayString() != "Iconistic" ||
                attribute.ConstructorArguments.Length < 2)
            {
                continue;
            }

            if (attribute.ConstructorArguments[0].Value is not string prefix ||
                string.IsNullOrWhiteSpace(prefix))
            {
                continue;
            }

            var iconsArgument = attribute.ConstructorArguments[1];
            var icons = iconsArgument.Kind == TypedConstantKind.Array
                ? iconsArgument.Values
                    .Select(static value => value.Value as string)
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value!)
                    .ToArray()
                : [];

            var storage = Storage.BakedIn;
            foreach (var named in attribute.NamedArguments)
            {
                if (named is { Key: "Storage", Value.Value: int value })
                {
                    storage = (Storage) value;
                }
            }

            packs.Add(new(prefix, EquatableArray<string>.From(icons), storage));
        }

        return EquatableArray<PackSpec>.From(packs);
    }

    static Settings ReadSettings(AnalyzerConfigOptionsProvider options, string assemblyName)
    {
        var global = options.GlobalOptions;

        var rootNamespace = TryGet(global, "build_property.RootNamespace");
        if (string.IsNullOrWhiteSpace(rootNamespace))
        {
            rootNamespace = assemblyName;
        }

        var cacheDirectory = TryGet(global, "build_property.IconisticCacheDirectory");
        if (string.IsNullOrWhiteSpace(cacheDirectory))
        {
            cacheDirectory = DefaultCacheDirectory();
        }

        var deployDirectory = TryGet(global, "build_property.IconisticDeployDirectory");
        if (string.IsNullOrWhiteSpace(deployDirectory))
        {
            var projectDirectory = TryGet(global, "build_property.IconisticProjectDirectory");
            deployDirectory = string.IsNullOrWhiteSpace(projectDirectory)
                ? Path.Combine(Path.GetTempPath(), "Iconistic", "deploy")
                : Path.Combine(projectDirectory!, "wwwroot", "iconistic");
        }

        var offline = string.Equals(
            TryGet(global, "build_property.IconisticOffline"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        return new(rootNamespace!, cacheDirectory!, deployDirectory!, offline);
    }

    static string DefaultCacheDirectory()
    {
        string root;
        try
        {
            root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        catch
        {
            root = "";
        }

        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.GetTempPath();
        }

        return Path.Combine(root, "Iconistic", "cache");
    }

    static string? TryGet(AnalyzerConfigOptions options, string key) =>
        options.TryGetValue(key, out var value) ? value : null;
}
