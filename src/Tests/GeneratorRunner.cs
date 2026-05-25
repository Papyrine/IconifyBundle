static class GeneratorRunner
{
    public static GeneratorDriverRunResult Run(
        string? data,
        bool disk,
        string prefix = "feather",
        string source = "public class Dummy;",
        string? razor = null)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var compilation = CSharpCompilation.Create(
            "GeneratorTest",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references,
            new(OutputKind.DynamicallyLinkedLibrary));

        // Razor markup is handed to the compiler as an AdditionalFile (the Razor SDK does this so its own
        // generator can read it); the icon data is an AdditionalFile too. Both flow through AdditionalTexts.
        var additionalTexts = ImmutableArray.CreateBuilder<AdditionalText>();
        if (data is not null)
        {
            additionalTexts.Add(new TestAdditionalText("pack.icondata", data));
        }

        if (razor is not null)
        {
            additionalTexts.Add(new TestAdditionalText("Component.razor", razor));
        }

        var driver = CSharpGeneratorDriver.Create(
            generators: [new IconifyBundleGenerator().AsSourceGenerator()],
            additionalTexts: additionalTexts.ToImmutable(),
            parseOptions: parseOptions,
            optionsProvider: new TestOptionsProvider(disk, prefix));

        return driver.RunGenerators(compilation).GetRunResult();
    }

    static List<MetadataReference> references = BuildReferences();

    static List<MetadataReference> BuildReferences()
    {
        var references = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(_ => _.Length > 0)
            .Select(_ => (MetadataReference) MetadataReference.CreateFromFile(_))
            .ToList();

        // The IconifyBundle runtime, so [IconifyPack]/Icon resolve and usage detection works.
        references.Add(MetadataReference.CreateFromFile(typeof(IconifyPackAttribute).Assembly.Location));
        return references;
    }

    sealed class TestAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(Cancel cancel = default) =>
            SourceText.From(text);
    }

    sealed class TestOptionsProvider(bool disk, string prefix) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new GlobalOptionsImpl(disk);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => EmptyOptions.Instance;

        // Only the pack data file carries the IconifyBundlePack metadata; razor files (and anything else)
        // are plain additional files, matching how the real build tags them.
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            textFile.Path.EndsWith(".icondata", StringComparison.Ordinal)
                ? new FileOptions(prefix)
                : EmptyOptions.Instance;
    }

    sealed class GlobalOptionsImpl(bool disk) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (key == "build_property.IconifyBundleMode")
            {
                value = disk ? "Disk" : "Resource";
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
            if (key == "build_metadata.AdditionalFiles.IconifyBundlePack")
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
