namespace Iconistic.Generator;

/// <summary>Mirrors <c>Iconistic.IconStorage</c> without referencing the runtime assembly.</summary>
enum Storage
{
    BakedIn = 0,
    EmbeddedResource = 1,
    DeployedFile = 2
}

/// <summary>A single <c>[assembly: IconPack(...)]</c> declaration, made equatable for the pipeline.</summary>
sealed record PackSpec(string Prefix, EquatableArray<string> Icons, Storage Storage);

/// <summary>Resolved generator settings, sourced from MSBuild properties and the compilation.</summary>
sealed record Settings(
    string RootNamespace,
    string CacheDirectory,
    string DeployDirectory,
    bool Offline);

/// <summary>A fully resolved icon: body plus viewBox and any transform.</summary>
readonly record struct NormIcon(
    string Body,
    int Width,
    int Height,
    int Left,
    int Top,
    int Rotate,
    bool HFlip,
    bool VFlip);
