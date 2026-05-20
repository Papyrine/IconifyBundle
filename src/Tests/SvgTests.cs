namespace Iconistic.Tests;

public class SvgTests
{
    static readonly IconisticIcon Square = new("<path d=\"M0 0\"/>", 24, 24);

    [Test]
    public async Task Defaults_to_one_em_and_viewbox()
    {
        var svg = Square.ToSvg();
        await Assert.That(svg).Contains("width=\"1em\"");
        await Assert.That(svg).Contains("height=\"1em\"");
        await Assert.That(svg).Contains("viewBox=\"0 0 24 24\"");
        await Assert.That(svg).Contains("<path d=\"M0 0\"/>");
    }

    [Test]
    public async Task Color_size_class_and_style()
    {
        var svg = Square.ToSvg(new()
        {
            Color = "#f00",
            Width = "32",
            Height = "16",
            CssClass = "x",
            Style = "opacity:.5"
        });

        await Assert.That(svg).Contains("width=\"32\"");
        await Assert.That(svg).Contains("height=\"16\"");
        await Assert.That(svg).Contains("class=\"x\"");
        await Assert.That(svg).Contains("style=\"opacity:.5;color:#f00\"");
    }

    [Test]
    public async Task Rotate_wraps_body_in_group()
    {
        var svg = Square.ToSvg(new() { Rotate = 1 });
        await Assert.That(svg).Contains("<g transform=\"rotate(90 12 12)\">");
        await Assert.That(svg).EndsWith("</g></svg>");
    }

    [Test]
    public async Task Horizontal_flip_translates_and_scales()
    {
        var svg = Square.ToSvg(new() { HFlip = true });
        await Assert.That(svg).Contains("<g transform=\"translate(24 0) scale(-1 1)\">");
    }

    [Test]
    public async Task Baked_and_option_transforms_combine()
    {
        var rotated = Square with { Rotate = 1 };
        var svg = rotated.ToSvg(new() { Rotate = 1 });
        // 90 + 90 == 180
        await Assert.That(svg).Contains("rotate(180 12 12)");
    }

    [Test]
    public async Task Odd_rotation_swaps_non_square_viewbox()
    {
        var wide = new IconisticIcon("<path/>", 32, 16);
        var svg = wide.ToSvg(new() { Rotate = 1 });
        await Assert.That(svg).Contains("viewBox=\"0 0 16 32\"");
    }
}
