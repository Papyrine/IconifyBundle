public class EmitterTests
{
    static Manifest Sample() =>
        Manifest.Parse(
            "feather",
            """
            prefix=feather
            class=Feather
            marker=IconisticPacks.FeatherPack

            activity
            alert-circle
            """);

    [Test]
    public async Task Emits_stream_surface_by_default()
    {
        var source = Emitter.Emit(Sample(), diskMode: false);

        await Assert.That(source.Contains("public static partial class Feather")).IsTrue();
        await Assert.That(source.Contains("ForAssembly(typeof(global::IconisticPacks.FeatherPack).Assembly, \"feather\")")).IsTrue();
        await Assert.That(source.Contains("public static global::Iconistic.Icon Activity => pack[\"activity\"];")).IsTrue();
        await Assert.That(source.Contains("AlertCircle => pack[\"alert-circle\"];")).IsTrue();
        await Assert.That(source.Contains("PathOf")).IsFalse();
    }

    [Test]
    public async Task Extract_disk_also_emits_path_surface()
    {
        var source = Emitter.Emit(Sample(), diskMode: true);

        await Assert.That(source.Contains("public static string ActivityPath => pack.PathOf(\"activity\");")).IsTrue();
    }

    [Test]
    public async Task Member_matching_type_name_is_renamed()
    {
        var manifest = Manifest.Parse(
            "feather",
            """
            prefix=feather
            class=Feather
            marker=IconisticPacks.FeatherPack

            feather
            """);

        var source = Emitter.Emit(manifest, diskMode: false);

        // The icon "feather" would collide with class "Feather"; it must be renamed.
        await Assert.That(source.Contains("Icon FeatherIcon => pack[\"feather\"];")).IsTrue();
    }
}
