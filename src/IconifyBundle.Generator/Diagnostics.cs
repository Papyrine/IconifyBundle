namespace IconifyBundle.Generator;

static class Diagnostics
{
    const string category = "IconifyBundle";

    public static readonly DiagnosticDescriptor InvalidRasterOptions = new(
        id: "IB0001",
        title: "Invalid IconifyBundle raster properties",
        messageFormat: "{0}",
        category: category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedSvgFeatures = new(
        id: "IB0002",
        title: "Icon uses SVG features the build-time rasteriser does not render",
        messageFormat: "Icon '{0}:{1}' uses SVG features the build-time rasteriser does not render ({2}); its PNG/BMP may differ from the SVG",
        category: category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RasterFailed = new(
        id: "IB0003",
        title: "Icon could not be rasterised",
        messageFormat: "Icon '{0}:{1}' could not be rasterised: {2}",
        category: category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
