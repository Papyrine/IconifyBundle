namespace Iconistic.Web.Tests;

[TestFixture]
public class IconTests : BunitContext
{
    [Test]
    public void Renders_inline_svg()
    {
        var component = Render<Icon>(parameters => parameters
            .Add(_ => _.Value, Icons.Mdi.Home));

        var svg = component.Find("svg");
        That(svg, Is.Not.Null);
        That(svg.GetAttribute("viewBox"), Is.EqualTo("0 0 24 24"));
        That(component.Markup, Does.Contain("<path"));
    }

    [Test]
    public void Defaults_to_one_em()
    {
        var component = Render<Icon>(parameters => parameters
            .Add(_ => _.Value, Icons.Mdi.Account));

        var svg = component.Find("svg");
        That(svg.GetAttribute("width"), Is.EqualTo("1em"));
        That(svg.GetAttribute("height"), Is.EqualTo("1em"));
    }

    [Test]
    public void Applies_color_and_size()
    {
        var component = Render<Icon>(parameters => parameters
            .Add(_ => _.Value, Icons.Mdi.Bell)
            .Add(_ => _.Color, "red")
            .Add(_ => _.Size, "32"));

        var svg = component.Find("svg");
        That(svg.GetAttribute("width"), Is.EqualTo("32"));
        That(svg.GetAttribute("height"), Is.EqualTo("32"));
        That(svg.GetAttribute("style"), Does.Contain("color:red"));
    }

    [Test]
    public void Applies_class()
    {
        var component = Render<Icon>(parameters => parameters
            .Add(_ => _.Value, Icons.Lucide.House)
            .Add(_ => _.Class, "nav-icon"));

        var svg = component.Find("svg");
        That(svg.GetAttribute("class"), Is.EqualTo("nav-icon"));
    }

    [Test]
    public Task Markup_snapshot()
    {
        var component = Render<Icon>(parameters => parameters
            .Add(_ => _.Value, Icons.Mdi.Cog)
            .Add(_ => _.Size, "24"));

        return Verify(component.Markup, "html");
    }
}
