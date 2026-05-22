using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

static class GeneratorRunner
{
    public static GeneratorDriverRunResult Run(
        string? manifest,
        bool extractDisk,
        string prefix = "feather")
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var compilation = CSharpCompilation.Create(
            "GeneratorTest",
            [CSharpSyntaxTree.ParseText("public class Dummy;", parseOptions)],
            References,
            new(OutputKind.DynamicallyLinkedLibrary));

        ImmutableArray<AdditionalText> additionalTexts = manifest is null
            ? []
            : [new TestAdditionalText("pack.manifest", manifest)];

        var driver = CSharpGeneratorDriver.Create(
            generators: [new IconisticGenerator().AsSourceGenerator()],
            additionalTexts: additionalTexts,
            parseOptions: parseOptions,
            optionsProvider: new TestOptionsProvider(extractDisk, prefix));

        return driver.RunGenerators(compilation).GetRunResult();
    }

    static readonly List<MetadataReference> References =
        ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(_ => _.Length > 0)
        .Select(_ => (MetadataReference) MetadataReference.CreateFromFile(_))
        .ToList();

    sealed class TestAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(Cancel cancel = default) =>
            SourceText.From(text);
    }

    sealed class TestOptionsProvider(bool extractDisk, string prefix) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new GlobalOptionsImpl(extractDisk);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => EmptyOptions.Instance;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new FileOptions(prefix);
    }

    sealed class GlobalOptionsImpl(bool extractDisk) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (key == "build_property.IconisticExtractDisk")
            {
                value = extractDisk ? "true" : "false";
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
