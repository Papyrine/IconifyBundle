public class IconisticGeneratorTests
{
    const string FeatherManifest =
        """
        prefix=feather
        class=Feather
        marker=IconisticPacks.FeatherPack

        activity
        alert-circle
        1password
        """;

    [Test]
    public async Task No_manifest_generates_nothing()
    {
        var result = GeneratorRunner.Run(manifest: null);
        await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Resource_mode_generates_compiling_api()
    {
        var result = GeneratorRunner.Run(FeatherManifest);

        await Assert.That(result.CompileErrors.Length).IsEqualTo(0);
        var source = result.Single()!;
        await Assert.That(source.Contains("public static partial class Feather")).IsTrue();
        await Assert.That(source.Contains("Icon Activity => pack[\"activity\"];")).IsTrue();
        await Assert.That(source.Contains("Icon AlertCircle => pack[\"alert-circle\"];")).IsTrue();
        await Assert.That(source.Contains("Icon _1password => pack[\"1password\"];")).IsTrue();
        await Assert.That(source.Contains("IconisticMode.Resource")).IsTrue();
    }

    [Test]
    public async Task Disk_mode_generates_path_members_that_compile()
    {
        var result = GeneratorRunner.Run(FeatherManifest, diskMode: true);

        await Assert.That(result.CompileErrors.Length).IsEqualTo(0);
        var source = result.Single()!;
        await Assert.That(source.Contains("string ActivityPath => pack.PathOf(\"activity\");")).IsTrue();
        await Assert.That(source.Contains("IconisticMode.Disk")).IsTrue();
    }
}
