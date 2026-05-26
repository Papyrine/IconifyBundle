# IconifyBundle

Strongly-typed [Iconify](https://iconify.design/) icons for .NET. Only the referenced icons are bundled - either baked into the consuming assembly (Resource mode) or written to the build output as `.svg` files (Disk mode). Blazor helpers included.


## How it works

* **`IconifyBundle`** — the core runtime (`Icon`, `IconPack`, `SvgBuilder`, `IconRegistry`).
* **`IconifyBundle.<Pack>`** — one NuGet per Iconify pack (e.g. `IconifyBundle.Feather`). Each pack ships a precompiled, strongly-typed class (e.g. `Feather`) with a member per icon (e.g. `Feather.Activity`), the pack's icon data (a build-time file, **not** embedded in the assembly), and the IconifyBundle source generator (as an analyzer). So a **single reference** to the pack suffices - it pulls the `IconifyBundle` runtime in transitively and runs the generator in the consuming project. These packages are produced on demand by the `PackBuilder` test, which downloads each pack from the Iconify data and packs it; they are *not* committed to source control.
* The generator detects which icons are referenced (member accesses like `Feather.Activity`) and **materialises only those** - so the pack assemblies stay tiny and the build only carries the icons in  use.


## Delivery

The two modes are mutually exclusive, selected by the `IconifyBundleMode` MSBuild property.


### Resource mode (default)

The referenced icons are baked into the consuming assembly, so the generated API exposes string/stream
access with no files on disk (`Feather.Activity.Svg`, `Feather.Activity.OpenStream()`). Nothing to
configure - reference a pack and use it.


### Disk mode

The referenced icons are written to the build and publish output as `.svg` files (under `iconifybundle/<prefix>/`, e.g. to serve them as static assets), and the generated API additionally exposes file paths (`Feather.ActivityPath`):

```xml
<PropertyGroup>
  <IconifyBundleMode>Disk</IconifyBundleMode>
</PropertyGroup>
```

`Feather.ActivityPath` (and `Feather.PathOf("activity")`) return the path under the output directory. The strongly-typed `...Path` members require C# 14 (emitted as static extension properties).

> Only icons referenced through the strongly-typed API are materialised. Dynamic, string-based lookups
> (`Feather.PathOf(name)`, the `IconPack` indexer) resolve only icons that were *also* referenced
> statically somewhere; otherwise they throw.
>
> This selection happens at **compile time**, not at trim or publish time. A
> `not materialised` error means the icon was never referenced through a member access
> the generator could see - not that trimming removed it. Materialised icons run their
> registration from a module initializer that the trimmer preserves.


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
// An Icon carries the pack prefix, the icon name, the inner SVG body, and intrinsic size.
var icon = new Icon(
    "feather",
    "activity",
    """<path stroke="currentColor" d="M12 2v20"/>""",
    24,
    24);

// full <svg> document
var svg = icon.Svg;

// UTF-8 stream of the SVG
using var stream = icon.OpenStream();
```
<sup><a href='/src/Tests/Snippets.cs#L6-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-RuntimeUsage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Blazor

```razor
@using IconifyBundle

<Iconify Value="Feather.Activity" Width="32" Height="32" class="text-primary" />
```

The `<Iconify>` component renders the icon as an inline `<svg>` (extra attributes like `class`/`style` are splatted onto it). There is also an `Icon.ToMarkup()` extension returning a `MarkupString`.


## Iconify JSON

The static `IconifyJson` class reads and writes the [iconify JSON format](https://iconify.design/docs/types/iconify-json.html)- the same shape published as `@iconify-json/*` and consumed by tooling like [Mermaid](https://github.com/mermaid-js/mermaid) and [Naiad](https://github.com/Papyrine/Naiad) - so IconifyBundle icons can be handed to any iconify-format consumer and arbitrary iconify JSON can be parsed back into `Icon`s. Three flows are supported.


### Writing iconify JSON

Serialise any set of `Icon`s as iconify-format JSON - to a string, a stream, or a file (sync and async). The pack prefix is taken from the icons (every `Icon` carries its pack `Prefix`), so callers do not pass it separately - and mixing icons from different packs in one call is rejected. Width/height shared by every icon are hoisted to the top level and omitted per-icon (the canonical shape; pass `IconifyJsonOptions { HoistCommonSize = false }` to opt out).

<!-- snippet: IconifyJsonSerialise -->
<a id='snippet-IconifyJsonSerialise'></a>
```cs
// The strongly-typed members from any IconifyBundle.<Pack> (e.g. Feather.Box,
// AntDesign.HomeOutlined) are the icons - just pass them in. Each Icon carries its pack
// prefix, so the prefix is derived from the icons - no need to pass it.

// As a JSON string...
var json = IconifyJson.Serialize(Feather.Box, Feather.Database);

// ...or as a stream (handy for feeding into a consumer that takes iconify JSON, e.g.
// Naiad's IconPack.Load).
using var stream = IconifyJson.OpenReadStream(Feather.Box, Feather.Database);

// ...or write directly to a file (sync/async).
IconifyJson.WriteToFile("sample.json", [Feather.Box, Feather.Database]);
```
<sup><a href='/IntegrationTests/IntegrationTests/ConsumeTests.cs#L46-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-IconifyJsonSerialise' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


Options:

* `Indented` - pretty-print, default off.
* `HoistCommonSize` - default on; factors a shared width/height up to the pack root. Set `false` to keep dimensions on every icon even when they match.
* `Info` - the iconify `info` block: name, author, license.

Equivalent `IconPack` overloads serialise the materialised icons of a runtime pack, e.g. `IconifyJson.Serialize(IconPack.ForPrefix("feather"))`.


### Reading iconify JSON

Parse iconify JSON (from a string, stream, or file) into an `IconifyPack` - the prefix, the icons, and any `info` block:

<!-- snippet: IconifyJsonRead -->
<a id='snippet-IconifyJsonRead'></a>
```cs
// Parse iconify-format JSON back into an IconifyPack (prefix + icons + optional info).
const string source =
    """
    {"prefix":"sample","width":24,"height":24,"icons":{"box":{"body":"<rect/>"}}}
    """;
var pack = IconifyJson.Parse(source);

Console.WriteLine(pack.Prefix);                 // "sample"
Console.WriteLine(pack.Icons.Count);            // 1
foreach (var icon in pack.Icons)
{
    Console.WriteLine($"{icon.Name}: {icon.Body} ({icon.Width}x{icon.Height})");
}
```
<sup><a href='/src/Tests/Snippets.cs#L28-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-IconifyJsonRead' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Per-icon `width`/`height` fall back to the top-level defaults (16×16 if neither is specified, matching the iconify spec).


### Upstream pack passthrough

Every `IconifyBundle.<Pack>` assembly embeds its full upstream pack data as a manifest resource, so the entire `@iconify-json/<pack>` dataset is available at runtime - not only the icons a consumer has materialised. Pass the pack class to `IconifyJson.OpenPackStream` / `ReadPack`:

<!-- snippet: IconifyJsonUpstream -->
<a id='snippet-IconifyJsonUpstream'></a>
```cs
// Open the full upstream pack data embedded in any IconifyBundle.<Pack> assembly. Pass the
// strongly-typed pack class (e.g. typeof(Feather)) - the result is the entire
// @iconify-json/<pack> dataset, not just the icons your project has materialised.
using var stream = IconifyJson.OpenPackStream(packClass);

// Or get the parsed pack back directly.
var pack = IconifyJson.ReadPack(packClass);
Console.WriteLine($"{pack.Prefix}: {pack.Icons.Count} icons");
```
<sup><a href='/src/Tests/Snippets.cs#L47-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-IconifyJsonUpstream' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use this when the whole pack is needed (for example handing the full Feather set to [Naiad](https://github.com/Papyrine/Naiad)'s `IconPack.Load`); for a slim, build-time-checked subset, prefer the write APIs above with the strongly-typed members directly.


## Building locally

```
dotnet build src -c Release
src\PackBuilder\bin\Release\net10.0\PackBuilder.exe   # downloads packs, builds IconifyBundle.<Pack> nugets
dotnet build IntegrationTests -c Release
dotnet build sample -c Release
```


## Notes

https://github.com/iconify/icon-sets/blob/master/collections.md


## NuGet packages

One NuGet per Iconify pack. The list is generated when the packs are built.

> **Note:** some Iconify packs are not published because their license is incompatible with redistribution in a public, commercially-consumable NuGet:<!-- include: packs. path: /src/packs.include.md -->
> - **Non-commercial (CC BY-NC*)**: [cbi](https://icon-sets.iconify.design/cbi/), [ps](https://icon-sets.iconify.design/ps/)
> - **Copyleft (GPL)**: [dashicons](https://icon-sets.iconify.design/dashicons/), [et](https://icon-sets.iconify.design/et/), [gala](https://icon-sets.iconify.design/gala/), [gridicons](https://icon-sets.iconify.design/gridicons/), [icomoon-free](https://icon-sets.iconify.design/icomoon-free/), [wordpress](https://icon-sets.iconify.design/wordpress/)

| Package | Iconify | License | NuGet size | Assembly size |
|---|---|---|--:|--:|
| [IconifyBundle.Academicons](https://www.nuget.org/packages/IconifyBundle.Academicons) | [academicons](https://icon-sets.iconify.design/academicons/) | [OFL](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 141.2KB | 18KB |
| [IconifyBundle.AkarIcons](https://www.nuget.org/packages/IconifyBundle.AkarIcons) | [akar-icons](https://icon-sets.iconify.design/akar-icons/) | [MIT](https://github.com/artcoholic/akar-icons/blob/master/LICENSE) | 76.4KB | 41KB |
| [IconifyBundle.AntDesign](https://www.nuget.org/packages/IconifyBundle.AntDesign) | [ant-design](https://icon-sets.iconify.design/ant-design/) | [MIT](https://github.com/ant-design/ant-design-icons/blob/master/LICENSE) | 186.2KB | 85KB |
| [IconifyBundle.Arcticons](https://www.nuget.org/packages/IconifyBundle.Arcticons) | [arcticons](https://icon-sets.iconify.design/arcticons/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 3.3MB | 1.2MB |
| [IconifyBundle.Basil](https://www.nuget.org/packages/IconifyBundle.Basil) | [basil](https://icon-sets.iconify.design/basil/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 114.2KB | 50.5KB |
| [IconifyBundle.Bi](https://www.nuget.org/packages/IconifyBundle.Bi) | [bi](https://icon-sets.iconify.design/bi/) | [MIT](https://github.com/twbs/icons/blob/main/LICENSE.md) | 287.4KB | 181KB |
| [IconifyBundle.BitcoinIcons](https://www.nuget.org/packages/IconifyBundle.BitcoinIcons) | [bitcoin-icons](https://icon-sets.iconify.design/bitcoin-icons/) | [MIT](https://github.com/BitcoinDesign/Bitcoin-Icons/blob/main/LICENSE-MIT) | 58.7KB | 35.5KB |
| [IconifyBundle.Boxicons](https://www.nuget.org/packages/IconifyBundle.Boxicons) | [boxicons](https://icon-sets.iconify.design/boxicons/) | [MIT](https://github.com/box-icons/boxicons-core/blob/main/LICENSE) | 390.1KB | 362KB |
| [IconifyBundle.Bpmn](https://www.nuget.org/packages/IconifyBundle.Bpmn) | [bpmn](https://icon-sets.iconify.design/bpmn/) | [OFL](https://github.com/bpmn-io/bpmn-font/blob/master/LICENSE) | 103.7KB | 17KB |
| [IconifyBundle.Brandico](https://www.nuget.org/packages/IconifyBundle.Brandico) | [brandico](https://icon-sets.iconify.design/brandico/) | [CC BY SA](https://creativecommons.org/licenses/by-sa/3.0/) | 43.7KB | 8KB |
| [IconifyBundle.Bx](https://www.nuget.org/packages/IconifyBundle.Bx) | [bx](https://icon-sets.iconify.design/bx/) | [MIT](https://github.com/box-icons/boxicons/blob/main/LICENSE) | 249.2KB | 135KB |
| [IconifyBundle.Bxl](https://www.nuget.org/packages/IconifyBundle.Bxl) | [bxl](https://icon-sets.iconify.design/bxl/) | [MIT](https://github.com/box-icons/boxicons-core/blob/main/LICENSE) | 129.8KB | 26KB |
| [IconifyBundle.Bxs](https://www.nuget.org/packages/IconifyBundle.Bxs) | [bxs](https://icon-sets.iconify.design/bxs/) | [MIT](https://github.com/box-icons/boxicons/blob/main/LICENSE) | 89.2KB | 55.5KB |
| [IconifyBundle.Bytesize](https://www.nuget.org/packages/IconifyBundle.Bytesize) | [bytesize](https://icon-sets.iconify.design/bytesize/) | [MIT](https://github.com/danklammer/bytesize-icons/blob/master/LICENSE.md) | 22.8KB | 12KB |
| [IconifyBundle.Carbon](https://www.nuget.org/packages/IconifyBundle.Carbon) | [carbon](https://icon-sets.iconify.design/carbon/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 381.5KB | 241KB |
| [IconifyBundle.Catppuccin](https://www.nuget.org/packages/IconifyBundle.Catppuccin) | [catppuccin](https://icon-sets.iconify.design/catppuccin/) | [MIT](https://github.com/catppuccin/vscode-icons/blob/main/LICENSE) | 91.6KB | 56.5KB |
| [IconifyBundle.Charm](https://www.nuget.org/packages/IconifyBundle.Charm) | [charm](https://icon-sets.iconify.design/charm/) | [MIT](https://github.com/jaynewey/charm-icons/blob/main/LICENSE) | 33.3KB | 24KB |
| [IconifyBundle.Ci](https://www.nuget.org/packages/IconifyBundle.Ci) | [ci](https://icon-sets.iconify.design/ci/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 91.6KB | 62.5KB |
| [IconifyBundle.Cib](https://www.nuget.org/packages/IconifyBundle.Cib) | [cib](https://icon-sets.iconify.design/cib/) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 495.3KB | 64.5KB |
| [IconifyBundle.Cif](https://www.nuget.org/packages/IconifyBundle.Cif) | [cif](https://icon-sets.iconify.design/cif/) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 1MB | 16KB |
| [IconifyBundle.Cil](https://www.nuget.org/packages/IconifyBundle.Cil) | [cil](https://icon-sets.iconify.design/cil/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 116.3KB | 47.5KB |
| [IconifyBundle.CircleFlags](https://www.nuget.org/packages/IconifyBundle.CircleFlags) | [circle-flags](https://icon-sets.iconify.design/circle-flags/) | [MIT](https://github.com/HatScripts/circle-flags/blob/gh-pages/LICENSE) | 137.7KB | 50KB |
| [IconifyBundle.Circum](https://www.nuget.org/packages/IconifyBundle.Circum) | [circum](https://icon-sets.iconify.design/circum/) | [MPL 2.0](https://github.com/Klarr-Agency/Circum-Icons/blob/main/LICENSE) | 73.6KB | 26.5KB |
| [IconifyBundle.Clarity](https://www.nuget.org/packages/IconifyBundle.Clarity) | [clarity](https://icon-sets.iconify.design/clarity/) | [MIT](https://github.com/vmware/clarity-assets/blob/master/LICENSE) | 170.3KB | 110.5KB |
| [IconifyBundle.Codex](https://www.nuget.org/packages/IconifyBundle.Codex) | [codex](https://icon-sets.iconify.design/codex/) | [MIT](https://github.com/codex-team/icons/blob/master/LICENSE) | 24.3KB | 10.5KB |
| [IconifyBundle.Codicon](https://www.nuget.org/packages/IconifyBundle.Codicon) | [codicon](https://icon-sets.iconify.design/codicon/) | [CC BY 4.0](https://github.com/microsoft/vscode-codicons/blob/main/LICENSE) | 130.4KB | 54KB |
| [IconifyBundle.Covid](https://www.nuget.org/packages/IconifyBundle.Covid) | [covid](https://icon-sets.iconify.design/covid/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 44.8KB | 23KB |
| [IconifyBundle.Cryptocurrency](https://www.nuget.org/packages/IconifyBundle.Cryptocurrency) | [cryptocurrency](https://icon-sets.iconify.design/cryptocurrency/) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 263.3KB | 33.5KB |
| [IconifyBundle.CryptocurrencyColor](https://www.nuget.org/packages/IconifyBundle.CryptocurrencyColor) | [cryptocurrency-color](https://icon-sets.iconify.design/cryptocurrency-color/) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 265.2KB | 33.5KB |
| [IconifyBundle.Cuida](https://www.nuget.org/packages/IconifyBundle.Cuida) | [cuida](https://icon-sets.iconify.design/cuida/) | [Apache 2.0](https://github.com/Sysvale/cuida-icons/blob/main/LICENSE) | 63.7KB | 22.5KB |
| [IconifyBundle.Devicon](https://www.nuget.org/packages/IconifyBundle.Devicon) | [devicon](https://icon-sets.iconify.design/devicon/) | [MIT](https://github.com/devicons/devicon/blob/master/LICENSE) | 1.8MB | 88.5KB |
| [IconifyBundle.DeviconPlain](https://www.nuget.org/packages/IconifyBundle.DeviconPlain) | [devicon-plain](https://icon-sets.iconify.design/devicon-plain/) | [MIT](https://github.com/devicons/devicon/blob/master/LICENSE) | 1.1MB | 68KB |
| [IconifyBundle.DinkieIcons](https://www.nuget.org/packages/IconifyBundle.DinkieIcons) | [dinkie-icons](https://icon-sets.iconify.design/dinkie-icons/) | [MIT](https://github.com/atelier-anchor/dinkie-icons/blob/main/LICENSE) | 94.9KB | 120KB |
| [IconifyBundle.DuoIcons](https://www.nuget.org/packages/IconifyBundle.DuoIcons) | [duo-icons](https://icon-sets.iconify.design/duo-icons/) | [MIT](https://github.com/fazdiu/duo-icons/blob/master/LICENSE) | 34.8KB | 11.5KB |
| [IconifyBundle.Ei](https://www.nuget.org/packages/IconifyBundle.Ei) | [ei](https://icon-sets.iconify.design/ei/) | [MIT](https://github.com/evil-icons/evil-icons/blob/master/LICENSE.txt) | 27.9KB | 10KB |
| [IconifyBundle.El](https://www.nuget.org/packages/IconifyBundle.El) | [el](https://icon-sets.iconify.design/el/) | [OFL](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 97.9KB | 26.5KB |
| [IconifyBundle.Emojione](https://www.nuget.org/packages/IconifyBundle.Emojione) | [emojione](https://icon-sets.iconify.design/emojione/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 737.1KB | 187.5KB |
| [IconifyBundle.EmojioneMonotone](https://www.nuget.org/packages/IconifyBundle.EmojioneMonotone) | [emojione-monotone](https://icon-sets.iconify.design/emojione-monotone/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1.1MB | 127.5KB |
| [IconifyBundle.EmojioneV1](https://www.nuget.org/packages/IconifyBundle.EmojioneV1) | [emojione-v1](https://icon-sets.iconify.design/emojione-v1/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 3.7MB | 116.5KB |
| [IconifyBundle.Entypo](https://www.nuget.org/packages/IconifyBundle.Entypo) | [entypo](https://icon-sets.iconify.design/entypo/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 70.7KB | 29.5KB |
| [IconifyBundle.EntypoSocial](https://www.nuget.org/packages/IconifyBundle.EntypoSocial) | [entypo-social](https://icon-sets.iconify.design/entypo-social/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 40.4KB | 11KB |
| [IconifyBundle.EosIcons](https://www.nuget.org/packages/IconifyBundle.EosIcons) | [eos-icons](https://icon-sets.iconify.design/eos-icons/) | [MIT](https://gitlab.com/SUSE-UIUX/eos-icons/-/blob/master/LICENSE) | 59.4KB | 27.5KB |
| [IconifyBundle.Ep](https://www.nuget.org/packages/IconifyBundle.Ep) | [ep](https://icon-sets.iconify.design/ep/) | [MIT](https://github.com/element-plus/element-plus-icons/blob/main/packages/svg/package.json) | 54.9KB | 26.5KB |
| [IconifyBundle.Eva](https://www.nuget.org/packages/IconifyBundle.Eva) | [eva](https://icon-sets.iconify.design/eva/) | [MIT](https://github.com/akveo/eva-icons/blob/master/LICENSE.txt) | 59.8KB | 50.5KB |
| [IconifyBundle.F7](https://www.nuget.org/packages/IconifyBundle.F7) | [f7](https://icon-sets.iconify.design/f7/) | [MIT](https://github.com/framework7io/framework7-icons/blob/master/LICENSE) | 347.8KB | 121KB |
| [IconifyBundle.Fa](https://www.nuget.org/packages/IconifyBundle.Fa) | [fa](https://icon-sets.iconify.design/fa/) | [OFL](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 163.1KB | 53.5KB |
| [IconifyBundle.Fa6Brands](https://www.nuget.org/packages/IconifyBundle.Fa6Brands) | [fa6-brands](https://icon-sets.iconify.design/fa6-brands/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 234.3KB | 42KB |
| [IconifyBundle.Fa6Regular](https://www.nuget.org/packages/IconifyBundle.Fa6Regular) | [fa6-regular](https://icon-sets.iconify.design/fa6-regular/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 49.6KB | 18KB |
| [IconifyBundle.Fa6Solid](https://www.nuget.org/packages/IconifyBundle.Fa6Solid) | [fa6-solid](https://icon-sets.iconify.design/fa6-solid/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 279.3KB | 118.5KB |
| [IconifyBundle.Fa7Brands](https://www.nuget.org/packages/IconifyBundle.Fa7Brands) | [fa7-brands](https://icon-sets.iconify.design/fa7-brands/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 257.9KB | 49KB |
| [IconifyBundle.Fa7Regular](https://www.nuget.org/packages/IconifyBundle.Fa7Regular) | [fa7-regular](https://icon-sets.iconify.design/fa7-regular/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 61KB | 26.5KB |
| [IconifyBundle.Fa7Solid](https://www.nuget.org/packages/IconifyBundle.Fa7Solid) | [fa7-solid](https://icon-sets.iconify.design/fa7-solid/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 330.7KB | 166KB |
| [IconifyBundle.FaBrands](https://www.nuget.org/packages/IconifyBundle.FaBrands) | [fa-brands](https://icon-sets.iconify.design/fa-brands/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 216.6KB | 39KB |
| [IconifyBundle.Fad](https://www.nuget.org/packages/IconifyBundle.Fad) | [fad](https://icon-sets.iconify.design/fad/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 70.5KB | 16.5KB |
| [IconifyBundle.Famicons](https://www.nuget.org/packages/IconifyBundle.Famicons) | [famicons](https://icon-sets.iconify.design/famicons/) | [MIT](https://github.com/familyjs/famicons/blob/main/LICENSE) | 246.8KB | 122KB |
| [IconifyBundle.FaRegular](https://www.nuget.org/packages/IconifyBundle.FaRegular) | [fa-regular](https://icon-sets.iconify.design/fa-regular/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 50KB | 16.5KB |
| [IconifyBundle.FaSolid](https://www.nuget.org/packages/IconifyBundle.FaSolid) | [fa-solid](https://icon-sets.iconify.design/fa-solid/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 240.9KB | 83KB |
| [IconifyBundle.Fe](https://www.nuget.org/packages/IconifyBundle.Fe) | [fe](https://icon-sets.iconify.design/fe/) | [MIT](https://github.com/feathericon/feathericon/blob/master/LICENSE) | 41.9KB | 23KB |
| [IconifyBundle.Feather](https://www.nuget.org/packages/IconifyBundle.Feather) | [feather](https://icon-sets.iconify.design/feather/) | [MIT](https://github.com/feathericons/feather/blob/master/LICENSE) | 33.9KB | 25.5KB |
| [IconifyBundle.FileIcons](https://www.nuget.org/packages/IconifyBundle.FileIcons) | [file-icons](https://icon-sets.iconify.design/file-icons/) | [ISC](https://github.com/file-icons/icons/blob/master/LICENSE.md) | 588.4KB | 69.5KB |
| [IconifyBundle.Flag](https://www.nuget.org/packages/IconifyBundle.Flag) | [flag](https://icon-sets.iconify.design/flag/) | [MIT](https://github.com/lipis/flag-icons/blob/main/LICENSE) | 1.1MB | 40.5KB |
| [IconifyBundle.Flagpack](https://www.nuget.org/packages/IconifyBundle.Flagpack) | [flagpack](https://icon-sets.iconify.design/flagpack/) | [MIT](https://github.com/Yummygum/flagpack-core/blob/main/LICENSE) | 310.1KB | 19KB |
| [IconifyBundle.FlatColorIcons](https://www.nuget.org/packages/IconifyBundle.FlatColorIcons) | [flat-color-icons](https://icon-sets.iconify.design/flat-color-icons/) | [MIT](https://opensource.org/license/mit) | 71.6KB | 30.5KB |
| [IconifyBundle.FlatUi](https://www.nuget.org/packages/IconifyBundle.FlatUi) | [flat-ui](https://icon-sets.iconify.design/flat-ui/) | [MIT](https://github.com/designmodo/Flat-UI/blob/master/LICENSE) | 69.2KB | 11.5KB |
| [IconifyBundle.Flowbite](https://www.nuget.org/packages/IconifyBundle.Flowbite) | [flowbite](https://icon-sets.iconify.design/flowbite/) | [MIT](https://github.com/themesberg/flowbite-icons/blob/main/LICENSE) | 120.3KB | 83KB |
| [IconifyBundle.Fluent](https://www.nuget.org/packages/IconifyBundle.Fluent) | [fluent](https://icon-sets.iconify.design/fluent/) | [MIT](https://github.com/microsoft/fluentui-system-icons/blob/main/LICENSE) | 2.8MB | 2.4MB |
| [IconifyBundle.FluentColor](https://www.nuget.org/packages/IconifyBundle.FluentColor) | [fluent-color](https://icon-sets.iconify.design/fluent-color/) | [MIT](https://github.com/microsoft/fluentui-system-icons/blob/main/LICENSE) | 294.3KB | 82KB |
| [IconifyBundle.FluentEmoji](https://www.nuget.org/packages/IconifyBundle.FluentEmoji) | [fluent-emoji](https://icon-sets.iconify.design/fluent-emoji/) | [MIT](https://github.com/microsoft/fluentui-emoji/blob/main/LICENSE) | 13.8MB | 335.5KB |
| [IconifyBundle.FluentEmojiFlat](https://www.nuget.org/packages/IconifyBundle.FluentEmojiFlat) | [fluent-emoji-flat](https://icon-sets.iconify.design/fluent-emoji-flat/) | [MIT](https://github.com/microsoft/fluentui-emoji/blob/main/LICENSE) | 1.2MB | 336KB |
| [IconifyBundle.FluentEmojiHighContrast](https://www.nuget.org/packages/IconifyBundle.FluentEmojiHighContrast) | [fluent-emoji-high-contrast](https://icon-sets.iconify.design/fluent-emoji-high-contrast/) | [MIT](https://github.com/microsoft/fluentui-emoji/blob/main/LICENSE) | 1.1MB | 139KB |
| [IconifyBundle.FluentMdl2](https://www.nuget.org/packages/IconifyBundle.FluentMdl2) | [fluent-mdl2](https://icon-sets.iconify.design/fluent-mdl2/) | [MIT](https://github.com/microsoft/fluentui/blob/master/packages/react-icons-mdl2/LICENSE) | 291.7KB | 153.5KB |
| [IconifyBundle.Fontelico](https://www.nuget.org/packages/IconifyBundle.Fontelico) | [fontelico](https://icon-sets.iconify.design/fontelico/) | [CC BY SA](https://creativecommons.org/licenses/by-sa/3.0/) | 36.8KB | 7.5KB |
| [IconifyBundle.Fontisto](https://www.nuget.org/packages/IconifyBundle.Fontisto) | [fontisto](https://icon-sets.iconify.design/fontisto/) | [MIT](https://github.com/kenangundogan/fontisto/blob/master/LICENSE) | 314.4KB | 49.5KB |
| [IconifyBundle.Formkit](https://www.nuget.org/packages/IconifyBundle.Formkit) | [formkit](https://icon-sets.iconify.design/formkit/) | [MIT](https://github.com/formkit/formkit/blob/master/packages/icons/LICENSE) | 43.6KB | 14.5KB |
| [IconifyBundle.Foundation](https://www.nuget.org/packages/IconifyBundle.Foundation) | [foundation](https://icon-sets.iconify.design/foundation/) | [MIT](https://opensource.org/license/mit) | 108.2KB | 26.5KB |
| [IconifyBundle.Fxemoji](https://www.nuget.org/packages/IconifyBundle.Fxemoji) | [fxemoji](https://icon-sets.iconify.design/fxemoji/) | [Apache 2.0](https://mozilla.github.io/fxemoji/LICENSE.md) | 1MB | 91KB |
| [IconifyBundle.GameIcons](https://www.nuget.org/packages/IconifyBundle.GameIcons) | [game-icons](https://icon-sets.iconify.design/game-icons/) | [CC BY 3.0](https://github.com/game-icons/icons/blob/master/license.txt) | 2.8MB | 332KB |
| [IconifyBundle.Garden](https://www.nuget.org/packages/IconifyBundle.Garden) | [garden](https://icon-sets.iconify.design/garden/) | [Apache 2.0](https://github.com/zendeskgarden/svg-icons/blob/main/LICENSE.md) | 141.1KB | 108KB |
| [IconifyBundle.Gcp](https://www.nuget.org/packages/IconifyBundle.Gcp) | [gcp](https://icon-sets.iconify.design/gcp/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 72.1KB | 24.5KB |
| [IconifyBundle.Geo](https://www.nuget.org/packages/IconifyBundle.Geo) | [geo](https://icon-sets.iconify.design/geo/) | [MIT](https://github.com/cugos/geoglyphs/blob/main/LICENSE.md) | 30.8KB | 7.5KB |
| [IconifyBundle.Gg](https://www.nuget.org/packages/IconifyBundle.Gg) | [gg](https://icon-sets.iconify.design/gg/) | [MIT](https://github.com/astrit/css.gg/blob/master/LICENSE) | 77.7KB | 60KB |
| [IconifyBundle.Ginetex](https://www.nuget.org/packages/IconifyBundle.Ginetex) | [ginetex](https://icon-sets.iconify.design/ginetex/) | [MIT](https://github.com/yessir-web-tech/ginetex-icons/blob/main/LICENSE) | 26KB | 10.5KB |
| [IconifyBundle.Gis](https://www.nuget.org/packages/IconifyBundle.Gis) | [gis](https://icon-sets.iconify.design/gis/) | [CC BY 4.0](https://github.com/Viglino/font-gis/blob/main/LICENSE-CC-BY.md) | 364.3KB | 33KB |
| [IconifyBundle.Glyphs](https://www.nuget.org/packages/IconifyBundle.Glyphs) | [glyphs](https://icon-sets.iconify.design/glyphs/) | [MIT](https://github.com/gorango/glyphs/blob/main/license) | 738KB | 299.5KB |
| [IconifyBundle.GlyphsPoly](https://www.nuget.org/packages/IconifyBundle.GlyphsPoly) | [glyphs-poly](https://icon-sets.iconify.design/glyphs-poly/) | [MIT](https://github.com/gorango/glyphs/blob/main/license) | 303.2KB | 68.5KB |
| [IconifyBundle.GravityUi](https://www.nuget.org/packages/IconifyBundle.GravityUi) | [gravity-ui](https://icon-sets.iconify.design/gravity-ui/) | [MIT](https://github.com/gravity-ui/icons/blob/main/LICENSE) | 138.7KB | 72.5KB |
| [IconifyBundle.GrommetIcons](https://www.nuget.org/packages/IconifyBundle.GrommetIcons) | [grommet-icons](https://icon-sets.iconify.design/grommet-icons/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 110.5KB | 51.5KB |
| [IconifyBundle.Guidance](https://www.nuget.org/packages/IconifyBundle.Guidance) | [guidance](https://icon-sets.iconify.design/guidance/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 69.3KB | 34.5KB |
| [IconifyBundle.Healthicons](https://www.nuget.org/packages/IconifyBundle.Healthicons) | [healthicons](https://icon-sets.iconify.design/healthicons/) | [MIT](https://github.com/resolvetosavelives/healthicons/blob/main/LICENSE) | 838.9KB | 270KB |
| [IconifyBundle.Heroicons](https://www.nuget.org/packages/IconifyBundle.Heroicons) | [heroicons](https://icon-sets.iconify.design/heroicons/) | [MIT](https://github.com/tailwindlabs/heroicons/blob/master/LICENSE) | 174.4KB | 135.5KB |
| [IconifyBundle.HeroiconsOutline](https://www.nuget.org/packages/IconifyBundle.HeroiconsOutline) | [heroicons-outline](https://icon-sets.iconify.design/heroicons-outline/) | [MIT](https://github.com/tailwindlabs/heroicons/blob/master/LICENSE) | 52.6KB | 37KB |
| [IconifyBundle.HeroiconsSolid](https://www.nuget.org/packages/IconifyBundle.HeroiconsSolid) | [heroicons-solid](https://icon-sets.iconify.design/heroicons-solid/) | [MIT](https://github.com/tailwindlabs/heroicons/blob/master/LICENSE) | 61KB | 37KB |
| [IconifyBundle.Hugeicons](https://www.nuget.org/packages/IconifyBundle.Hugeicons) | [hugeicons](https://icon-sets.iconify.design/hugeicons/) | [MIT](https://opensource.org/license/mit) | 826.6KB | 452.5KB |
| [IconifyBundle.Humbleicons](https://www.nuget.org/packages/IconifyBundle.Humbleicons) | [humbleicons](https://icon-sets.iconify.design/humbleicons/) | [MIT](https://github.com/zraly/humbleicons/blob/master/license) | 39.7KB | 26.5KB |
| [IconifyBundle.Ic](https://www.nuget.org/packages/IconifyBundle.Ic) | [ic](https://icon-sets.iconify.design/ic/) | [Apache 2.0](https://github.com/material-icons/material-icons/blob/master/LICENSE) | 1.3MB | 1.1MB |
| [IconifyBundle.Iconamoon](https://www.nuget.org/packages/IconifyBundle.Iconamoon) | [iconamoon](https://icon-sets.iconify.design/iconamoon/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 119.7KB | 178KB |
| [IconifyBundle.Iconoir](https://www.nuget.org/packages/IconifyBundle.Iconoir) | [iconoir](https://icon-sets.iconify.design/iconoir/) | [MIT](https://github.com/iconoir-icons/iconoir/blob/main/LICENSE) | 172.1KB | 148KB |
| [IconifyBundle.IconPark](https://www.nuget.org/packages/IconifyBundle.IconPark) | [icon-park](https://icon-sets.iconify.design/icon-park/) | [Apache 2.0](https://github.com/bytedance/IconPark/blob/master/LICENSE) | 406.4KB | 216KB |
| [IconifyBundle.IconParkOutline](https://www.nuget.org/packages/IconifyBundle.IconParkOutline) | [icon-park-outline](https://icon-sets.iconify.design/icon-park-outline/) | [Apache 2.0](https://github.com/bytedance/IconPark/blob/master/LICENSE) | 288.9KB | 216KB |
| [IconifyBundle.IconParkSolid](https://www.nuget.org/packages/IconifyBundle.IconParkSolid) | [icon-park-solid](https://icon-sets.iconify.design/icon-park-solid/) | [Apache 2.0](https://github.com/bytedance/IconPark/blob/master/LICENSE) | 241.6KB | 161KB |
| [IconifyBundle.IconParkTwotone](https://www.nuget.org/packages/IconifyBundle.IconParkTwotone) | [icon-park-twotone](https://icon-sets.iconify.design/icon-park-twotone/) | [Apache 2.0](https://github.com/bytedance/IconPark/blob/master/LICENSE) | 245KB | 159KB |
| [IconifyBundle.Icons8](https://www.nuget.org/packages/IconifyBundle.Icons8) | [icons8](https://icon-sets.iconify.design/icons8/) | [MIT](https://opensource.org/license/mit) | 55.3KB | 22.5KB |
| [IconifyBundle.Il](https://www.nuget.org/packages/IconifyBundle.Il) | [il](https://icon-sets.iconify.design/il/) | [MIT](https://opensource.org/license/mit) | 29.2KB | 10.5KB |
| [IconifyBundle.Ion](https://www.nuget.org/packages/IconifyBundle.Ion) | [ion](https://icon-sets.iconify.design/ion/) | [MIT](https://github.com/ionic-team/ionicons/blob/main/LICENSE) | 492KB | 209KB |
| [IconifyBundle.Iwwa](https://www.nuget.org/packages/IconifyBundle.Iwwa) | [iwwa](https://icon-sets.iconify.design/iwwa/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 63.4KB | 12KB |
| [IconifyBundle.Ix](https://www.nuget.org/packages/IconifyBundle.Ix) | [ix](https://icon-sets.iconify.design/ix/) | [MIT](https://github.com/siemens/ix-icons/blob/main/LICENSE.md) | 282.5KB | 131KB |
| [IconifyBundle.Jam](https://www.nuget.org/packages/IconifyBundle.Jam) | [jam](https://icon-sets.iconify.design/jam/) | [MIT](https://github.com/cyberalien/jam-backup/blob/main/LICENSE) | 132.5KB | 80.5KB |
| [IconifyBundle.La](https://www.nuget.org/packages/IconifyBundle.La) | [la](https://icon-sets.iconify.design/la/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 403.7KB | 124.5KB |
| [IconifyBundle.LetsIcons](https://www.nuget.org/packages/IconifyBundle.LetsIcons) | [lets-icons](https://icon-sets.iconify.design/lets-icons/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 228.8KB | 140.5KB |
| [IconifyBundle.Lineicons](https://www.nuget.org/packages/IconifyBundle.Lineicons) | [lineicons](https://icon-sets.iconify.design/lineicons/) | [MIT](https://github.com/LineiconsHQ/Lineicons/blob/main/LICENSE.md) | 307.1KB | 77.5KB |
| [IconifyBundle.LineMd](https://www.nuget.org/packages/IconifyBundle.LineMd) | [line-md](https://icon-sets.iconify.design/line-md/) | [MIT](https://github.com/cyberalien/line-md/blob/main/license.txt) | 108.9KB | 127.5KB |
| [IconifyBundle.Logos](https://www.nuget.org/packages/IconifyBundle.Logos) | [logos](https://icon-sets.iconify.design/logos/) | [CC0](https://raw.githubusercontent.com/gilbarbara/logos/master/LICENSE.txt) | 2.7MB | 160.5KB |
| [IconifyBundle.Ls](https://www.nuget.org/packages/IconifyBundle.Ls) | [ls](https://icon-sets.iconify.design/ls/) | [OFL](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 94.8KB | 28KB |
| [IconifyBundle.Lsicon](https://www.nuget.org/packages/IconifyBundle.Lsicon) | [lsicon](https://icon-sets.iconify.design/lsicon/) | [MIT](https://github.com/wisdesignsystem/lsicon/blob/main/LICENSE) | 84.4KB | 74KB |
| [IconifyBundle.Lucide](https://www.nuget.org/packages/IconifyBundle.Lucide) | [lucide](https://icon-sets.iconify.design/lucide/) | [ISC](https://github.com/lucide-icons/lucide/blob/main/LICENSE) | 139.9KB | 147.5KB |
| [IconifyBundle.LucideLab](https://www.nuget.org/packages/IconifyBundle.LucideLab) | [lucide-lab](https://icon-sets.iconify.design/lucide-lab/) | [ISC](https://github.com/lucide-icons/lucide-lab/blob/main/LICENSE) | 49.9KB | 34.5KB |
| [IconifyBundle.Mage](https://www.nuget.org/packages/IconifyBundle.Mage) | [mage](https://icon-sets.iconify.design/mage/) | [Apache 2.0](https://github.com/Mage-Icons/mage-icons/blob/main/License.txt) | 175.3KB | 96.5KB |
| [IconifyBundle.Majesticons](https://www.nuget.org/packages/IconifyBundle.Majesticons) | [majesticons](https://icon-sets.iconify.design/majesticons/) | [MIT](https://github.com/halfmage/majesticons/blob/main/LICENSE) | 113.1KB | 93.5KB |
| [IconifyBundle.Maki](https://www.nuget.org/packages/IconifyBundle.Maki) | [maki](https://icon-sets.iconify.design/maki/) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | 85.8KB | 37.5KB |
| [IconifyBundle.Map](https://www.nuget.org/packages/IconifyBundle.Map) | [map](https://icon-sets.iconify.design/map/) | [OFL](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 75.4KB | 18KB |
| [IconifyBundle.Marketeq](https://www.nuget.org/packages/IconifyBundle.Marketeq) | [marketeq](https://icon-sets.iconify.design/marketeq/) | [MIT](https://opensource.org/license/mit) | 72.7KB | 52KB |
| [IconifyBundle.MaterialIconTheme](https://www.nuget.org/packages/IconifyBundle.MaterialIconTheme) | [material-icon-theme](https://icon-sets.iconify.design/material-icon-theme/) | [MIT](https://github.com/material-extensions/vscode-material-icon-theme/blob/main/LICENSE) | 289.5KB | 99.5KB |
| [IconifyBundle.MaterialSymbols](https://www.nuget.org/packages/IconifyBundle.MaterialSymbols) | [material-symbols](https://icon-sets.iconify.design/material-symbols/) | [Apache 2.0](https://github.com/google/material-design-icons/blob/master/LICENSE) | 1.6MB | 1.7MB |
| [IconifyBundle.MaterialSymbolsLight](https://www.nuget.org/packages/IconifyBundle.MaterialSymbolsLight) | [material-symbols-light](https://icon-sets.iconify.design/material-symbols-light/) | [Apache 2.0](https://github.com/google/material-design-icons/blob/master/LICENSE) | 2.1MB | 1.7MB |
| [IconifyBundle.Mdi](https://www.nuget.org/packages/IconifyBundle.Mdi) | [mdi](https://icon-sets.iconify.design/mdi/) | [Apache 2.0](https://github.com/Templarian/MaterialDesign/blob/master/LICENSE) | 863.9KB | 734.5KB |
| [IconifyBundle.MdiLight](https://www.nuget.org/packages/IconifyBundle.MdiLight) | [mdi-light](https://icon-sets.iconify.design/mdi-light/) | [OFL](https://github.com/Templarian/MaterialDesignLight/blob/master/LICENSE.md) | 46.1KB | 29KB |
| [IconifyBundle.MedicalIcon](https://www.nuget.org/packages/IconifyBundle.MedicalIcon) | [medical-icon](https://icon-sets.iconify.design/medical-icon/) | [MIT](https://github.com/samcome/webfont-medical-icons/blob/master/LICENSE) | 99.5KB | 17.5KB |
| [IconifyBundle.Memory](https://www.nuget.org/packages/IconifyBundle.Memory) | [memory](https://icon-sets.iconify.design/memory/) | [Apache 2.0](https://github.com/Pictogrammers/Memory/blob/main/LICENSE) | 55.3KB | 70KB |
| [IconifyBundle.Meteocons](https://www.nuget.org/packages/IconifyBundle.Meteocons) | [meteocons](https://icon-sets.iconify.design/meteocons/) | [MIT](https://github.com/basmilius/weather-icons/blob/dev/LICENSE) | 113.7KB | 48.5KB |
| [IconifyBundle.MeteorIcons](https://www.nuget.org/packages/IconifyBundle.MeteorIcons) | [meteor-icons](https://icon-sets.iconify.design/meteor-icons/) | [MIT](https://github.com/zkreations/icons/blob/main/LICENSE) | 35.1KB | 28.5KB |
| [IconifyBundle.Mi](https://www.nuget.org/packages/IconifyBundle.Mi) | [mi](https://icon-sets.iconify.design/mi/) | [MIT](https://github.com/mono-company/mono-icons/blob/master/LICENSE.md) | 35.3KB | 18KB |
| [IconifyBundle.Mingcute](https://www.nuget.org/packages/IconifyBundle.Mingcute) | [mingcute](https://icon-sets.iconify.design/mingcute/) | [Apache 2.0](https://github.com/Richard9394/MingCute/blob/main/LICENSE) | 611KB | 300.5KB |
| [IconifyBundle.MonoIcons](https://www.nuget.org/packages/IconifyBundle.MonoIcons) | [mono-icons](https://icon-sets.iconify.design/mono-icons/) | [MIT](https://github.com/mono-company/mono-icons/blob/master/LICENSE.md) | 35.4KB | 18KB |
| [IconifyBundle.Mynaui](https://www.nuget.org/packages/IconifyBundle.Mynaui) | [mynaui](https://icon-sets.iconify.design/mynaui/) | [MIT](https://github.com/praveenjuge/mynaui-icons/blob/main/LICENSE) | 314.2KB | 239.5KB |
| [IconifyBundle.Nimbus](https://www.nuget.org/packages/IconifyBundle.Nimbus) | [nimbus](https://icon-sets.iconify.design/nimbus/) | [MIT](https://github.com/cyberalien/nimbus-icons/blob/main/LICENSE) | 42KB | 15KB |
| [IconifyBundle.Nonicons](https://www.nuget.org/packages/IconifyBundle.Nonicons) | [nonicons](https://icon-sets.iconify.design/nonicons/) | [MIT](https://github.com/yamatsum/nonicons/blob/master/LICENSE) | 38KB | 10KB |
| [IconifyBundle.Noto](https://www.nuget.org/packages/IconifyBundle.Noto) | [noto](https://icon-sets.iconify.design/noto/) | [Apache 2.0](https://github.com/googlefonts/noto-emoji/blob/main/svg/LICENSE) | 3.3MB | 486.5KB |
| [IconifyBundle.NotoV1](https://www.nuget.org/packages/IconifyBundle.NotoV1) | [noto-v1](https://icon-sets.iconify.design/noto-v1/) | [Apache 2.0](https://github.com/googlefonts/noto-emoji/blob/main/svg/LICENSE) | 1.6MB | 239KB |
| [IconifyBundle.Nrk](https://www.nuget.org/packages/IconifyBundle.Nrk) | [nrk](https://icon-sets.iconify.design/nrk/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 45.3KB | 25.5KB |
| [IconifyBundle.Octicon](https://www.nuget.org/packages/IconifyBundle.Octicon) | [octicon](https://icon-sets.iconify.design/octicon/) | [MIT](https://github.com/primer/octicons/blob/main/LICENSE) | 161KB | 80KB |
| [IconifyBundle.Oi](https://www.nuget.org/packages/IconifyBundle.Oi) | [oi](https://icon-sets.iconify.design/oi/) | [MIT](https://github.com/iconic/open-iconic/blob/master/ICON-LICENSE) | 35.4KB | 21.5KB |
| [IconifyBundle.Ooui](https://www.nuget.org/packages/IconifyBundle.Ooui) | [ooui](https://icon-sets.iconify.design/ooui/) | [MIT](https://github.com/wikimedia/oojs-ui/blob/master/LICENSE-MIT) | 61.2KB | 36KB |
| [IconifyBundle.Openmoji](https://www.nuget.org/packages/IconifyBundle.Openmoji) | [openmoji](https://icon-sets.iconify.design/openmoji/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 1.3MB | 551.5KB |
| [IconifyBundle.Oui](https://www.nuget.org/packages/IconifyBundle.Oui) | [oui](https://icon-sets.iconify.design/oui/) | [Apache 2.0](https://github.com/opensearch-project/oui/blob/main/LICENSE.txt) | 97.1KB | 41.5KB |
| [IconifyBundle.Pajamas](https://www.nuget.org/packages/IconifyBundle.Pajamas) | [pajamas](https://icon-sets.iconify.design/pajamas/) | [MIT](https://gitlab.com/gitlab-org/gitlab-svgs/-/blob/main/LICENSE) | 73KB | 37KB |
| [IconifyBundle.Pepicons](https://www.nuget.org/packages/IconifyBundle.Pepicons) | [pepicons](https://icon-sets.iconify.design/pepicons/) | [CC BY 4.0](https://github.com/CyCraft/pepicons/blob/dev/LICENSE) | 126.9KB | 40KB |
| [IconifyBundle.PepiconsPencil](https://www.nuget.org/packages/IconifyBundle.PepiconsPencil) | [pepicons-pencil](https://icon-sets.iconify.design/pepicons-pencil/) | [CC BY 4.0](https://github.com/CyCraft/pepicons/blob/dev/LICENSE) | 137.1KB | 125.5KB |
| [IconifyBundle.PepiconsPop](https://www.nuget.org/packages/IconifyBundle.PepiconsPop) | [pepicons-pop](https://icon-sets.iconify.design/pepicons-pop/) | [CC BY 4.0](https://github.com/CyCraft/pepicons/blob/dev/LICENSE) | 138KB | 126.5KB |
| [IconifyBundle.PepiconsPrint](https://www.nuget.org/packages/IconifyBundle.PepiconsPrint) | [pepicons-print](https://icon-sets.iconify.design/pepicons-print/) | [CC BY 4.0](https://github.com/CyCraft/pepicons/blob/dev/LICENSE) | 184.8KB | 126KB |
| [IconifyBundle.Ph](https://www.nuget.org/packages/IconifyBundle.Ph) | [ph](https://icon-sets.iconify.design/ph/) | [MIT](https://github.com/phosphor-icons/core/blob/main/LICENSE) | 1.1MB | 907.5KB |
| [IconifyBundle.Picon](https://www.nuget.org/packages/IconifyBundle.Picon) | [picon](https://icon-sets.iconify.design/picon/) | [OFL](https://github.com/yne/picon/blob/master/OFL.txt) | 58.5KB | 59.5KB |
| [IconifyBundle.Pinhead](https://www.nuget.org/packages/IconifyBundle.Pinhead) | [pinhead](https://icon-sets.iconify.design/pinhead/) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | 404.5KB | 209.5KB |
| [IconifyBundle.Pixel](https://www.nuget.org/packages/IconifyBundle.Pixel) | [pixel](https://icon-sets.iconify.design/pixel/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 60.5KB | 52KB |
| [IconifyBundle.Pixelarticons](https://www.nuget.org/packages/IconifyBundle.Pixelarticons) | [pixelarticons](https://icon-sets.iconify.design/pixelarticons/) | [MIT](https://github.com/halfmage/pixelarticons/blob/master/LICENSE) | 81.5KB | 97.5KB |
| [IconifyBundle.Prime](https://www.nuget.org/packages/IconifyBundle.Prime) | [prime](https://icon-sets.iconify.design/prime/) | [MIT](https://github.com/primefaces/primeicons/blob/master/LICENSE) | 63.5KB | 28.5KB |
| [IconifyBundle.Proicons](https://www.nuget.org/packages/IconifyBundle.Proicons) | [proicons](https://icon-sets.iconify.design/proicons/) | [MIT](https://github.com/ProCode-Software/proicons/blob/main/LICENSE) | 84.3KB | 47.5KB |
| [IconifyBundle.QlementineIcons](https://www.nuget.org/packages/IconifyBundle.QlementineIcons) | [qlementine-icons](https://icon-sets.iconify.design/qlementine-icons/) | [MIT](https://github.com/oclero/qlementine-icons/blob/master/LICENSE) | 268.4KB | 79KB |
| [IconifyBundle.Quill](https://www.nuget.org/packages/IconifyBundle.Quill) | [quill](https://icon-sets.iconify.design/quill/) | [MIT](https://github.com/yourtempo/tempo-quill-icons/blob/main/LICENSE) | 32.1KB | 15KB |
| [IconifyBundle.RadixIcons](https://www.nuget.org/packages/IconifyBundle.RadixIcons) | [radix-icons](https://icon-sets.iconify.design/radix-icons/) | [MIT](https://github.com/radix-ui/icons/blob/master/LICENSE) | 71.7KB | 32KB |
| [IconifyBundle.Raphael](https://www.nuget.org/packages/IconifyBundle.Raphael) | [raphael](https://icon-sets.iconify.design/raphael/) | [MIT](https://opensource.org/license/mit) | 100.3KB | 22.5KB |
| [IconifyBundle.Ri](https://www.nuget.org/packages/IconifyBundle.Ri) | [ri](https://icon-sets.iconify.design/ri/) | [Apache 2.0](https://github.com/cyberalien/RemixIcon/blob/master/License) | 346.2KB | 292.5KB |
| [IconifyBundle.RivetIcons](https://www.nuget.org/packages/IconifyBundle.RivetIcons) | [rivet-icons](https://icon-sets.iconify.design/rivet-icons/) | [BSD 3-Clause](https://github.com/indiana-university/rivet-icons/blob/develop/LICENSE) | 34.8KB | 21.5KB |
| [IconifyBundle.Roentgen](https://www.nuget.org/packages/IconifyBundle.Roentgen) | [roentgen](https://icon-sets.iconify.design/roentgen/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 94.6KB | 54.5KB |
| [IconifyBundle.Si](https://www.nuget.org/packages/IconifyBundle.Si) | [si](https://icon-sets.iconify.design/si/) | [MIT](https://github.com/planetabhi/sargam-icons/blob/main/LICENSE.txt) | 134KB | 130.5KB |
| [IconifyBundle.Sidekickicons](https://www.nuget.org/packages/IconifyBundle.Sidekickicons) | [sidekickicons](https://icon-sets.iconify.design/sidekickicons/) | [MIT](https://github.com/ndri/sidekickicons/blob/master/LICENSE) | 50.1KB | 27.5KB |
| [IconifyBundle.SiGlyph](https://www.nuget.org/packages/IconifyBundle.SiGlyph) | [si-glyph](https://icon-sets.iconify.design/si-glyph/) | [CC BY SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 187.2KB | 67KB |
| [IconifyBundle.SimpleIcons](https://www.nuget.org/packages/IconifyBundle.SimpleIcons) | [simple-icons](https://icon-sets.iconify.design/simple-icons/) | [CC0 1.0](https://github.com/simple-icons/simple-icons/blob/develop/LICENSE.md) | 1.9MB | 274.5KB |
| [IconifyBundle.SimpleLineIcons](https://www.nuget.org/packages/IconifyBundle.SimpleLineIcons) | [simple-line-icons](https://icon-sets.iconify.design/simple-line-icons/) | [MIT](https://github.com/thesabbir/simple-line-icons/blob/master/LICENSE.md) | 95.8KB | 19.5KB |
| [IconifyBundle.SkillIcons](https://www.nuget.org/packages/IconifyBundle.SkillIcons) | [skill-icons](https://icon-sets.iconify.design/skill-icons/) | [MIT](https://github.com/tandpfun/skill-icons/blob/main/LICENSE) | 520.2KB | 37KB |
| [IconifyBundle.Solar](https://www.nuget.org/packages/IconifyBundle.Solar) | [solar](https://icon-sets.iconify.design/solar/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1.2MB | 840.5KB |
| [IconifyBundle.Stash](https://www.nuget.org/packages/IconifyBundle.Stash) | [stash](https://icon-sets.iconify.design/stash/) | [MIT](https://github.com/stash-ui/icons/blob/master/LICENSE) | 271.8KB | 95.5KB |
| [IconifyBundle.Streamline](https://www.nuget.org/packages/IconifyBundle.Streamline) | [streamline](https://icon-sets.iconify.design/streamline/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 605.5KB | 542KB |
| [IconifyBundle.StreamlineBlock](https://www.nuget.org/packages/IconifyBundle.StreamlineBlock) | [streamline-block](https://icon-sets.iconify.design/streamline-block/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 38.2KB | 35KB |
| [IconifyBundle.StreamlineColor](https://www.nuget.org/packages/IconifyBundle.StreamlineColor) | [streamline-color](https://icon-sets.iconify.design/streamline-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 325.9KB | 192.5KB |
| [IconifyBundle.StreamlineCyber](https://www.nuget.org/packages/IconifyBundle.StreamlineCyber) | [streamline-cyber](https://icon-sets.iconify.design/streamline-cyber/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 70.3KB | 48KB |
| [IconifyBundle.StreamlineCyberColor](https://www.nuget.org/packages/IconifyBundle.StreamlineCyberColor) | [streamline-cyber-color](https://icon-sets.iconify.design/streamline-cyber-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 106.3KB | 48KB |
| [IconifyBundle.StreamlineEmojis](https://www.nuget.org/packages/IconifyBundle.StreamlineEmojis) | [streamline-emojis](https://icon-sets.iconify.design/streamline-emojis/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 414.1KB | 74KB |
| [IconifyBundle.StreamlineFlex](https://www.nuget.org/packages/IconifyBundle.StreamlineFlex) | [streamline-flex](https://icon-sets.iconify.design/streamline-flex/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 493.1KB | 153KB |
| [IconifyBundle.StreamlineFlexColor](https://www.nuget.org/packages/IconifyBundle.StreamlineFlexColor) | [streamline-flex-color](https://icon-sets.iconify.design/streamline-flex-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 304.1KB | 99.5KB |
| [IconifyBundle.StreamlineFreehand](https://www.nuget.org/packages/IconifyBundle.StreamlineFreehand) | [streamline-freehand](https://icon-sets.iconify.design/streamline-freehand/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1MB | 111.5KB |
| [IconifyBundle.StreamlineFreehandColor](https://www.nuget.org/packages/IconifyBundle.StreamlineFreehandColor) | [streamline-freehand-color](https://icon-sets.iconify.design/streamline-freehand-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1MB | 111.5KB |
| [IconifyBundle.StreamlineKameleonColor](https://www.nuget.org/packages/IconifyBundle.StreamlineKameleonColor) | [streamline-kameleon-color](https://icon-sets.iconify.design/streamline-kameleon-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 174.7KB | 37KB |
| [IconifyBundle.StreamlineLogos](https://www.nuget.org/packages/IconifyBundle.StreamlineLogos) | [streamline-logos](https://icon-sets.iconify.design/streamline-logos/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 285.7KB | 141.5KB |
| [IconifyBundle.StreamlinePixel](https://www.nuget.org/packages/IconifyBundle.StreamlinePixel) | [streamline-pixel](https://icon-sets.iconify.design/streamline-pixel/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 149.2KB | 88.5KB |
| [IconifyBundle.StreamlinePlump](https://www.nuget.org/packages/IconifyBundle.StreamlinePlump) | [streamline-plump](https://icon-sets.iconify.design/streamline-plump/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 646.5KB | 151KB |
| [IconifyBundle.StreamlinePlumpColor](https://www.nuget.org/packages/IconifyBundle.StreamlinePlumpColor) | [streamline-plump-color](https://icon-sets.iconify.design/streamline-plump-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 426KB | 97.5KB |
| [IconifyBundle.StreamlineSharp](https://www.nuget.org/packages/IconifyBundle.StreamlineSharp) | [streamline-sharp](https://icon-sets.iconify.design/streamline-sharp/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 209.9KB | 152.5KB |
| [IconifyBundle.StreamlineSharpColor](https://www.nuget.org/packages/IconifyBundle.StreamlineSharpColor) | [streamline-sharp-color](https://icon-sets.iconify.design/streamline-sharp-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 140.6KB | 99KB |
| [IconifyBundle.StreamlineStickiesColor](https://www.nuget.org/packages/IconifyBundle.StreamlineStickiesColor) | [streamline-stickies-color](https://icon-sets.iconify.design/streamline-stickies-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 169.6KB | 22KB |
| [IconifyBundle.StreamlineUltimate](https://www.nuget.org/packages/IconifyBundle.StreamlineUltimate) | [streamline-ultimate](https://icon-sets.iconify.design/streamline-ultimate/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 415.5KB | 213.5KB |
| [IconifyBundle.StreamlineUltimateColor](https://www.nuget.org/packages/IconifyBundle.StreamlineUltimateColor) | [streamline-ultimate-color](https://icon-sets.iconify.design/streamline-ultimate-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 290.4KB | 99.5KB |
| [IconifyBundle.Subway](https://www.nuget.org/packages/IconifyBundle.Subway) | [subway](https://icon-sets.iconify.design/subway/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 53.4KB | 27KB |
| [IconifyBundle.SvgSpinners](https://www.nuget.org/packages/IconifyBundle.SvgSpinners) | [svg-spinners](https://icon-sets.iconify.design/svg-spinners/) | [MIT](https://github.com/n3r4zzurr0/svg-spinners/blob/main/LICENSE) | 23.8KB | 8.5KB |
| [IconifyBundle.SystemUicons](https://www.nuget.org/packages/IconifyBundle.SystemUicons) | [system-uicons](https://icon-sets.iconify.design/system-uicons/) | [Unlicense](https://github.com/CoreyGinnivan/system-uicons/blob/master/LICENSE) | 47.9KB | 38KB |
| [IconifyBundle.Tabler](https://www.nuget.org/packages/IconifyBundle.Tabler) | [tabler](https://icon-sets.iconify.design/tabler/) | [MIT](https://github.com/tabler/tabler-icons/blob/master/LICENSE) | 489.8KB | 570.5KB |
| [IconifyBundle.Tdesign](https://www.nuget.org/packages/IconifyBundle.Tdesign) | [tdesign](https://icon-sets.iconify.design/tdesign/) | [MIT](https://github.com/Tencent/tdesign-icons/blob/main/LICENSE) | 261.2KB | 209.5KB |
| [IconifyBundle.Teenyicons](https://www.nuget.org/packages/IconifyBundle.Teenyicons) | [teenyicons](https://icon-sets.iconify.design/teenyicons/) | [MIT](https://github.com/teenyicons/teenyicons/blob/master/LICENSE) | 147.6KB | 117.5KB |
| [IconifyBundle.Temaki](https://www.nuget.org/packages/IconifyBundle.Temaki) | [temaki](https://icon-sets.iconify.design/temaki/) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | 131.3KB | 49.5KB |
| [IconifyBundle.Token](https://www.nuget.org/packages/IconifyBundle.Token) | [token](https://icon-sets.iconify.design/token/) | [MIT](https://github.com/0xa3k5/web3icons/blob/main/LICENCE) | 830.9KB | 118KB |
| [IconifyBundle.TokenBranded](https://www.nuget.org/packages/IconifyBundle.TokenBranded) | [token-branded](https://icon-sets.iconify.design/token-branded/) | [MIT](https://github.com/0xa3k5/web3icons/blob/main/LICENCE) | 2MB | 134.5KB |
| [IconifyBundle.Topcoat](https://www.nuget.org/packages/IconifyBundle.Topcoat) | [topcoat](https://icon-sets.iconify.design/topcoat/) | [Apache 2.0](https://github.com/topcoat/icons/blob/master/LICENSE) | 34.7KB | 11KB |
| [IconifyBundle.Twemoji](https://www.nuget.org/packages/IconifyBundle.Twemoji) | [twemoji](https://icon-sets.iconify.design/twemoji/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1.6MB | 526KB |
| [IconifyBundle.Typcn](https://www.nuget.org/packages/IconifyBundle.Typcn) | [typcn](https://icon-sets.iconify.design/typcn/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 83.3KB | 32.5KB |
| [IconifyBundle.Uil](https://www.nuget.org/packages/IconifyBundle.Uil) | [uil](https://icon-sets.iconify.design/uil/) | [Apache 2.0](https://github.com/Iconscout/unicons/blob/master/LICENSE) | 182.2KB | 102.5KB |
| [IconifyBundle.Uim](https://www.nuget.org/packages/IconifyBundle.Uim) | [uim](https://icon-sets.iconify.design/uim/) | [Apache 2.0](https://github.com/Iconscout/unicons/blob/master/LICENSE) | 67.9KB | 28.5KB |
| [IconifyBundle.Uis](https://www.nuget.org/packages/IconifyBundle.Uis) | [uis](https://icon-sets.iconify.design/uis/) | [Apache 2.0](https://github.com/Iconscout/unicons/blob/master/LICENSE) | 34.8KB | 20KB |
| [IconifyBundle.Uit](https://www.nuget.org/packages/IconifyBundle.Uit) | [uit](https://icon-sets.iconify.design/uit/) | [Apache 2.0](https://github.com/Iconscout/unicons/blob/master/LICENSE) | 52.1KB | 22.5KB |
| [IconifyBundle.Uiw](https://www.nuget.org/packages/IconifyBundle.Uiw) | [uiw](https://icon-sets.iconify.design/uiw/) | [MIT](https://github.com/uiwjs/icons/blob/master/LICENSE) | 67.9KB | 20.5KB |
| [IconifyBundle.Unjs](https://www.nuget.org/packages/IconifyBundle.Unjs) | [unjs](https://icon-sets.iconify.design/unjs/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 95.8KB | 9KB |
| [IconifyBundle.Vaadin](https://www.nuget.org/packages/IconifyBundle.Vaadin) | [vaadin](https://icon-sets.iconify.design/vaadin/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 87.4KB | 53KB |
| [IconifyBundle.Vs](https://www.nuget.org/packages/IconifyBundle.Vs) | [vs](https://icon-sets.iconify.design/vs/) | [OFL](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 55.8KB | 16.5KB |
| [IconifyBundle.VscodeIcons](https://www.nuget.org/packages/IconifyBundle.VscodeIcons) | [vscode-icons](https://icon-sets.iconify.design/vscode-icons/) | [MIT](https://github.com/vscode-icons/vscode-icons/blob/master/LICENSE) | 1.1MB | 157.5KB |
| [IconifyBundle.Websymbol](https://www.nuget.org/packages/IconifyBundle.Websymbol) | [websymbol](https://icon-sets.iconify.design/websymbol/) | [OFL](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 27.5KB | 11KB |
| [IconifyBundle.Weui](https://www.nuget.org/packages/IconifyBundle.Weui) | [weui](https://icon-sets.iconify.design/weui/) | [MIT](https://opensource.org/license/mit) | 35KB | 19.5KB |
| [IconifyBundle.Whh](https://www.nuget.org/packages/IconifyBundle.Whh) | [whh](https://icon-sets.iconify.design/whh/) | [OFL](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 374.9KB | 162KB |
| [IconifyBundle.Wi](https://www.nuget.org/packages/IconifyBundle.Wi) | [wi](https://icon-sets.iconify.design/wi/) | [OFL](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 97.8KB | 25.5KB |
| [IconifyBundle.Wpf](https://www.nuget.org/packages/IconifyBundle.Wpf) | [wpf](https://icon-sets.iconify.design/wpf/) | [MIT](https://opensource.org/license/mit) | 73.3KB | 20KB |
| [IconifyBundle.Zmdi](https://www.nuget.org/packages/IconifyBundle.Zmdi) | [zmdi](https://icon-sets.iconify.design/zmdi/) | [OFL](https://openfontlicense.org/) | 99.7KB | 66.5KB |
| [IconifyBundle.Zondicons](https://www.nuget.org/packages/IconifyBundle.Zondicons) | [zondicons](https://icon-sets.iconify.design/zondicons/) | [MIT](https://github.com/dukestreetstudio/zondicons/blob/master/LICENSE) | 37.6KB | 28KB |
<!-- endInclude -->


## Icon

[Pattern](https://thenounproject.com/icon/pattern-42427/) designed by [gira Park](https://thenounproject.com/creator/gila.bag) from [The Noun Project](https://thenounproject.com).
