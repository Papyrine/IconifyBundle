using System.Net.Http;
using Iconistic.Api;

namespace Iconistic.Tests;

// Compiled samples surfaced in the readme via MarkdownSnippets. Public so they are not flagged
// as unused; never executed.
public static class Snippets
{
    public static void Quickstart()
    {
        #region quickstart
        var svg = Icons.Mdi.Home.ToSvg();
        #endregion
        _ = svg;
    }

    public static void Rendering()
    {
        #region render-icon
        var svg = Icons.Mdi.Home.ToSvg();

        var red = Icons.Mdi.Heart.ToSvg("red", "32");

        var custom = Icons.Mdi.Cog.ToSvg(new SvgOptions
        {
            Color = "#43a047",
            Width = "1.5em",
            Rotate = 1,
            HFlip = true,
            CssClass = "spin"
        });
        #endregion
        _ = (svg, red, custom);
    }

    public static async Task ApiUsage()
    {
        #region api-usage
        using var client = new IconifyClient();

        var data = await client.GetIconDataAsync("mdi", ["home", "account"]);
        var home = await client.GetIconAsync("mdi", "home");

        var svg = await client.GetSvgAsync("mdi", "home", new() { Color = "red" });
        var css = await client.GetCssAsync("mdi", ["home", "account"]);

        var hits = await client.SearchAsync("home", limit: 20);
        var all = await client.GetCollectionsAsync();
        var collection = await client.GetCollectionAsync("mdi");
        #endregion
        _ = (data, home, svg, css, hits, all, collection);
    }

    public static async Task DeployedInit(HttpClient httpClient)
    {
        #region deployed-init
        // Blazor Program.cs, after building the host
        await Icons.InitializeAsync(httpClient);
        #endregion
        await Task.CompletedTask;
    }
}
