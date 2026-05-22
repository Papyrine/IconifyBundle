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
    public Task No_manifest() =>
        Verify(GeneratorRunner.Run(manifest: null, extractDisk: true));

    [Test]
    public Task Without_extract_disk() =>
        Verify(GeneratorRunner.Run(featherManifest, extractDisk: false));

    [Test]
    public Task With_extract_disk() =>
        Verify(GeneratorRunner.Run(featherManifest, extractDisk: true));
}
