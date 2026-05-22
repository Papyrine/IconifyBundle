namespace Iconistic.Web.Tests;

[TestFixture]
public class GalleryTests : BunitContext
{
    [Test]
    public void Renders_the_feather_grid()
    {
        var cut = Render<Gallery>();

        var svgs = cut.FindAll(".grid svg");
        Assert.That(svgs.Count, Is.GreaterThanOrEqualTo(20));

        // The "feather" icon would collide with the class name and is exposed as FeatherIcon.
        Assert.That(cut.Markup, Does.Contain("feather"));
    }
}
