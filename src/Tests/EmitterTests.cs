public class EmitterTests
{
    static Manifest Sample() =>
        Manifest.Parse(
            "feather",
            """
            prefix=feather
            class=Feather

            activity
            alert-circle
            """);

    [Test]
    public async Task Pack_class_has_icon_members()
    {
        var source = Emitter.EmitPackClass(Sample());

        await Assert.That(source.Contains("public static class Feather")).IsTrue();
        await Assert.That(source.Contains("ForAssembly(typeof(Feather).Assembly, \"feather\")")).IsTrue();
        await Assert.That(source.Contains("public static global::Iconistic.Icon Activity => pack[\"activity\"];")).IsTrue();
        await Assert.That(source.Contains("AlertCircle => pack[\"alert-circle\"];")).IsTrue();
        await Assert.That(source.Contains("public static string PathOf(string name) => pack.PathOf(name);")).IsTrue();
    }

    [Test]
    public async Task Path_extensions_target_the_pack_class()
    {
        var source = Emitter.EmitPathExtensions(Sample());

        await Assert.That(source.Contains("extension(global::Iconistic.Feather)")).IsTrue();
        await Assert.That(source.Contains("public static string ActivityPath => global::Iconistic.Feather.PathOf(\"activity\");")).IsTrue();
    }

    [Test]
    public async Task Member_matching_type_name_is_renamed()
    {
        var manifest = Manifest.Parse(
            "feather",
            """
            prefix=feather
            class=Feather

            feather
            """);

        var source = Emitter.EmitPackClass(manifest);

        // The icon "feather" would collide with class "Feather"; it must be renamed.
        await Assert.That(source.Contains("Icon FeatherIcon => pack[\"feather\"];")).IsTrue();
    }
}
