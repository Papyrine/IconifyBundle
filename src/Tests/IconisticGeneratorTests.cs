public class IconisticGeneratorTests
{
    const string featherManifest =
        """
        prefix=feather
        class=Feather

        activity
        alert-circle
        1password
        """;

    [Test]
    public async Task No_manifest_generates_nothing()
    {
        var result = GeneratorRunner.Run(manifest: null, diskMode: true);
        await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Without_extract_disk_generates_nothing()
    {
        // The icon API is compiled into the pack assembly; the generator only adds path members.
        var result = GeneratorRunner.Run(featherManifest, diskMode: false);
        await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Extract_disk_generates_compiling_path_extensions()
    {
        var result = GeneratorRunner.Run(featherManifest, diskMode: true);

        await Assert.That(result.CompileErrors.Length).IsEqualTo(0);
        var source = result.Single()!;
        await Assert.That(source.Contains("extension(global::Iconistic.Feather)")).IsTrue();
        await Assert.That(source.Contains("string ActivityPath => global::Iconistic.Feather.PathOf(\"activity\");")).IsTrue();
        await Assert.That(source.Contains("string _1passwordPath => global::Iconistic.Feather.PathOf(\"1password\");")).IsTrue();
    }
}
