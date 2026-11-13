namespace IconifyBundle;

/// <summary>
/// Returns the build-time rasters of one pack, back to back, as generated into the consuming assembly in
/// Resource mode. The span points at constant data in the assembly, so nothing is allocated until a raster
/// is read. Used by generated code; not intended for direct use.
/// </summary>
public delegate ReadOnlySpan<byte> RasterData();
