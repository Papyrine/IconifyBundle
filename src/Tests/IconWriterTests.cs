public class IconWriterTests
{
    // The disk-mode reconstruction must produce exactly what the runtime's SvgBuilder produces, so that
    // an on-disk file and the in-assembly (Resource mode) rendering agree.
    [Test]
    [Arguments(24, 24)]
    [Arguments(16, 16)]
    [Arguments(25.9, 30)]
    [Arguments(48, 17.5)]
    public async Task BuildSvg_matches_SvgBuilder(double width, double height)
    {
        const string body = "<path stroke=\"currentColor\" d=\"M1 1\"/>";

        var expected = SvgBuilder.Build(new("activity", body, width, height));
        await Assert.That(IconWriter.BuildSvg(width, height, body)).IsEqualTo(expected);
    }

    [Test]
    public async Task ParseIconData_round_trips_escaped_body()
    {
        // A body containing a tab and backslash exercises the escape/unescape round-trip.
        const string body = "<path d=\"M1\t2\\3\"/>";
        var content =
            "prefix=feather\n" +
            "class=Feather\n" +
            "\n" +
            Manifest.FormatDataLine("activity", 24, 25.9, body) + "\n";

        var data = IconWriter.ParseIconData(content);

        await Assert.That(data.ContainsKey("activity")).IsTrue();
        await Assert.That(data["activity"].Body).IsEqualTo(body);
        await Assert.That(data["activity"].Width).IsEqualTo(24);
        await Assert.That(data["activity"].Height).IsEqualTo(25.9);
    }

    [Test]
    public async Task Write_emits_only_used_icons_matching_SvgBuilder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "iconwriter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var dataPath = Path.Combine(dir, "feather.icondata");
        await File.WriteAllTextAsync(
            dataPath,
            "prefix=feather\n\n" +
            Manifest.FormatDataLine("activity", 24, 24, "<path d=\"M1 1\"/>") + "\n" +
            Manifest.FormatDataLine("zap", 24, 24, "<rect/>") + "\n");
        var outDir = Path.Combine(dir, "out");

        try
        {
            var missing = IconWriter.Write(dataPath, ["activity", "ghost"], outDir);

            var activity = Path.Combine(outDir, "activity.svg");
            await Assert.That(File.Exists(activity)).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(activity))
                .IsEqualTo(SvgBuilder.Build(new("activity", "<path d=\"M1 1\"/>", 24, 24)));
            // 'zap' exists in the pack but was not requested - not written.
            await Assert.That(File.Exists(Path.Combine(outDir, "zap.svg"))).IsFalse();
            // 'ghost' was requested but is not in the pack - reported as missing.
            await Assert.That(missing).Contains("ghost");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
