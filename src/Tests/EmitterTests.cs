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
