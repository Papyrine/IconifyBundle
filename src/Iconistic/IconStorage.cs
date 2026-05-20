namespace Iconistic;

/// <summary>
/// Controls how the SVG data for an icon pack is included in the consuming assembly.
/// </summary>
public enum IconStorage
{
    /// <summary>
    /// SVG bodies are emitted directly into the generated C# source as string literals.
    /// Zero runtime files, trimmer friendly. The default.
    /// </summary>
    BakedIn,

    /// <summary>
    /// The pack is embedded in the assembly as a compact JSON string constant and parsed
    /// lazily at runtime. Produces smaller generated code for very large packs.
    /// </summary>
    EmbeddedResource,

    /// <summary>
    /// The pack JSON is written to a deploy directory (<c>wwwroot/iconistic</c> by default)
    /// and fetched at runtime. Ideal for Blazor where <c>wwwroot</c> serves static assets.
    /// Requires calling the generated <c>InitializeAsync</c> at startup.
    /// </summary>
    DeployedFile
}
