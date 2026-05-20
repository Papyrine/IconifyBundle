using System.Net;
using System.Net.Http;
using Iconistic.Api;

namespace Iconistic.Tests;

public class ApiTests
{
    const string IconDataJson =
        """
        {"prefix":"mdi","width":24,"height":24,"icons":{"home":{"body":"<path d=\"M1\"/>"}},"aliases":{"home-alias":{"parent":"home","rotate":1}}}
        """;

    const string SearchJson =
        """
        {"icons":["mdi:home","mdi:home-outline"],"total":2,"limit":32,"start":0}
        """;

    [Test]
    public async Task Get_icon_data_resolves_icons_and_misses()
    {
        var handler = new StubHandler { Response = IconDataJson };
        using var client = new IconifyClient(new HttpClient(handler));

        var data = await client.GetIconDataAsync("mdi", ["home", "missing"]);

        await Assert.That(data.Icons.ContainsKey("home")).IsTrue();
        await Assert.That(data.Icons["home"].Body).Contains("path");
        await Assert.That(data.NotFound).Contains("missing");
        await Assert.That(handler.LastUrl).Contains("/mdi.json?icons=home,missing");
    }

    [Test]
    public async Task Get_icon_data_follows_alias_with_rotation()
    {
        var handler = new StubHandler { Response = IconDataJson };
        using var client = new IconifyClient(new HttpClient(handler));

        var data = await client.GetIconDataAsync("mdi", ["home-alias"]);

        await Assert.That(data.Icons["home-alias"].Rotate).IsEqualTo(1);
    }

    [Test]
    public async Task Svg_request_builds_query()
    {
        var handler = new StubHandler { Response = "<svg/>" };
        using var client = new IconifyClient(new HttpClient(handler));

        await client.GetSvgAsync("mdi", "home", new()
        {
            Color = "red",
            Width = "24",
            FlipHorizontal = true,
            Rotate = 1
        });

        await Assert.That(handler.LastUrl).Contains("/mdi/home.svg?");
        await Assert.That(handler.LastUrl).Contains("color=red");
        await Assert.That(handler.LastUrl).Contains("width=24");
        await Assert.That(handler.LastUrl).Contains("flip=horizontal");
        await Assert.That(handler.LastUrl).Contains("rotate=90deg");
    }

    [Test]
    public async Task Search_parses_response()
    {
        var handler = new StubHandler { Response = SearchJson };
        using var client = new IconifyClient(new HttpClient(handler));

        var result = await client.SearchAsync("home", limit: 32);

        await Assert.That(result.Total).IsEqualTo(2);
        await Assert.That(result.Icons).Contains("mdi:home");
        await Assert.That(handler.LastUrl).Contains("/search?query=home&limit=32");
    }

    sealed class StubHandler : HttpMessageHandler
    {
        public string Response { get; set; } = "{}";
        public string? LastUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastUrl = request.RequestUri!.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Response)
            });
        }
    }
}
