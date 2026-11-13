using System.Globalization;

namespace IconifyBundle.Raster;

/// <summary>
/// The raster output a consumer asks for through MSBuild: the formats (<c>png</c>, <c>bmp</c>), the pixel
/// sizes, and the colour that stands in for <c>currentColor</c>. Rasters are produced only when at least one
/// size is given. Shared (as source) by the generator and the build task, so both read the properties the
/// same way.
/// </summary>
sealed class RasterOptions :
    IEquatable<RasterOptions>
{
    /// <summary>The largest accepted size, so a typo cannot bake megabytes of pixels into an assembly.</summary>
    public const int MaxSize = 1024;

    public static readonly RasterOptions Disabled = new(false, false, [], 0xFF000000, null);

    readonly int[] sizes;

    RasterOptions(bool png, bool bmp, int[] sizes, uint color, string? error)
    {
        Png = png;
        Bmp = bmp;
        this.sizes = sizes;
        Color = color;
        Error = error;
    }

    public bool Png { get; }

    public bool Bmp { get; }

    /// <summary>The pixel sizes, ascending and distinct.</summary>
    public IReadOnlyList<int> Sizes => sizes;

    /// <summary>The colour used for <c>currentColor</c>, as <c>0xAARRGGBB</c>.</summary>
    public uint Color { get; }

    /// <summary>Why the properties could not be used, or null when they are valid.</summary>
    public string? Error { get; }

    public bool Enabled => Error == null && sizes.Length > 0;

    /// <summary>The formats to produce, <c>png</c> before <c>bmp</c>; empty when rasters are off.</summary>
    public IReadOnlyList<string> Formats
    {
        get
        {
            var formats = new List<string>();
            if (!Enabled)
            {
                return formats;
            }

            if (Png)
            {
                formats.Add("png");
            }

            if (Bmp)
            {
                formats.Add("bmp");
            }

            return formats;
        }
    }

    /// <summary>
    /// Reads the <c>IconifyBundleRasterFormat</c>, <c>IconifyBundleRasterSizes</c> and
    /// <c>IconifyBundleRasterColor</c> values. Lists may be separated by <c>;</c> or <c>,</c>; the colour may
    /// omit its <c>#</c> (the build hands it to the generator without one).
    /// </summary>
    public static RasterOptions Parse(string? formats, string? sizes, string? color)
    {
        var png = false;
        var bmp = false;
        foreach (var token in Split(formats))
        {
            switch (token.ToLowerInvariant())
            {
                case "png":
                    png = true;
                    break;
                case "bmp":
                    bmp = true;
                    break;
                default:
                    return Invalid($"IconifyBundleRasterFormat '{formats}' is not valid: '{token}' is not a supported format (use png, bmp, or both)");
            }
        }

        var sizeList = new List<int>();
        foreach (var token in Split(sizes))
        {
            if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var size) ||
                size < 1 ||
                size > MaxSize)
            {
                return Invalid($"IconifyBundleRasterSizes '{sizes}' is not valid: '{token}' is not a whole number of pixels from 1 to {MaxSize}");
            }

            if (!sizeList.Contains(size))
            {
                sizeList.Add(size);
            }
        }

        if (sizeList.Count == 0)
        {
            if (png || bmp)
            {
                return Invalid("IconifyBundleRasterFormat is set but IconifyBundleRasterSizes is empty: give one or more pixel sizes, for example 16;24;32");
            }

            return Disabled;
        }

        sizeList.Sort();

        if (!png && !bmp)
        {
            png = true;
        }

        // Opaque black. The literal is a uint, being past int.MaxValue.
        var argb = 0xFF000000;
        if (color != null &&
            color.Trim().Length > 0)
        {
            if (!SvgColor.TryParse(color, out var parsed))
            {
                return Invalid($"IconifyBundleRasterColor '{color}' is not a colour: use #RRGGBB, #RRGGBBAA or a CSS colour name");
            }

            argb = parsed.ToArgb();
        }

        return new(png, bmp, sizeList.ToArray(), argb, null);
    }

    static RasterOptions Invalid(string error) =>
        new(false, false, [], 0xFF000000, error);

    static readonly char[] separators = [';', ',', ' ', '\t'];

    static IEnumerable<string> Split(string? value)
    {
        if (value == null)
        {
            return [];
        }

        return value
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(_ => _.Trim())
            .Where(_ => _.Length > 0);
    }

    public bool Equals(RasterOptions? other)
    {
        if (other is null)
        {
            return false;
        }

        return Png == other.Png &&
               Bmp == other.Bmp &&
               Color == other.Color &&
               Error == other.Error &&
               sizes.SequenceEqual(other.sizes);
    }

    public override bool Equals(object? obj) =>
        Equals(obj as RasterOptions);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int)Color;
            foreach (var size in sizes)
            {
                hash = (hash * 397) ^ size;
            }

            return hash;
        }
    }
}
