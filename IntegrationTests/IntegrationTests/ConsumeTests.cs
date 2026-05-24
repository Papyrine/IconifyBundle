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
