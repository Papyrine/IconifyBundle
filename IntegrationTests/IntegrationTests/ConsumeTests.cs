using Iconistic.Api;

namespace IntegrationTests;

public class ConsumeTests
{
    [Test]
    public async Task Generated_api_from_packed_generator()
    {
        var svg = Icons.Mdi.Home.ToSvg();
        await Assert.That(svg).StartsWith("<svg");
        await Assert.That(svg).Contains("viewBox=\"0 0 24 24\"");
    }

    [Test]
    public async Task Api_wrapper_from_package()
    {
        using var client = new IconifyClient();
        var icon = await client.GetIconAsync("mdi", "cog");
        await Assert.That(icon).IsNotNull();
        await Assert.That(icon!.Value.Body.Length).IsGreaterThan(0);
    }
}
