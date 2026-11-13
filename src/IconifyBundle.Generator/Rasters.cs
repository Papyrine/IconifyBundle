namespace IconifyBundle.Generator;

/// <summary>One encoded raster of a used icon, as embedded by <see cref="Emitter.EmitResourceRasters"/>.</summary>
public sealed record RenderedRaster(string Name, string Format, int Size, byte[] Bytes);

/// <summary>
/// A pack and the icons the consumer uses from it. Compared by value, so the steps downstream of it (the
/// registrations and the rasters) re-run only when the set of used icons changes, not on every edit.
/// </summary>
sealed class PackUsage(Manifest manifest, List<string> used) :
    IEquatable<PackUsage>
{
    public Manifest Manifest { get; } = manifest;
    public List<string> Used { get; } = used;

    public bool Equals(PackUsage? other)
    {
        if (other is null)
        {
            return false;
        }

        return (ReferenceEquals(Manifest, other.Manifest) || Manifest.Equals(other.Manifest)) &&
               Used.SequenceEqual(other.Used);
    }

    public override bool Equals(object? obj) => Equals(obj as PackUsage);

    public override int GetHashCode() => Manifest.GetHashCode() ^ Used.Count;
}

/// <summary>
/// One used icon to render in Resource mode. It carries only what rendering needs (not the whole manifest),
/// so comparing it to the previous run is cheap and an unchanged icon is not rendered again.
/// </summary>
sealed record RasterJob(string Prefix, string ClassName, string Name, IconData Data, RasterOptions Options)
{
    public RenderedIcon Render()
    {
        try
        {
            var rasters = new List<RenderedRaster>();
            IReadOnlyCollection<string> unsupported = [];
            foreach (var size in Options.Sizes)
            {
                var image = IconRaster.Render(Data.Body, Data.Width, Data.Height, size, Options.Color);
                unsupported = image.Unsupported;
                foreach (var format in Options.Formats)
                {
                    rasters.Add(new(Name, format, size, IconRaster.Encode(image, format)));
                }
            }

            return new(this, rasters, unsupported, null);
        }
        catch (Exception exception)
        {
            return new(this, [], [], exception.Message);
        }
    }
}

sealed record RenderedIcon(
    RasterJob Job,
    IReadOnlyList<RenderedRaster> Rasters,
    IReadOnlyCollection<string> Unsupported,
    string? Error);
