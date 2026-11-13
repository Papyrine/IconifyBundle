public class EmitterTests
{
    static Manifest Sample() =>
        Manifest.Parse(
            "feather",
            $"""
             prefix=feather
             class=Feather

             {Manifest.FormatDataLine("activity", 24, 24, """<path d="M12 2v20"/>""")}
             {Manifest.FormatDataLine("alert-circle", 24, 24, """<circle cx="12"/>""")}

             """);

    [Test]
    public Task Pack_class() =>
        Verify(Emitter.EmitPackClass(Sample()));

    [Test]
    public Task Path_extensions() =>
        Verify(Emitter.EmitPathExtensions(Sample()));

    [Test]
    public Task Resource_registration() =>
        Verify(Emitter.EmitResourceRegistration(Sample(), ["activity", "alert-circle"]));

    [Test]
    public Task Disk_registration() =>
        Verify(Emitter.EmitDiskRegistration(Sample(), ["activity", "alert-circle"]));

    [Test]
    public Task Used_list() =>
        Verify(Emitter.EmitUsedList(Sample(), ["activity", "alert-circle"]));

    [Test]
    public Task Path_extensions_with_rasters() =>
        Verify(Emitter.EmitPathExtensions(Sample(), ["png", "bmp"]));

    [Test]
    public Task Disk_rasters() =>
        Verify(Emitter.EmitDiskRasters(Sample(), ["activity", "alert-circle"], ["png", "bmp"], [16, 24]));

    // The byte span is laid out back to back in the order given, and each registration names its slice.
    [Test]
    public Task Resource_rasters() =>
        Verify(
            Emitter.EmitResourceRasters(
                "feather",
                "Feather",
                [
                    new("activity", "png", 16, Enumerable.Range(0, 40).Select(_ => (byte)_).ToArray()),
                    new("alert-circle", "bmp", 16, [255, 254, 253])
                ]));

    // An icon whose raster path name would collide with another icon's member gets a suffix, and the
    // names do not depend on which formats are enabled.
    [Test]
    public async Task Raster_path_members_are_deduplicated()
    {
        var manifest = Manifest.Parse(
            "sample",
            """
            prefix=sample
            class=Sample

            box
            box-png-path
            """);

        var members = Emitter.RasterPathMembers(manifest);

        await Assert.That(members[0]).IsEqualTo(("box", "BoxPngPath2", "BoxBmpPath"));
        await Assert.That(members[1]).IsEqualTo(("box-png-path", "BoxPngPathPngPath", "BoxPngPathBmpPath"));
    }

    [Test]
    public Task Member_matching_type_name_is_renamed()
    {
        // The icon "feather" would collide with class "Feather"; it must be renamed.
        var manifest = Manifest.Parse(
            "feather",
            """
            prefix=feather
            class=Feather

            feather
            """);

        return Verify(Emitter.EmitPackClass(manifest));
    }
}
