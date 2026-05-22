using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Iconistic.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Tests;

static class GeneratorRunner
{
    public static GeneratorResult Run(
        string? manifest,
        string prefix = "feather",
        bool diskMode = false,
        bool includeMarker = true)
    {
        var sources = new List<SyntaxTree>
        {
            CSharpSyntaxTree.ParseText("public class Dummy;")
        };

        if (includeMarker)
        {
            var markerClass = IdentifierNaming.ToPascalCase(prefix) + "Pack";
            sources.Add(CSharpSyntaxTree.ParseText(
                $$"""
                  namespace IconisticPacks;
                  public static class {{markerClass}};
                  """));
        }

        var references = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(_ => _.Length > 0)
            .Select(_ => (MetadataReference) MetadataReference.CreateFromFile(_))
            .Append(MetadataReference.CreateFromFile(typeof(Icon).Assembly.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "GeneratorTest",
            sources,
            references,
            new(OutputKind.DynamicallyLinkedLibrary));

        var additionalTexts = manifest is null
            ? ImmutableArray<AdditionalText>.Empty
            : [new TestAdditionalText("pack.manifest", manifest)];

        var driver = CSharpGeneratorDriver.Create(
            generators: [new IconisticGenerator().AsSourceGenerator()],
            additionalTexts: additionalTexts,
            parseOptions: null,
            optionsProvider: new TestOptionsProvider(diskMode, prefix));

        var runResult = driver
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _)
            .GetRunResult();

        var errors = output
            .GetDiagnostics()
            .Where(_ => _.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        var generated = runResult.GeneratedTrees
            .Select(_ => _.ToString())
            .ToImmutableArray();

        return new(generated, errors);
    }

    public sealed record GeneratorResult(
        ImmutableArray<string> GeneratedSources,
        ImmutableArray<Diagnostic> CompileErrors)
    {
        public string? Single() => GeneratedSources.SingleOrDefault();
    }

    sealed class TestAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(text);
    }

    sealed class TestOptionsProvider(bool diskMode, string prefix) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new GlobalOptionsImpl(diskMode);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => EmptyOptions.Instance;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new FileOptions(prefix);
    }

    sealed class GlobalOptionsImpl(bool diskMode) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (key == "build_property.IconisticMode")
            {
                value = diskMode ? "Disk" : "Resource";
                return true;
            }

            value = null;
            return false;
        }
    }

    sealed class FileOptions(string prefix) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (key == "build_metadata.AdditionalFiles.IconisticPack")
            {
                value = prefix;
                return true;
            }

            value = null;
            return false;
        }
    }

    sealed class EmptyOptions : AnalyzerConfigOptions
    {
        public static readonly EmptyOptions Instance = new();

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            value = null;
            return false;
        }
    }
}
