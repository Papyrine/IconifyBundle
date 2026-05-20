namespace Iconistic.Tests;

public class GeneratorTests
{
    [Test]
    public async Task Baked_icon_is_generated()
    {
        var icon = Icons.Mdi.Home;
        await Assert.That(icon.Width).IsEqualTo(24);
        await Assert.That(icon.Height).IsEqualTo(24);
        await Assert.That(icon.Body).Contains("path");
    }

    [Test]
    public async Task Baked_icon_renders_svg()
    {
        var svg = Icons.Mdi.Account.ToSvg();
        await Assert.That(svg).StartsWith("<svg");
        await Assert.That(svg).Contains("viewBox=\"0 0 24 24\"");
        await Assert.That(svg).Contains("width=\"1em\"");
        await Assert.That(svg).EndsWith("</svg>");
    }

    [Test]
    public async Task Kebab_names_become_pascal_members()
    {
        var icon = Icons.Mdi.AccountOutline;
        await Assert.That(icon.Body).Contains("path");
    }

    [Test]
    public async Task Embedded_pack_resolves_icons()
    {
        var icon = Icons.Lucide.House;
        await Assert.That(icon.Body.Length).IsGreaterThan(0);
        var svg = icon.ToSvg("red", "32");
        await Assert.That(svg).Contains("color:red");
        await Assert.That(svg).Contains("width=\"32\"");
    }

    [Test]
    public async Task Deployed_pack_writes_file_and_loads()
    {
        var path = Path.Combine(Path.GetTempPath(), "Iconistic", "deploy", Icons.Tabler.DeployedFileName);
        await Assert.That(File.Exists(path)).IsTrue();

        // Simulate the runtime InitializeAsync fetch by loading the deployed JSON directly.
        Icons.Tabler.Pack.Load(File.ReadAllText(path));

        await Assert.That(Icons.Tabler.Home.Body.Length).IsGreaterThan(0);
        await Assert.That(Icons.Tabler.Heart.ToSvg()).StartsWith("<svg");
    }
}
