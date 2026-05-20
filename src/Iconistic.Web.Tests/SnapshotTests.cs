using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Iconistic.Web.Tests;

/// <summary>
/// End-to-end visual snapshot tests of the published Blazor app, served from the publish output.
/// Tagged "Playwright" so they can be excluded where browsers are not installed
/// (run <c>playwright install</c> first; baselines are accepted on first run).
/// </summary>
[TestFixture]
[Category("Playwright")]
public class SnapshotTests
{
    static WebApplication? app;
    static int port;
    static IPlaywright? playwright;
    static IBrowser? browser;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        port = GetAvailablePort();

        var testAssemblyDirectory = Path.GetDirectoryName(typeof(SnapshotTests).Assembly.Location)!;
        var wwwroot = Path.Combine(testAssemblyDirectory, "..", "blazor-publish", "wwwroot");

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{port}");
        builder.Logging.ClearProviders();
        app = builder.Build();

        var fileProvider = new PhysicalFileProvider(Path.GetFullPath(wwwroot));
        var contentTypes = new FileExtensionContentTypeProvider
        {
            Mappings = { [".wasm"] = "application/wasm" }
        };

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            ContentTypeProvider = contentTypes,
            ServeUnknownFileTypes = true
        });
        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });

        await app.StartAsync();

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (browser is not null)
        {
            await browser.CloseAsync();
        }

        playwright?.Dispose();

        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Test]
    public async Task HomePage()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync($"http://localhost:{port}/");
        await page.WaitForSelectorAsync(".gallery svg");
        await Verify(page);
    }

    static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint) listener.LocalEndpoint).Port;
    }
}
