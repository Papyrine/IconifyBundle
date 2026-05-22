public class ManifestParserTests
{
    [Test]
    public async Task Parses_headers_and_names()
    {
        var manifest = Manifest.Parse(
            "ignored",
            """
            prefix=feather
            class=Feather

            activity
            airplay
            alert-circle
            """);

        await Assert.That(manifest.Prefix).IsEqualTo("feather");
        await Assert.That(manifest.ClassName).IsEqualTo("Feather");
        await Assert.That(manifest.IconNames.Count).IsEqualTo(3);
        await Assert.That(manifest.IconNames[0]).IsEqualTo("activity");
        await Assert.That(manifest.IconNames[2]).IsEqualTo("alert-circle");
    }

    [Test]
    public async Task Defaults_class_from_prefix()
    {
        var manifest = Manifest.Parse(
            "simple-icons",
            """
            prefix=simple-icons

            github
            """);

        await Assert.That(manifest.ClassName).IsEqualTo("SimpleIcons");
    }

    [Test]
    public async Task Skips_comments()
    {
        var manifest = Manifest.Parse(
            "feather",
            """
            prefix=feather

            # a comment
            activity
            """);

        await Assert.That(manifest.IconNames.Count).IsEqualTo(1);
        await Assert.That(manifest.IconNames[0]).IsEqualTo("activity");
    }
}
