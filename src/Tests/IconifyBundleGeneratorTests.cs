public class IconifyBundleGeneratorTests
{
    // The shipped pack data file form: header + "name\twidth\theight\tbody" lines.
    const string featherData =
        "prefix=feather\n" +
        "class=Feather\n" +
        "\n" +
        "activity\t24\t24\t<path d=\"M12 2v20\"/>\n" +
        "alert-circle\t24\t24\t<circle cx=\"12\"/>\n" +
        "1password\t24\t24\t<rect width=\"24\"/>\n";

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
}
