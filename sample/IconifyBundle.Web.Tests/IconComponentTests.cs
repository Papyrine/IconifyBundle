namespace IconifyBundle.Web.Tests;

[TestFixture]
public class IconComponentTests : BunitContext
{
    static readonly Icon Sample = new("feather", "activity", "<path stroke=\"currentColor\" d=\"M1 1\"/>", 24, 24);

    [Test]
    public void Renders_inline_svg()
    {
        var cut = Render<Iconify>(_ => _.Add(c => c.Value, Sample));

        var svg = cut.Find("svg");
        Assert.That(svg.GetAttribute("viewBox"), Is.EqualTo("0 0 24 24"));
        Assert.That(svg.InnerHtml, Does.Contain("currentColor"));
    }

    [Test]
    public void Applies_size_override_and_splatted_attributes()
    {
        var cut = Render<Iconify>(_ => _
            .Add(c => c.Value, Sample)
            .Add(c => c.Width, 48)
            .Add(c => c.Height, 40)
            .AddUnmatched("class", "my-icon"));

        var svg = cut.Find("svg");
        Assert.That(svg.GetAttribute("width"), Is.EqualTo("48"));
        Assert.That(svg.GetAttribute("height"), Is.EqualTo("40"));
        Assert.That(svg.GetAttribute("class"), Is.EqualTo("my-icon"));
    }

    [Test]
    public void Renders_nothing_for_default_icon()
    {
        var cut = Render<Iconify>();
        Assert.That(cut.FindAll("svg"), Is.Empty);
    }

    [Test]
    public void ToMarkup_produces_full_svg()
    {
        var markup = Sample.ToMarkup().Value;
        Assert.That(markup, Does.StartWith("<svg"));
        Assert.That(markup, Does.Contain("viewBox=\"0 0 24 24\""));
    }
}
