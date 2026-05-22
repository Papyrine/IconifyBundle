using System;
using Microsoft.CodeAnalysis;

namespace Iconistic.Generator;

/// <summary>
/// Emits a strongly-typed API for every referenced <c>Iconistic.&lt;Pack&gt;</c> NuGet. For each
/// pack manifest (an <c>AdditionalFiles</c> entry tagged with <c>IconisticPack</c> metadata) a
/// <c>public static partial class</c> is generated under the <c>Iconistic</c> namespace with one
/// member per icon. The shape of the API depends on the <c>IconisticMode</c> MSBuild property.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class IconisticGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var diskMode = context.AnalyzerConfigOptionsProvider.Select(
            (provider, _) =>
                provider.GlobalOptions.TryGetValue("build_property.IconisticMode", out var value) &&
                string.Equals(value, "Disk", StringComparison.OrdinalIgnoreCase));

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

                return Manifest.Parse(prefix!, content);
            })
            .Where(static manifest => manifest is not null)
            .Select(static (manifest, _) => manifest!);

        var combined = manifests.Combine(diskMode);
        context.RegisterSourceOutput(
            combined,
            static (productionContext, pair) =>
            {
                var (manifest, disk) = pair;
                var source = Emitter.Emit(manifest, disk);
                productionContext.AddSource($"{manifest.ClassName}.g.cs", source);
            });
    }
}
