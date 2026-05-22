# Iconistic

Strongly-typed [Iconify](https://iconify.design/) icons for .NET, with a Roslyn source generator,
two delivery modes (embedded **Resource** or on-disk **Disk**), and Blazor helpers.

## How it works

* **`Iconistic`** — the core runtime plus the source generator (shipped together).
* **`Iconistic.<Pack>`** — one NuGet per Iconify pack (e.g. `Iconistic.Feather`). These packages are
  produced on demand by the `PackBuilder` test, which downloads each pack from the Iconify data and
  packs it. They are *not* committed to source control.
* Reference `Iconistic` plus any `Iconistic.<Pack>` packages. The generator emits a strongly-typed
  class per pack (e.g. `Feather`) with a member per icon (e.g. `Feather.Activity`).

## Modes

Set via the `IconisticMode` MSBuild property (default `Resource`):

```xml
<PropertyGroup>
  <IconisticMode>Disk</IconisticMode>
</PropertyGroup>
```

* **Resource** (default): pack data is loaded from resources embedded in the pack assembly. The
  generated API exposes string/stream access (`Feather.Activity.Svg`, `Feather.Activity.OpenStream()`).
* **Disk**: pack SVG files are additionally copied to the build output, and the generated API also
  exposes file paths (`Feather.ActivityPath`).

## Usage

```csharp
Icon icon = Feather.Activity;

string svg = icon.Svg;            // full <svg> document
using Stream stream = icon.OpenStream();
```

The lower-level runtime API (the same `Icon` type the generated members return):

<!-- snippet: RuntimeUsage -->
<a id='snippet-RuntimeUsage'></a>
```cs
// An Icon carries the inner SVG body and intrinsic size.
var icon = new Icon("activity", "<path stroke=\"currentColor\" d=\"M12 2v20\"/>", 24, 24);

var svg = icon.Svg;                   // full <svg> document
using var stream = icon.OpenStream(); // UTF-8 stream of the SVG
```
<sup><a href='/src/Tests/Snippets.cs#L8-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-RuntimeUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Blazor

```razor
@using Iconistic

<Iconify Value="Feather.Activity" Width="32" Height="32" class="text-primary" />
```

The `<Iconify>` component renders the icon as an inline `<svg>` (extra attributes like `class`/`style`
are splatted onto it). There is also an `Icon.ToMarkup()` extension returning a `MarkupString`.

## Building locally

```
dotnet build src -c Release
src\PackBuilder\bin\Release\net10.0\PackBuilder.exe   # downloads packs, builds Iconistic.<Pack> nugets
dotnet build IntegrationTests -c Release
dotnet build sample -c Release
```

## License

MIT
