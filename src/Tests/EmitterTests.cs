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
    public Task Pack_class() =>
        Verify(Emitter.EmitPackClass(Sample()));

    [Test]
    public Task Path_extensions() =>
        Verify(Emitter.EmitPathExtensions(Sample()));

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
