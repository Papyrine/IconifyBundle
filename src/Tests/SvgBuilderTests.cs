public class SvgBuilderTests
{
    static readonly Icon Sample = new("activity", "<path d=\"M1 1\"/>", 24, 24);

    [Test]
    public async Task Build_default_uses_intrinsic_size() =>
        await Assert.That(SvgBuilder.Build(Sample))
            .IsEqualTo("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24\" height=\"24\" viewBox=\"0 0 24 24\"><path d=\"M1 1\"/></svg>");

    [Test]
    public async Task Build_with_size_and_class()
    {
        var svg = SvgBuilder.Build(Sample, 48, 32, "icon");
        await Assert.That(svg.Contains("width=\"48\"")).IsTrue();
        await Assert.That(svg.Contains("height=\"32\"")).IsTrue();
        await Assert.That(svg.Contains("viewBox=\"0 0 24 24\"")).IsTrue();
        await Assert.That(svg.Contains("class=\"icon\"")).IsTrue();
    }

    [Test]
    public async Task Icon_svg_matches_builder() =>
        await Assert.That(Sample.Svg).IsEqualTo(SvgBuilder.Build(Sample));

    [Test]
    public async Task Default_icon_renders_nothing()
    {
        await Assert.That(default(Icon).HasBody).IsFalse();
        await Assert.That(Sample.HasBody).IsTrue();
        await Assert.That(SvgBuilder.Build(default)).IsEqualTo("");
        await Assert.That(default(Icon).Svg).IsEqualTo("");
    }

    [Test]
    public async Task Icon_stream_round_trips()
    {
        using var stream = Sample.OpenStream();
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();
        await Assert.That(text).IsEqualTo(Sample.Svg);
    }
}
