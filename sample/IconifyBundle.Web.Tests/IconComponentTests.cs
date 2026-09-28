namespace IconifyBundle.Web.Tests;

public class IconComponentTests : BunitContext
{
    static readonly Icon Sample = new("feather", "activity", "<path stroke=\"currentColor\" d=\"M1 1\"/>", 24, 24);

    [Test]
    public async Task Renders_inline_svg()
    {
        var cut = Render<Iconify>(_ => _.Add(c => c.Value, Sample));

        var svg = cut.Find("svg");
        await Assert.That(svg.GetAttribute("viewBox")).IsEqualTo("0 0 24 24");
        await Assert.That(svg.InnerHtml).Contains("currentColor");
    }

    [Test]
    public async Task Applies_size_override_and_splatted_attributes()
    {
        var cut = Render<Iconify>(_ => _
            .Add(c => c.Value, Sample)
            .Add(c => c.Width, 48)
            .Add(c => c.Height, 40)
            .AddUnmatched("class", "my-icon"));

        var svg = cut.Find("svg");
        await Assert.That(svg.GetAttribute("width")).IsEqualTo("48");
        await Assert.That(svg.GetAttribute("height")).IsEqualTo("40");
        await Assert.That(svg.GetAttribute("class")).IsEqualTo("my-icon");
    }

    [Test]
    public async Task Renders_nothing_for_default_icon()
    {
        var cut = Render<Iconify>();
        await Assert.That(cut.FindAll("svg")).IsEmpty();
    }

    [Test]
    public async Task ToMarkup_produces_full_svg()
    {
        var markup = Sample.ToMarkup().Value;
        await Assert.That(markup).StartsWith("<svg");
        await Assert.That(markup).Contains("viewBox=\"0 0 24 24\"");
    }
}
