public class IconifyJsonTests
{
    static readonly Icon box = new("sample", "box", """<path d="M1 1h22v22H1z"/>""", 24, 24);
    static readonly Icon database = new("sample", "database", """<ellipse cx="12" cy="5" rx="9" ry="3"/>""", 24, 24);
    static readonly Icon star = new("sample", "star", """<polygon points="12,2 15,9 22,9"/>""", 32, 32);

    [Test]
    public Task Serialize_hoists_common_size() =>
        Verify(IconifyJson.Serialize(box, database), extension: "json");

    [Test]
    public Task Serialize_indented() =>
        Verify(
            IconifyJson.Serialize([box, database], new() { Indented = true }),
            extension: "json");

    [Test]
    public Task Serialize_writes_per_icon_size_when_varying() =>
        Verify(IconifyJson.Serialize(box, star), extension: "json");

    [Test]
    public Task Serialize_writes_per_icon_size_when_hoist_opted_out() =>
        Verify(
            IconifyJson.Serialize([box, database], new() { HoistCommonSize = false }),
            extension: "json");

    [Test]
    public Task Serialize_with_info() =>
        Verify(
            IconifyJson.Serialize([box], new()
            {
                Indented = true,
                Info = new("Sample Pack", "Test Author", "MIT")
            }),
            extension: "json");

    [Test]
    public async Task Serialize_throws_on_missing_prefix()
    {
        var noPrefix = new Icon("", "box", "<path/>", 24, 24);
        await Assert.That(() => IconifyJson.Serialize(noPrefix))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Serialize_throws_on_no_icons() =>
        await Assert.That(() => IconifyJson.Serialize())
            .Throws<ArgumentException>();

    [Test]
    public async Task Serialize_throws_on_duplicate_name() =>
        await Assert.That(() => IconifyJson.Serialize(box, box))
            .Throws<ArgumentException>();

    [Test]
    public async Task Serialize_throws_on_default_icon() =>
        await Assert.That(() => IconifyJson.Serialize(default(Icon)))
            .Throws<ArgumentException>();

    [Test]
    public async Task Serialize_throws_on_mixed_prefixes()
    {
        var other = new Icon("other", "box", """<path d="M1 1h22v22H1z"/>""", 24, 24);
        await Assert.That(() => IconifyJson.Serialize(box, other))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task OpenReadStream_yields_seekable_utf8_stream()
    {
        await using var stream = IconifyJson.OpenReadStream(box);
        await Assert.That(stream.CanSeek).IsTrue();
        await Assert.That(stream.Position).IsEqualTo(0);

        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();
        await Assert.That(text).IsEqualTo(IconifyJson.Serialize(box));
    }

    [Test]
    public async Task WriteToFileAsync_writes_file()
    {
        var path = Path.Combine(Path.GetTempPath(), $"iconify-{Guid.NewGuid():N}.json");
        try
        {
            await IconifyJson.WriteToFileAsync(path, [box, database]);
            var text = await File.ReadAllTextAsync(path);
            await Assert.That(text).IsEqualTo(IconifyJson.Serialize(box, database));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task Parse_reads_iconify_json()
    {
        const string json =
            """
            {
              "prefix": "sample",
              "width": 24,
              "height": 24,
              "icons": {
                "box": {
                  "body": "<rect/>"
                }
              }
            }
            """;
        var pack = IconifyJson.Parse(json);

        await Assert.That(pack.Prefix).IsEqualTo("sample");
        await Assert.That(pack.Icons.Count).IsEqualTo(1);
        await Assert.That(pack.Icons[0].Prefix).IsEqualTo("sample");
        await Assert.That(pack.Icons[0].Name).IsEqualTo("box");
        await Assert.That(pack.Icons[0].Body).IsEqualTo("<rect/>");
        await Assert.That(pack.Icons[0].Width).IsEqualTo(24d);
        await Assert.That(pack.Icons[0].Height).IsEqualTo(24d);
    }

    [Test]
    public async Task Parse_falls_back_to_top_level_dimensions()
    {
        const string json = """
            {"prefix":"sample","width":48,"height":48,"icons":{"a":{"body":"<a/>"},"b":{"body":"<b/>","width":32,"height":16}}}
            """;
        var pack = IconifyJson.Parse(json);

        await Assert.That(pack.Icons[0].Width).IsEqualTo(48d);
        await Assert.That(pack.Icons[0].Height).IsEqualTo(48d);
        await Assert.That(pack.Icons[1].Width).IsEqualTo(32d);
        await Assert.That(pack.Icons[1].Height).IsEqualTo(16d);
    }

    [Test]
    public async Task Parse_reads_info()
    {
        const string json = """
            {"prefix":"sample","info":{"name":"S","author":"A","license":"MIT"},"icons":{"x":{"body":"<x/>","width":16,"height":16}}}
            """;
        var pack = IconifyJson.Parse(json);

        await Assert.That(pack.Info).IsNotNull();
        await Assert.That(pack.Info!.Name).IsEqualTo("S");
        await Assert.That(pack.Info.Author).IsEqualTo("A");
        await Assert.That(pack.Info.License).IsEqualTo("MIT");
    }

    [Test]
    public async Task Parse_throws_on_missing_prefix() =>
        await Assert.That(() => IconifyJson.Parse("""{"icons":{}}"""))
            .Throws<FormatException>();

    [Test]
    public async Task Round_trip_hoisted_pack()
    {
        var json = IconifyJson.Serialize(box, database);
        var pack = IconifyJson.Parse(json);

        await Assert.That(pack.Prefix).IsEqualTo("sample");
        await Assert.That(pack.Icons.Count).IsEqualTo(2);

        var roundTripped = pack.Icons.ToDictionary(i => i.Name);
        await Assert.That(roundTripped["box"].Prefix).IsEqualTo("sample");
        await Assert.That(roundTripped["box"].Body).IsEqualTo(box.Body);
        await Assert.That(roundTripped["box"].Width).IsEqualTo(box.Width);
        await Assert.That(roundTripped["box"].Height).IsEqualTo(box.Height);
        await Assert.That(roundTripped["database"].Body).IsEqualTo(database.Body);
    }

    [Test]
    public async Task Round_trip_varying_sizes()
    {
        var json = IconifyJson.Serialize(box, star);
        var pack = IconifyJson.Parse(json);

        var roundTripped = pack.Icons.ToDictionary(i => i.Name);
        await Assert.That(roundTripped["box"].Width).IsEqualTo(24d);
        await Assert.That(roundTripped["star"].Width).IsEqualTo(32d);
        await Assert.That(roundTripped["star"].Body).IsEqualTo(star.Body);
    }

    [Test]
    public async Task Round_trip_async_to_stream()
    {
        using var buffer = new MemoryStream();
        await IconifyJson.WriteToAsync(buffer, [box, database]);
        buffer.Position = 0;
        var pack = await IconifyJson.ReadAsync(buffer);
        await Assert.That(pack.Icons.Count).IsEqualTo(2);
    }

    static Type LoadFeatherPackClass()
    {
        // Find IconifyBundle.Feather.dll relative to the build output of the Feather pack project. Loading
        // it via reflection avoids a ProjectReference and the version-clash it would cause (Feather pins
        // IconifyBundle 0.1.0 by package reference; we want the in-source build for everything else).
        // The test runner's configuration does not have to match Feather's - probe both, prefer whatever's
        // freshest, so the test works whether Feather was built Release or Debug.
        var solutionDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (solutionDir is not null && !Directory.Exists(Path.Combine(solutionDir.FullName, "packs")))
        {
            solutionDir = solutionDir.Parent;
        }

        var featherDir = Path.Combine(solutionDir!.FullName, "packs", "IconifyBundle.Feather", "bin");
        var candidates = new[] { "Release", "Debug" }
            .Select(configuration => Path.Combine(featherDir, configuration, "net8.0", "IconifyBundle.Feather.dll"))
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new FileNotFoundException(
                $"IconifyBundle.Feather.dll not found under {featherDir}. " +
                "Build the pack first: 'dotnet build packs/IconifyBundle.Feather/IconifyBundle.Feather.csproj'.");
        }

        var assembly = System.Reflection.Assembly.LoadFrom(candidates[0]);
        return assembly.GetType("IconifyBundle.Feather", throwOnError: true)!;
    }

    [Test]
    public async Task ReadPack_loads_full_upstream_feather()
    {
        var feather = LoadFeatherPackClass();
        var pack = IconifyJson.ReadPack(feather);

        await Assert.That(pack.Prefix).IsEqualTo("feather");
        // The Feather set ships 286 icons.
        await Assert.That(pack.Icons.Count).IsEqualTo(286);

        var activity = pack.Icons.First(i => i.Name == "activity");
        await Assert.That(activity.Prefix).IsEqualTo("feather");
        await Assert.That(activity.Width).IsEqualTo(24d);
        await Assert.That(activity.Height).IsEqualTo(24d);
        await Assert.That(activity.Body.Contains("M22 12h-4")).IsTrue();
    }

    [Test]
    public async Task OpenPackStream_yields_parseable_iconify_json()
    {
        var feather = LoadFeatherPackClass();
        await using var stream = IconifyJson.OpenPackStream(feather);

        // ReSharper disable once MethodHasAsyncOverload
        var pack = IconifyJson.Read(stream);
        await Assert.That(pack.Prefix).IsEqualTo("feather");
        await Assert.That(pack.Icons.Count).IsEqualTo(286);
    }

    [Test]
    public async Task ReadPack_throws_on_assembly_without_resource() =>
        // The IconifyBundle core assembly has no embedded pack.
        await Assert.That(() => IconifyJson.ReadPack(typeof(IconPack)))
            .Throws<InvalidOperationException>();
}
