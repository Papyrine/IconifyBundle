using Microsoft.CodeAnalysis;

namespace Iconistic.Generator;

static class Diagnostics
{
    const string Category = "Iconistic";

    public static readonly DiagnosticDescriptor DownloadFailed = new(
        "ICON001",
        "Icon pack download failed",
        "Failed to download Iconify pack '{0}': {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Offline = new(
        "ICON002",
        "Icon pack not cached",
        "Iconify pack '{0}' is not in the cache and IconisticOffline is set; no icons were generated for it",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IconNotFound = new(
        "ICON003",
        "Icon not found",
        "Icon(s) not found in Iconify collection '{0}': {1}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor EmptyPack = new(
        "ICON004",
        "Icon pack declares no icons",
        "[assembly: IconPack(\"{0}\")] declares no icons, so nothing was generated",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
