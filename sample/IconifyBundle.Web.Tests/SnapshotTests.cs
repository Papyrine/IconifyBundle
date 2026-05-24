using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace IconifyBundle.Web.Tests;

// End-to-end: serves the published WASM output and drives a real browser via Playwright.
[TestFixture]
public class SnapshotTests
{
    static WebApplication? app;
    static int port;
    static IPlaywright? playwright;
    static IBrowser? browser;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        port = GetFreePort();
        var testDir = Path.GetDirectoryName(typeof(SnapshotTests).Assembly.Location)!;
        // BlazorPublishDir is bin\<Config>\blazor-publish (one level up from the test's net10.0 output).
        var wwwroot = Path.Combine(testDir, "..", "blazor-publish", "wwwroot");

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{port}");
        builder.Logging.ClearProviders();
        app = builder.Build();

        var provider = new PhysicalFileProvider(wwwroot);
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".wasm"] = "application/wasm";

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = provider,
                ContentTypeProvider = contentTypes,
                ServeUnknownFileTypes = true
            });
        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = provider });

        await app.StartAsync();

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (browser is not null)
        {
            await browser.DisposeAsync();
        }

        playwright?.Dispose();

        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }

    [Test]
    public async Task Gallery_renders_icons_in_browser()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync($"http://localhost:{port}/");
        await page.WaitForSelectorAsync(".grid svg", new() { Timeout = 60000 });

        var count = await page.Locator(".grid svg").CountAsync();
        Assert.That(count, Is.GreaterThanOrEqualTo(20));
    }

    static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var freePort = ((IPEndPoint) listener.LocalEndpoint).Port;
        listener.Stop();
        return freePort;
    }
}
