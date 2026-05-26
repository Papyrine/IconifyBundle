namespace IntegrationTests;

// These tests only compile if the source generator emitted the strongly-typed Feather API into this
// (Resource mode) consumer compilation from the referenced IconifyBundle.Feather package.
public class ConsumeTests
{
    [Test]
    public async Task Generated_icon_resolves_svg()
    {
        var icon = Feather.Activity;

        await Assert.That(icon.Name).IsEqualTo("activity");
        await Assert.That(icon.Width).IsEqualTo(24);
        await Assert.That(icon.Svg.Contains("<svg")).IsTrue();
        await Assert.That(icon.Svg.Contains("currentColor")).IsTrue();
        await Assert.That(icon.Svg.Contains("viewBox=\"0 0 24 24\"")).IsTrue();
    }

    [Test]
    public async Task Kebab_names_become_pascal_members() =>
        await Assert.That(Feather.AlertCircle.Name).IsEqualTo("alert-circle");

    [Test]
    public async Task Open_stream_round_trips()
    {
        using var stream = Feather.Activity.OpenStream();
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        await Assert.That(text).IsEqualTo(Feather.Activity.Svg);
    }

    // Dynamic, string-based access resolves icons that were also referenced statically (so materialised)...
    [Test]
    public async Task Dynamic_lookup_resolves_materialised_icon()
    {
        var pack = IconPack.ForPrefix("feather");

        await Assert.That(pack["activity"].Name).IsEqualTo("activity");
        await Assert.That(pack.Contains("activity")).IsTrue();
    }

    [Test]
    public async Task IconifyJson_serialise_strongly_typed()
    {
        #region IconifyJsonSerialise
        // The strongly-typed members from any IconifyBundle.<Pack> (e.g. Feather.Box,
        // AntDesign.HomeOutlined) are the icons - just pass them in. Each Icon carries its pack
        // prefix, so the prefix is derived from the icons - no need to pass it.

        // As a JSON string...
        var json = IconifyJson.Serialize(Feather.Box, Feather.Database);

        // ...or as a stream (handy for feeding into a consumer that takes iconify JSON, e.g.
        // Naiad's IconPack.Load).
        using var stream = IconifyJson.OpenReadStream(Feather.Box, Feather.Database);

        // ...or write directly to a file (sync/async).
        IconifyJson.WriteToFile("sample.json", [Feather.Box, Feather.Database]);
        #endregion

        await Assert.That(json).Contains("\"prefix\":\"feather\"");
        await Assert.That(json).Contains("\"box\"");
        await Assert.That(json).Contains("\"database\"");
        await Assert.That(stream.Length).IsGreaterThan(0);
        await Assert.That(File.Exists("sample.json")).IsTrue();
        File.Delete("sample.json");
    }

    // ...but throws for an icon that was never referenced statically (and so was tree-shaken away).
    [Test]
    public async Task Dynamic_lookup_throws_for_unmaterialised_icon()
    {
        var pack = IconPack.ForPrefix("feather");
        await Assert.That(pack.Contains("zap")).IsFalse();

        var threw = false;
        try
        {
            _ = pack["zap"];
        }
        catch (KeyNotFoundException)
        {
            threw = true;
        }

        await Assert.That(threw).IsTrue();
    }
}
