# IconifyBundle

Strongly-typed [Iconify](https://iconify.design/) icons for .NET, with optional on-disk extraction
and Blazor helpers.


## How it works

* **`IconifyBundle`** — the core runtime (`Icon`, `IconPack`, `SvgBuilder`) plus a small source generator
  used only for on-disk extraction (see below).
* **`IconifyBundle.<Pack>`** — one NuGet per Iconify pack (e.g. `IconifyBundle.Feather`). Each pack ships a
  precompiled, strongly-typed class (e.g. `Feather`) with a member per icon (e.g. `Feather.Activity`),
  so a **single reference** to the pack suffices - it pulls the `IconifyBundle` runtime in
  transitively. These packages are produced on demand by the `PackBuilder` test, which downloads each
  pack from the Iconify data and packs it; they are *not* committed to source control.


## Delivery

By default, icon data is loaded from a resource embedded in the pack assembly, so the generated API
exposes string/stream access with no files on disk (`Feather.Activity.Svg`,
`Feather.Activity.OpenStream()`).

To also copy the pack's `.svg` files into the build output (e.g. to serve them as static assets),
set the `IconifyBundleExtractDisk` MSBuild property. The pack's shipped SVGs are then copied to the output
directory and the generated API additionally exposes file paths (`Feather.ActivityPath`):

```xml
<PropertyGroup>
  <IconifyBundleExtractDisk>true</IconifyBundleExtractDisk>
</PropertyGroup>
```

The strongly-typed `Feather.ActivityPath` members are emitted as static extension properties by the
generator, so for those a project also needs a direct `IconifyBundle` reference (analyzers don't flow
transitively) and C# 14. `Feather.PathOf("activity")` is always available without either. The SVG
files are copied to `iconifybundle/<prefix>/` under the output directory.


## Usage

```csharp
Icon icon = Feather.Activity;

// full <svg> document
string svg = icon.Svg;
using Stream stream = icon.OpenStream();
```

The lower-level runtime API (the same `Icon` type the generated members return):

<!-- snippet: RuntimeUsage -->
<a id='snippet-RuntimeUsage'></a>
```cs
// An Icon carries the inner SVG body and intrinsic size.
var icon = new Icon(
    "activity",
    "<path stroke=\"currentColor\" d=\"M12 2v20\"/>",
    24,
    24);

// full <svg> document
var svg = icon.Svg;

// UTF-8 stream of the SVG
using var stream = icon.OpenStream();
```
<sup><a href='/src/Tests/Snippets.cs#L6-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-RuntimeUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Blazor

```razor
@using IconifyBundle

<Iconify Value="Feather.Activity" Width="32" Height="32" class="text-primary" />
```

The `<Iconify>` component renders the icon as an inline `<svg>` (extra attributes like `class`/`style`
are splatted onto it). There is also an `Icon.ToMarkup()` extension returning a `MarkupString`.


## Building locally

```
dotnet build src -c Release
src\PackBuilder\bin\Release\net10.0\PackBuilder.exe   # downloads packs, builds IconifyBundle.<Pack> nugets
dotnet build IntegrationTests -c Release
dotnet build sample -c Release
```

## Notes

https://github.com/iconify/icon-sets/blob/master/collections.md


## Icon

[Pattern](https://thenounproject.com/icon/pattern-42427/) designed by [gira Park](https://thenounproject.com/creator/gila.bag) from [The Noun Project](https://thenounproject.com).


