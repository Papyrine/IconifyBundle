public class IconifyBundleGeneratorTests
{
    // The shipped pack data file form: header + "name\twidth\theight\tbody" lines.
    const string featherData =
        """
        prefix=feather
        class=Feather

        activity	24	24	<path d="M12 2v20"/>
        alert-circle	24	24	<circle cx="12"/>
        1password	24	24	<rect width="24"/>

        """;

    // A stub pack class (as the compiled IconifyBundle.Feather assembly would expose) plus a consumer that
    // uses only Feather.Activity - so only that icon should be materialised.
    const string usage =
        """
        namespace IconifyBundle
        {
            [IconifyPack("feather")]
            public static class Feather
            {
                public static Icon Activity => default;
                public static Icon AlertCircle => default;
            }
        }

        public class Use
        {
            public static IconifyBundle.Icon A = IconifyBundle.Feather.Activity;
        }
        """;

    [Test]
    public Task No_data() =>
        Verify(GeneratorRunner.Run(data: null, disk: false));

    [Test]
    public Task Resource_mode_with_no_usage_emits_nothing() =>
        Verify(GeneratorRunner.Run(featherData, disk: false));

    [Test]
    public Task Resource_mode_registers_only_used_icons() =>
        Verify(GeneratorRunner.Run(featherData, disk: false, source: usage));

    [Test]
    public Task Disk_mode_emits_path_members_and_registers_only_used_icons() =>
        Verify(GeneratorRunner.Run(featherData, disk: true, source: usage));

    // .razor markup compiles via the Razor source generator, whose output this generator never sees, so
    // pack accesses there are found by scanning the markup text. Covers the unqualified form (the common
    // case, after a `@using IconifyBundle`) and the fully-qualified chain.
    const string razorUsage =
        """
        @namespace Demo
        <Iconify Value="Feather.Activity" />
        <Iconify Value="IconifyBundle.Feather.AlertCircle" />
        """;

    [Test]
    public Task Resource_mode_detects_razor_usage() =>
        Verify(GeneratorRunner.Run(featherData, disk: false, razor: razorUsage));

    // A real (visible) icon body, so the raster snapshots hold something.
    static readonly string rasterData =
        $"""
         prefix=feather
         class=Feather

         {Manifest.FormatDataLine("activity", 24, 24, RasterTests.FeatherActivity)}
         {Manifest.FormatDataLine("alert-circle", 24, 24, "<circle cx=\"12\" cy=\"12\" r=\"10\"/>")}

         """;

    // Rasters are rendered into the assembly: a module initializer registering each (icon, format, size)
    // slice of one constant byte span.
    [Test]
    public Task Resource_mode_with_rasters_embeds_the_used_icons() =>
        Verify(GeneratorRunner.Run(rasterData, disk: false, source: usage, rasterFormats: "png,bmp", rasterSizes: "8", rasterColor: "336699"));

    // Disk mode: the build task writes the files; the generator registers them and emits the per-format
    // path members.
    [Test]
    public Task Disk_mode_with_rasters_emits_raster_path_members_and_registrations() =>
        Verify(GeneratorRunner.Run(rasterData, disk: true, source: usage, rasterFormats: "png,bmp", rasterSizes: "16,24"));

    // A raster path member (Feather.AlertCirclePngPath(16)) is a use of its icon.
    [Test]
    public async Task Raster_path_member_counts_as_a_use()
    {
        const string source =
            """
            namespace IconifyBundle
            {
                [IconifyPack("feather")]
                public static class Feather;
            }

            public class Use
            {
                public static object A = IconifyBundle.Feather.AlertCirclePngPath(16);
            }
            """;

        var result = GeneratorRunner.Run(rasterData, disk: true, source: source, rasterSizes: "16");
        var registration = result.Results.Single().GeneratedSources.Single(_ => _.HintName == "Feather.Rasters.g.cs");

        await Assert.That(registration.SourceText.ToString()).Contains("""RegisterRasterFile("feather", "alert-circle", "png", 16)""");
    }

    [Test]
    public Task Invalid_raster_properties_report_IB0001() =>
        Verify(GeneratorRunner.Run(rasterData, disk: false, source: usage, rasterFormats: "gif", rasterSizes: "16"));

    [Test]
    public Task Unsupported_svg_features_report_IB0002()
    {
        var data =
            $"""
             prefix=feather
             class=Feather

             {Manifest.FormatDataLine("activity", 24, 24, "<path stroke=\"currentColor\" stroke-dasharray=\"2\" d=\"M0 12h24\"/>")}

             """;

        return Verify(GeneratorRunner.Run(data, disk: false, source: usage, rasterSizes: "4"));
    }
}
