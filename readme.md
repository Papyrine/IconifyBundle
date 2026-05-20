# Iconistic

Strongly typed [Iconify](https://iconify.design/) icons for .NET.

Declare the wanted icon packs with an assembly attribute. A source generator downloads them at
build time (caching to disk) and emits a strongly typed API — `Icons.Mdi.Home`, `Icons.Lucide.House`
— with no magic strings. Render them anywhere, or drop the `<Icon>` component into a Blazor app.

Declare packs with `[assembly: IconPack("mdi", "home", "account", "cog")]`, then use the
generated members:

<!-- snippet: quickstart -->
<a id='snippet-quickstart'></a>
```cs
var svg = Icons.Mdi.Home.ToSvg();
```
<sup><a href='/src/Tests/Snippets.cs#L12-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-quickstart' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Packages

| Package            | What it is                                                        |
|--------------------|-------------------------------------------------------------------|
| `Iconistic`        | Runtime types **and** the source generator (one install).         |
| `Iconistic.Api`    | A .NET wrapper over the [Iconify API](https://iconify.design/docs/api). |
| `Iconistic.Blazor` | An `<Icon>` Blazor component that renders icons inline as SVG.     |


## How it works

1. Packs are declared with `[assembly: IconPack(...)]`.
2. The source generator reads those attributes and, for each pack, downloads the icon data from
   `https://api.iconify.design`, caching the response under `%LOCALAPPDATA%\Iconistic\cache` so the
   network is only hit on a cache miss.
3. It emits a `static partial class Icons` with a nested class per pack and a member per icon.
   Kebab-case names become PascalCase (`account-outline` → `AccountOutline`).

Because downloads are cached, offline and CI builds work once a pack has been fetched. Set
`IconisticOffline=true` to forbid network access (a cache miss then produces an `ICON002` warning
rather than a build hang).


## Declaring icons

<!-- snippet: declare-icons -->
<a id='snippet-declare-icons'></a>
```cs
// Baked into the generated C# (the default): zero runtime files, trimmer friendly.
[assembly: IconPack("mdi", "home", "account", "account-outline", "cog", "heart")]

// Embedded in the assembly as a compact JSON blob, parsed lazily at runtime.
[assembly: IconPack("lucide", "house", "settings", Storage = IconStorage.EmbeddedResource)]

// Written to wwwroot/iconistic and fetched at runtime (great for Blazor).
[assembly: IconPack("tabler", "home", "heart", Storage = IconStorage.DeployedFile)]
```
<sup><a href='/src/Tests/IconPacks.cs#L1-L10' title='Snippet source file'>snippet source</a> | <a href='#snippet-declare-icons' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `prefix` is an Iconify collection (`mdi`, `lucide`, `material-symbols`, `tabler`, …); the
remaining arguments are icon names from that collection.


### Storage modes

| Mode                            | Where the SVG lives                              | Runtime cost                |
|---------------------------------|--------------------------------------------------|-----------------------------|
| `BakedIn` (default)             | String literals in generated C#.                 | None.                       |
| `EmbeddedResource`              | A compact JSON string constant in the assembly.  | Parsed once on first use.   |
| `DeployedFile`                  | `wwwroot/iconistic/{prefix}.json`.               | Fetched once at startup.    |

For `DeployedFile`, call the generated initializer once at startup so the packs are loaded:

<!-- snippet: deployed-init -->
<a id='snippet-deployed-init'></a>
```cs
// Blazor Program.cs, after building the host
await Icons.InitializeAsync(httpClient);
```
<sup><a href='/src/Tests/Snippets.cs#L57-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-deployed-init' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Rendering

`IconisticIcon` is an immutable value with a `ToSvg` method:

<!-- snippet: render-icon -->
<a id='snippet-render-icon'></a>
```cs
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
```
<sup><a href='/src/Tests/Snippets.cs#L20-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-render-icon' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Bodies use `currentColor`, so `Color` is applied via `style="color:…"`. Rotation and flips use the
same transform algorithm as the Iconify renderer.


## Blazor

Reference `Iconistic.Blazor` and use the `<Icon>` component:

```razor
@using Iconistic.Blazor
```

<!-- snippet: blazor-usage -->
<a id='snippet-blazor-usage'></a>
```razor
<Icon Value="Icons.Mdi.Home" />
<Icon Value="Icons.Mdi.Heart" Color="#e53935" Size="32" />
<Icon Value="Icons.Mdi.Cog" Rotate="1" Class="spin" />
```
<sup><a href='/src/Iconistic.Web/Pages/Home.razor#L10-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-blazor-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See `src/Iconistic.Web` for a complete Blazor WebAssembly sample.


## Iconify API wrapper

`Iconistic.Api.IconifyClient` wraps the public Iconify API:

<!-- snippet: api-usage -->
<a id='snippet-api-usage'></a>
```cs
using var client = new IconifyClient();

var data = await client.GetIconDataAsync("mdi", ["home", "account"]);
var home = await client.GetIconAsync("mdi", "home");

var svg = await client.GetSvgAsync("mdi", "home", new() { Color = "red" });
var css = await client.GetCssAsync("mdi", ["home", "account"]);

var hits = await client.SearchAsync("home", limit: 20);
var all = await client.GetCollectionsAsync();
var collection = await client.GetCollectionAsync("mdi");
```
<sup><a href='/src/Tests/Snippets.cs#L39-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-api-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## MSBuild configuration

These properties are exposed to the generator (set them in the consuming project):

| Property                     | Default                                  | Purpose                                  |
|------------------------------|------------------------------------------|------------------------------------------|
| `IconisticOffline`           | `false`                                  | Forbid network access (cache only).      |
| `IconisticCacheDirectory`    | `%LOCALAPPDATA%\Iconistic\cache`         | Where downloaded icon data is cached.    |
| `IconisticDeployDirectory`   | `$(MSBuildProjectDirectory)\wwwroot\iconistic` | Output for `DeployedFile` packs.   |


## Diagnostics

| Id        | Meaning                                                        |
|-----------|---------------------------------------------------------------|
| `ICON001` | A pack download failed (network/parse error).                 |
| `ICON002` | A pack is not cached and `IconisticOffline` is set.           |
| `ICON003` | One or more requested icons were not found in the collection. |
| `ICON004` | A pack declared no icons.                                      |


## Icon licensing

Iconistic only downloads and embeds icon data; the icons themselves are licensed by their authors.
Check each collection's license (via `GetCollectionsAsync`, or on iconify.design) before shipping.
