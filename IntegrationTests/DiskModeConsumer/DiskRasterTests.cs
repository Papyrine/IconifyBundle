namespace DiskModeConsumer;

// Disk mode with IconifyBundleRasterSizes=16;24 (see the csproj): the pack's build target renders the
// referenced icons to PNG next to their .svg files, and the generator emits a path member per format.
public class DiskRasterTests
{
    [Test]
    public async Task Png_path_member_points_to_the_written_file()
    {
        #region RasterDiskUsage
        // iconifybundle/feather/16/activity.png under the output directory.
        var path = Feather.ActivityPngPath(16);

        // The same file, for an icon picked by name.
        var byName = Feather.PngPathOf("activity", 16);

        // Icon.OpenPng/Png read the file in Disk mode, so the code is the same in both modes.
        using var stream = Feather.Activity.OpenPng(16);
        #endregion

        await Assert.That(byName).IsEqualTo(path);
        await Assert.That(path).IsEqualTo(Path.Combine(AppContext.BaseDirectory, "iconifybundle", "feather", "16", "activity.png"));
        var bytes = await File.ReadAllBytesAsync(path);
        await Assert.That(bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })).IsTrue();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        await Assert.That(memory.ToArray().SequenceEqual(bytes)).IsTrue();
    }

    [Test]
    public async Task Each_configured_size_is_written()
    {
        await Assert.That(File.Exists(Feather.AlertCirclePngPath(24))).IsTrue();
        await Assert.That(Feather.AlertCircle.PngSizes).IsEquivalentTo([16, 24]);
    }

    // Only png was asked for (the default format), so no BMP was written or registered.
    [Test]
    public async Task Unrequested_format_is_absent()
    {
        await Assert.That(Feather.Activity.BmpSizes).IsEmpty();
        await Assert.That(File.Exists(IconPack.ForPrefix("feather").BmpPathOf("activity", 16))).IsFalse();
    }
}
