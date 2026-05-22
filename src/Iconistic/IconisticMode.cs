namespace Iconistic;

/// <summary>
/// Controls how icon pack data is delivered and how the generated API is shaped.
/// Configured via the <c>IconisticMode</c> MSBuild property (default <see cref="Resource"/>).
/// </summary>
public enum IconisticMode
{
    /// <summary>
    /// Icon data is loaded from resources embedded in the <c>Iconistic.&lt;Pack&gt;</c> assembly.
    /// The generated API terminates in a stream/string based surface.
    /// </summary>
    Resource,

    /// <summary>
    /// Icon SVG files are additionally copied to the build output. The generated API also
    /// exposes a file-path based surface in addition to the stream/string surface.
    /// </summary>
    Disk
}
