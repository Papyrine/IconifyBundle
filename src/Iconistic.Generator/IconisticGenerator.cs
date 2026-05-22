using Microsoft.CodeAnalysis;

namespace Iconistic.Generator;

/// <summary>
/// The strongly-typed pack class (one member per icon) is compiled into each <c>Iconistic.&lt;Pack&gt;</c>
/// assembly at pack time. This generator only adds the per-icon file-path members - as static extension
/// properties on the pack class (e.g. <c>Feather.ActivityPath</c>) - and only when the consumer sets the
/// <c>IconisticExtractDisk</c> MSBuild property.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class IconisticGenerator :
    IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var extractDisk = context.AnalyzerConfigOptionsProvider.Select(
            (provider, _) =>
                provider.GlobalOptions.TryGetValue("build_property.IconisticExtractDisk", out var value) &&
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));

        var manifests = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, token) =>
            {
                var (text, provider) = pair;
                var options = provider.GetOptions(text);
                if (!options.TryGetValue("build_metadata.AdditionalFiles.IconisticPack", out var prefix) ||
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

        var combined = manifests.Combine(extractDisk);
        context.RegisterSourceOutput(
            combined,
            static (productionContext, pair) =>
            {
                var (manifest, disk) = pair;
                if (!disk)
                {
                    return;
                }

                productionContext.AddSource($"{manifest.ClassName}.Paths.g.cs", Emitter.EmitPathExtensions(manifest));
            });
    }
}
