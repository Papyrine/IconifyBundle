using System.Buffers.Binary;

namespace IntegrationTests;

// Resource mode with IconifyBundleRasterFormat=png;bmp, IconifyBundleRasterSizes=16;24 and
// IconifyBundleRasterColor=#1E90FF (see the csproj): the generator renders the referenced icons at build time
// and bakes the PNG and BMP bytes into this assembly, next to their SVG.
public class RasterTests
{
    [Test]
    public async Task Png_is_embedded_at_each_configured_size()
    {
        #region RasterResourceUsage
        // A PNG rendered at build time, at any size listed in IconifyBundleRasterSizes...
        using var stream = Feather.Activity.OpenPng(24);

        // ...or its bytes, e.g. for a data: URI.
        var png = Feather.Activity.Png(16);

        // The sizes produced for the icon: [16, 24].
        var sizes = Feather.Activity.PngSizes;
        #endregion

        await Assert.That(sizes).IsEquivalentTo([16, 24]);
        await Assert.That(PngSize(png.ToArray())).IsEqualTo((16, 16));
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        await Assert.That(PngSize(memory.ToArray())).IsEqualTo((24, 24));
    }

    [Test]
    public async Task Bmp_is_embedded_in_the_configured_colour()
    {
        var bmp = Feather.Activity.Bmp(16).ToArray();

        await Assert.That(bmp[0]).IsEqualTo((byte)'B');
        await Assert.That(bmp[1]).IsEqualTo((byte)'M');
        await Assert.That(BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(18))).IsEqualTo(16);
        await Assert.That(BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(22))).IsEqualTo(16);
        await Assert.That(Feather.Activity.BmpSizes).IsEquivalentTo([16, 24]);

        // Every fully opaque pixel is #1E90FF, stored as B, G, R, A.
        var offset = BinaryPrimitives.ReadInt32LittleEndian(bmp.AsSpan(10));
        var opaque = 0;
        for (var i = offset; i < bmp.Length; i += 4)
        {
            if (bmp[i + 3] != 255)
            {
                continue;
            }

            opaque++;
            await Assert.That(bmp.AsSpan(i, 3).ToArray()).IsEquivalentTo(new byte[] { 0xFF, 0x90, 0x1E });
        }

        await Assert.That(opaque).IsGreaterThan(0);
    }

    // Image snapshot of what was baked in.
    [Test]
    public Task Png_snapshot() =>
        Verify(Feather.AlertCircle.OpenPng(24), "png");

    [Test]
    public async Task Unconfigured_size_throws()
    {
        var exception = await Assert.That(() => Feather.Activity.OpenPng(20)).Throws<KeyNotFoundException>();

        await Assert.That(exception!.Message).Contains("Available sizes: 16, 24");
    }

    // An icon built by hand (not materialised by the generator) has no rasters.
    [Test]
    public async Task Hand_built_icon_has_no_rasters()
    {
        var icon = new Icon("custom", "dot", "<circle r=\"1\"/>", 2, 2);

        await Assert.That(icon.PngSizes).IsEmpty();
        await Assert.That(() => icon.OpenPng(16)).Throws<KeyNotFoundException>();
    }

    static (int Width, int Height) PngSize(byte[] png)
    {
        if (!png.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            throw new("Not a PNG");
        }

        return (BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)), BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
    }
}
