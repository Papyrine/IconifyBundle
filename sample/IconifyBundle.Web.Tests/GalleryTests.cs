namespace IconifyBundle.Web.Tests;

public class GalleryTests : BunitContext
{
    [Test]
    public async Task Renders_the_feather_grid()
    {
        var cut = Render<Gallery>();

        var svgs = cut.FindAll(".grid svg");
        await Assert.That(svgs.Count).IsGreaterThanOrEqualTo(20);

        // The "feather" icon would collide with the class name and is exposed as FeatherIcon.
        await Assert.That(cut.Markup).Contains("feather");
    }
}
