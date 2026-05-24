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


## NuGet packages

One NuGet per Iconify pack. The list is generated when the packs are built.

> **Note:** some Iconify packs are not published because their license is incompatible with redistribution in a public, commercially-consumable NuGet:<!-- include: packs. path: /src/packs.include.md -->
> - **Non-commercial (CC BY-NC*)**: [cbi](https://icon-sets.iconify.design/cbi/), [ps](https://icon-sets.iconify.design/ps/)
> - **Copyleft (GPL)**: [dashicons](https://icon-sets.iconify.design/dashicons/), [et](https://icon-sets.iconify.design/et/), [gala](https://icon-sets.iconify.design/gala/), [gridicons](https://icon-sets.iconify.design/gridicons/), [icomoon-free](https://icon-sets.iconify.design/icomoon-free/), [wordpress](https://icon-sets.iconify.design/wordpress/)

| Package | Iconify | License | NuGet size | Assembly size |
|---|---|---|--:|--:|
| [IconifyBundle.Academicons](https://www.nuget.org/packages/IconifyBundle.Academicons) | [academicons](https://icon-sets.iconify.design/academicons/) | [Open Font License](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 295 KB | 307 KB |
| [IconifyBundle.AkarIcons](https://www.nuget.org/packages/IconifyBundle.AkarIcons) | [akar-icons](https://icon-sets.iconify.design/akar-icons/) | [MIT](https://github.com/artcoholic/akar-icons/blob/master/LICENSE) | 240.2 KB | 258.5 KB |
| [IconifyBundle.AntDesign](https://www.nuget.org/packages/IconifyBundle.AntDesign) | [ant-design](https://icon-sets.iconify.design/ant-design/) | [MIT](https://github.com/ant-design/ant-design-icons/blob/master/LICENSE) | 617.6 KB | 727 KB |
| [IconifyBundle.Arcticons](https://www.nuget.org/packages/IconifyBundle.Arcticons) | [arcticons](https://icon-sets.iconify.design/arcticons/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 10.4 MB | 14.4 MB |
| [IconifyBundle.Basil](https://www.nuget.org/packages/IconifyBundle.Basil) | [basil](https://icon-sets.iconify.design/basil/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 346.6 KB | 409.5 KB |
| [IconifyBundle.Bi](https://www.nuget.org/packages/IconifyBundle.Bi) | [bi](https://icon-sets.iconify.design/bi/) | [MIT](https://github.com/twbs/icons/blob/main/LICENSE.md) | 1.1 MB | 1.3 MB |
| [IconifyBundle.BitcoinIcons](https://www.nuget.org/packages/IconifyBundle.BitcoinIcons) | [bitcoin-icons](https://icon-sets.iconify.design/bitcoin-icons/) | [MIT](https://github.com/BitcoinDesign/Bitcoin-Icons/blob/main/LICENSE-MIT) | 175.6 KB | 202.5 KB |
| [IconifyBundle.Boxicons](https://www.nuget.org/packages/IconifyBundle.Boxicons) | [boxicons](https://icon-sets.iconify.design/boxicons/) | [MIT](https://github.com/box-icons/boxicons-core/blob/main/LICENSE) | 1.8 MB | 1.8 MB |
| [IconifyBundle.Bpmn](https://www.nuget.org/packages/IconifyBundle.Bpmn) | [bpmn](https://icon-sets.iconify.design/bpmn/) | [Open Font License](https://github.com/bpmn-io/bpmn-font/blob/master/LICENSE) | 237.9 KB | 296.5 KB |
| [IconifyBundle.Brandico](https://www.nuget.org/packages/IconifyBundle.Brandico) | [brandico](https://icon-sets.iconify.design/brandico/) | [CC BY SA](https://creativecommons.org/licenses/by-sa/3.0/) | 68.9 KB | 66.5 KB |
| [IconifyBundle.Bx](https://www.nuget.org/packages/IconifyBundle.Bx) | [bx](https://icon-sets.iconify.design/bx/) | [MIT](https://github.com/box-icons/boxicons/blob/main/LICENSE) | 858.8 KB | 832 KB |
| [IconifyBundle.Bxl](https://www.nuget.org/packages/IconifyBundle.Bxl) | [bxl](https://icon-sets.iconify.design/bxl/) | [MIT](https://github.com/box-icons/boxicons-core/blob/main/LICENSE) | 295.9 KB | 296.5 KB |
| [IconifyBundle.Bxs](https://www.nuget.org/packages/IconifyBundle.Bxs) | [bxs](https://icon-sets.iconify.design/bxs/) | [MIT](https://github.com/box-icons/boxicons/blob/main/LICENSE) | 306 KB | 275.5 KB |
| [IconifyBundle.Bytesize](https://www.nuget.org/packages/IconifyBundle.Bytesize) | [bytesize](https://icon-sets.iconify.design/bytesize/) | [MIT](https://github.com/danklammer/bytesize-icons/blob/master/LICENSE.md) | 40.4 KB | 39 KB |
| [IconifyBundle.Carbon](https://www.nuget.org/packages/IconifyBundle.Carbon) | [carbon](https://icon-sets.iconify.design/carbon/) | Apache 2.0 | 1.4 MB | 1.4 MB |
| [IconifyBundle.Catppuccin](https://www.nuget.org/packages/IconifyBundle.Catppuccin) | [catppuccin](https://icon-sets.iconify.design/catppuccin/) | [MIT](https://github.com/catppuccin/vscode-icons/blob/main/LICENSE) | 341.3 KB | 415.5 KB |
| [IconifyBundle.Charm](https://www.nuget.org/packages/IconifyBundle.Charm) | [charm](https://icon-sets.iconify.design/charm/) | [MIT](https://github.com/jaynewey/charm-icons/blob/main/LICENSE) | 103.1 KB | 117 KB |
| [IconifyBundle.Ci](https://www.nuget.org/packages/IconifyBundle.Ci) | [ci](https://icon-sets.iconify.design/ci/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 346 KB | 353.5 KB |
| [IconifyBundle.Cib](https://www.nuget.org/packages/IconifyBundle.Cib) | [cib](https://icon-sets.iconify.design/cib/) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 1.1 MB | 1.2 MB |
| [IconifyBundle.Cif](https://www.nuget.org/packages/IconifyBundle.Cif) | [cif](https://icon-sets.iconify.design/cif/) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 2 MB | 3.7 MB |
| [IconifyBundle.Cil](https://www.nuget.org/packages/IconifyBundle.Cil) | [cil](https://icon-sets.iconify.design/cil/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 336.5 KB | 333 KB |
| [IconifyBundle.CircleFlags](https://www.nuget.org/packages/IconifyBundle.CircleFlags) | [circle-flags](https://icon-sets.iconify.design/circle-flags/) | [MIT](https://github.com/HatScripts/circle-flags/blob/gh-pages/LICENSE) | 477.4 KB | 715 KB |
| [IconifyBundle.Circum](https://www.nuget.org/packages/IconifyBundle.Circum) | [circum](https://icon-sets.iconify.design/circum/) | [Mozilla Public License 2.0](https://github.com/Klarr-Agency/Circum-Icons/blob/main/LICENSE) | 188.8 KB | 215 KB |
| [IconifyBundle.Clarity](https://www.nuget.org/packages/IconifyBundle.Clarity) | [clarity](https://icon-sets.iconify.design/clarity/) | [MIT](https://github.com/vmware/clarity-assets/blob/master/LICENSE) | 674.1 KB | 1010.5 KB |
| [IconifyBundle.Codex](https://www.nuget.org/packages/IconifyBundle.Codex) | [codex](https://icon-sets.iconify.design/codex/) | [MIT](https://github.com/codex-team/icons/blob/master/LICENSE) | 38.6 KB | 37.5 KB |
| [IconifyBundle.Codicon](https://www.nuget.org/packages/IconifyBundle.Codicon) | [codicon](https://icon-sets.iconify.design/codicon/) | [CC BY 4.0](https://github.com/microsoft/vscode-codicons/blob/main/LICENSE) | 384.4 KB | 400 KB |
| [IconifyBundle.Covid](https://www.nuget.org/packages/IconifyBundle.Covid) | [covid](https://icon-sets.iconify.design/covid/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 109.1 KB | 134.5 KB |
| [IconifyBundle.Cryptocurrency](https://www.nuget.org/packages/IconifyBundle.Cryptocurrency) | [cryptocurrency](https://icon-sets.iconify.design/cryptocurrency/) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 619.2 KB | 679 KB |
| [IconifyBundle.CryptocurrencyColor](https://www.nuget.org/packages/IconifyBundle.CryptocurrencyColor) | [cryptocurrency-color](https://icon-sets.iconify.design/cryptocurrency-color/) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | 621 KB | 719 KB |
| [IconifyBundle.Cuida](https://www.nuget.org/packages/IconifyBundle.Cuida) | [cuida](https://icon-sets.iconify.design/cuida/) | [Apache 2.0](https://github.com/Sysvale/cuida-icons/blob/main/LICENSE) | 151.4 KB | 183 KB |
| [IconifyBundle.Devicon](https://www.nuget.org/packages/IconifyBundle.Devicon) | [devicon](https://icon-sets.iconify.design/devicon/) | [MIT](https://github.com/devicons/devicon/blob/master/LICENSE) | 3.7 MB | 5.3 MB |
| [IconifyBundle.DeviconPlain](https://www.nuget.org/packages/IconifyBundle.DeviconPlain) | [devicon-plain](https://icon-sets.iconify.design/devicon-plain/) | [MIT](https://github.com/devicons/devicon/blob/master/LICENSE) | 2.3 MB | 2.6 MB |
| [IconifyBundle.DinkieIcons](https://www.nuget.org/packages/IconifyBundle.DinkieIcons) | [dinkie-icons](https://icon-sets.iconify.design/dinkie-icons/) | [MIT](https://github.com/atelier-anchor/dinkie-icons/blob/main/LICENSE) | 458.4 KB | 379 KB |
| [IconifyBundle.DuoIcons](https://www.nuget.org/packages/IconifyBundle.DuoIcons) | [duo-icons](https://icon-sets.iconify.design/duo-icons/) | [MIT](https://github.com/fazdiu/duo-icons/blob/master/LICENSE) | 66.3 KB | 75.5 KB |
| [IconifyBundle.Ei](https://www.nuget.org/packages/IconifyBundle.Ei) | [ei](https://icon-sets.iconify.design/ei/) | [MIT](https://github.com/evil-icons/evil-icons/blob/master/LICENSE.txt) | 44.1 KB | 45.5 KB |
| [IconifyBundle.El](https://www.nuget.org/packages/IconifyBundle.El) | [el](https://icon-sets.iconify.design/el/) | [Open Font License](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 234.5 KB | 210.5 KB |
| [IconifyBundle.Emojione](https://www.nuget.org/packages/IconifyBundle.Emojione) | [emojione](https://icon-sets.iconify.design/emojione/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 2.2 MB | 3.5 MB |
| [IconifyBundle.EmojioneMonotone](https://www.nuget.org/packages/IconifyBundle.EmojioneMonotone) | [emojione-monotone](https://icon-sets.iconify.design/emojione-monotone/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 2.6 MB | 2.9 MB |
| [IconifyBundle.EmojioneV1](https://www.nuget.org/packages/IconifyBundle.EmojioneV1) | [emojione-v1](https://icon-sets.iconify.design/emojione-v1/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 7.7 MB | 11.2 MB |
| [IconifyBundle.Entypo](https://www.nuget.org/packages/IconifyBundle.Entypo) | [entypo](https://icon-sets.iconify.design/entypo/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 185.7 KB | 165 KB |
| [IconifyBundle.EntypoSocial](https://www.nuget.org/packages/IconifyBundle.EntypoSocial) | [entypo-social](https://icon-sets.iconify.design/entypo-social/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 70.4 KB | 64 KB |
| [IconifyBundle.EosIcons](https://www.nuget.org/packages/IconifyBundle.EosIcons) | [eos-icons](https://icon-sets.iconify.design/eos-icons/) | [MIT](https://gitlab.com/SUSE-UIUX/eos-icons/-/blob/master/LICENSE) | 153.5 KB | 175.5 KB |
| [IconifyBundle.Ep](https://www.nuget.org/packages/IconifyBundle.Ep) | [ep](https://icon-sets.iconify.design/ep/) | [MIT](https://github.com/element-plus/element-plus-icons/blob/main/packages/svg/package.json) | 151 KB | 146 KB |
| [IconifyBundle.Eva](https://www.nuget.org/packages/IconifyBundle.Eva) | [eva](https://icon-sets.iconify.design/eva/) | [MIT](https://github.com/akveo/eva-icons/blob/master/LICENSE.txt) | 224.5 KB | 245.5 KB |
| [IconifyBundle.F7](https://www.nuget.org/packages/IconifyBundle.F7) | [f7](https://icon-sets.iconify.design/f7/) | [MIT](https://github.com/framework7io/framework7-icons/blob/master/LICENSE) | 1 MB | 1.2 MB |
| [IconifyBundle.Fa](https://www.nuget.org/packages/IconifyBundle.Fa) | [fa](https://icon-sets.iconify.design/fa/) | [Open Font License](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 461 KB | 449.5 KB |
| [IconifyBundle.Fa6Brands](https://www.nuget.org/packages/IconifyBundle.Fa6Brands) | [fa6-brands](https://icon-sets.iconify.design/fa6-brands/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 549.1 KB | 540 KB |
| [IconifyBundle.Fa6Regular](https://www.nuget.org/packages/IconifyBundle.Fa6Regular) | [fa6-regular](https://icon-sets.iconify.design/fa6-regular/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 118.3 KB | 126 KB |
| [IconifyBundle.Fa6Solid](https://www.nuget.org/packages/IconifyBundle.Fa6Solid) | [fa6-solid](https://icon-sets.iconify.design/fa6-solid/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 921.7 KB | 979.5 KB |
| [IconifyBundle.Fa7Brands](https://www.nuget.org/packages/IconifyBundle.Fa7Brands) | [fa7-brands](https://icon-sets.iconify.design/fa7-brands/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 627.4 KB | 633.5 KB |
| [IconifyBundle.Fa7Regular](https://www.nuget.org/packages/IconifyBundle.Fa7Regular) | [fa7-regular](https://icon-sets.iconify.design/fa7-regular/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 182 KB | 205 KB |
| [IconifyBundle.Fa7Solid](https://www.nuget.org/packages/IconifyBundle.Fa7Solid) | [fa7-solid](https://icon-sets.iconify.design/fa7-solid/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1.2 MB | 1.3 MB |
| [IconifyBundle.FaBrands](https://www.nuget.org/packages/IconifyBundle.FaBrands) | [fa-brands](https://icon-sets.iconify.design/fa-brands/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 506.5 KB | 498 KB |
| [IconifyBundle.Fad](https://www.nuget.org/packages/IconifyBundle.Fad) | [fad](https://icon-sets.iconify.design/fad/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 156.3 KB | 165.5 KB |
| [IconifyBundle.Famicons](https://www.nuget.org/packages/IconifyBundle.Famicons) | [famicons](https://icon-sets.iconify.design/famicons/) | [MIT](https://github.com/familyjs/famicons/blob/main/LICENSE) | 816.8 KB | 927.5 KB |
| [IconifyBundle.FaRegular](https://www.nuget.org/packages/IconifyBundle.FaRegular) | [fa-regular](https://icon-sets.iconify.design/fa-regular/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 114.8 KB | 120.5 KB |
| [IconifyBundle.FaSolid](https://www.nuget.org/packages/IconifyBundle.FaSolid) | [fa-solid](https://icon-sets.iconify.design/fa-solid/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 708.6 KB | 736 KB |
| [IconifyBundle.Fe](https://www.nuget.org/packages/IconifyBundle.Fe) | [fe](https://icon-sets.iconify.design/fe/) | [MIT](https://github.com/feathericon/feathericon/blob/master/LICENSE) | 114.2 KB | 100 KB |
| [IconifyBundle.Feather](https://www.nuget.org/packages/IconifyBundle.Feather) | [feather](https://icon-sets.iconify.design/feather/) | [MIT](https://github.com/feathericons/feather/blob/master/LICENSE) | 109.3 KB | 117 KB |
| [IconifyBundle.FileIcons](https://www.nuget.org/packages/IconifyBundle.FileIcons) | [file-icons](https://icon-sets.iconify.design/file-icons/) | [ISC](https://github.com/file-icons/icons/blob/master/LICENSE.md) | 1.3 MB | 1.3 MB |
| [IconifyBundle.Flag](https://www.nuget.org/packages/IconifyBundle.Flag) | [flag](https://icon-sets.iconify.design/flag/) | [MIT](https://github.com/lipis/flag-icons/blob/main/LICENSE) | 2.5 MB | 4.4 MB |
| [IconifyBundle.Flagpack](https://www.nuget.org/packages/IconifyBundle.Flagpack) | [flagpack](https://icon-sets.iconify.design/flagpack/) | [MIT](https://github.com/Yummygum/flagpack-core/blob/main/LICENSE) | 673.5 KB | 1007.5 KB |
| [IconifyBundle.FlatColorIcons](https://www.nuget.org/packages/IconifyBundle.FlatColorIcons) | [flat-color-icons](https://icon-sets.iconify.design/flat-color-icons/) | MIT | 195.8 KB | 230 KB |
| [IconifyBundle.FlatUi](https://www.nuget.org/packages/IconifyBundle.FlatUi) | [flat-ui](https://icon-sets.iconify.design/flat-ui/) | [MIT](https://github.com/designmodo/Flat-UI/blob/master/LICENSE) | 136.6 KB | 202 KB |
| [IconifyBundle.Flowbite](https://www.nuget.org/packages/IconifyBundle.Flowbite) | [flowbite](https://icon-sets.iconify.design/flowbite/) | [MIT](https://github.com/themesberg/flowbite-icons/blob/main/LICENSE) | 421.7 KB | 443.5 KB |
| [IconifyBundle.Fluent](https://www.nuget.org/packages/IconifyBundle.Fluent) | [fluent](https://icon-sets.iconify.design/fluent/) | [MIT](https://github.com/microsoft/fluentui-system-icons/blob/main/LICENSE) | 11.8 MB | 14.2 MB |
| [IconifyBundle.FluentColor](https://www.nuget.org/packages/IconifyBundle.FluentColor) | [fluent-color](https://icon-sets.iconify.design/fluent-color/) | [MIT](https://github.com/microsoft/fluentui-system-icons/blob/main/LICENSE) | 967.7 KB | 2.3 MB |
| [IconifyBundle.FluentEmoji](https://www.nuget.org/packages/IconifyBundle.FluentEmoji) | [fluent-emoji](https://icon-sets.iconify.design/fluent-emoji/) | [MIT](https://github.com/microsoft/fluentui-emoji/blob/main/LICENSE) | 31.5 MB | 134 MB |
| [IconifyBundle.FluentEmojiFlat](https://www.nuget.org/packages/IconifyBundle.FluentEmojiFlat) | [fluent-emoji-flat](https://icon-sets.iconify.design/fluent-emoji-flat/) | [MIT](https://github.com/microsoft/fluentui-emoji/blob/main/LICENSE) | 5.4 MB | 9.8 MB |
| [IconifyBundle.FluentEmojiHighContrast](https://www.nuget.org/packages/IconifyBundle.FluentEmojiHighContrast) | [fluent-emoji-high-contrast](https://icon-sets.iconify.design/fluent-emoji-high-contrast/) | [MIT](https://github.com/microsoft/fluentui-emoji/blob/main/LICENSE) | 2.5 MB | 2.9 MB |
| [IconifyBundle.FluentMdl2](https://www.nuget.org/packages/IconifyBundle.FluentMdl2) | [fluent-mdl2](https://icon-sets.iconify.design/fluent-mdl2/) | [MIT](https://github.com/microsoft/fluentui/blob/master/packages/react-icons-mdl2/LICENSE) | 1019.7 KB | 962.5 KB |
| [IconifyBundle.Fontelico](https://www.nuget.org/packages/IconifyBundle.Fontelico) | [fontelico](https://icon-sets.iconify.design/fontelico/) | [CC BY SA](https://creativecommons.org/licenses/by-sa/3.0/) | 55.2 KB | 57.5 KB |
| [IconifyBundle.Fontisto](https://www.nuget.org/packages/IconifyBundle.Fontisto) | [fontisto](https://icon-sets.iconify.design/fontisto/) | [MIT](https://github.com/kenangundogan/fontisto/blob/master/LICENSE) | 745.9 KB | 835 KB |
| [IconifyBundle.Formkit](https://www.nuget.org/packages/IconifyBundle.Formkit) | [formkit](https://icon-sets.iconify.design/formkit/) | [MIT](https://github.com/formkit/formkit/blob/master/packages/icons/LICENSE) | 95.9 KB | 105 KB |
| [IconifyBundle.Foundation](https://www.nuget.org/packages/IconifyBundle.Foundation) | [foundation](https://icon-sets.iconify.design/foundation/) | MIT | 258.3 KB | 278 KB |
| [IconifyBundle.Fxemoji](https://www.nuget.org/packages/IconifyBundle.Fxemoji) | [fxemoji](https://icon-sets.iconify.design/fxemoji/) | [Apache 2.0](https://mozilla.github.io/fxemoji/LICENSE.md) | 2.3 MB | 2.9 MB |
| [IconifyBundle.GameIcons](https://www.nuget.org/packages/IconifyBundle.GameIcons) | [game-icons](https://icon-sets.iconify.design/game-icons/) | [CC BY 3.0](https://github.com/game-icons/icons/blob/master/license.txt) | 6.5 MB | 6.5 MB |
| [IconifyBundle.Garden](https://www.nuget.org/packages/IconifyBundle.Garden) | [garden](https://icon-sets.iconify.design/garden/) | [Apache 2.0](https://github.com/zendeskgarden/svg-icons/blob/main/LICENSE.md) | 528.2 KB | 566 KB |
| [IconifyBundle.Gcp](https://www.nuget.org/packages/IconifyBundle.Gcp) | [gcp](https://icon-sets.iconify.design/gcp/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 166.6 KB | 215 KB |
| [IconifyBundle.Geo](https://www.nuget.org/packages/IconifyBundle.Geo) | [geo](https://icon-sets.iconify.design/geo/) | [MIT](https://github.com/cugos/geoglyphs/blob/main/LICENSE.md) | 41.9 KB | 55 KB |
| [IconifyBundle.Gg](https://www.nuget.org/packages/IconifyBundle.Gg) | [gg](https://icon-sets.iconify.design/gg/) | [MIT](https://github.com/astrit/css.gg/blob/master/LICENSE) | 296.2 KB | 289 KB |
| [IconifyBundle.Gis](https://www.nuget.org/packages/IconifyBundle.Gis) | [gis](https://icon-sets.iconify.design/gis/) | [CC BY 4.0](https://github.com/Viglino/font-gis/blob/main/LICENSE-CC-BY.md) | 829 KB | 957.5 KB |
| [IconifyBundle.Glyphs](https://www.nuget.org/packages/IconifyBundle.Glyphs) | [glyphs](https://icon-sets.iconify.design/glyphs/) | [MIT](https://github.com/gorango/glyphs/blob/main/license) | 2.5 MB | 3.9 MB |
| [IconifyBundle.GlyphsPoly](https://www.nuget.org/packages/IconifyBundle.GlyphsPoly) | [glyphs-poly](https://icon-sets.iconify.design/glyphs-poly/) | [MIT](https://github.com/gorango/glyphs/blob/main/license) | 848.4 KB | 1.2 MB |
| [IconifyBundle.GravityUi](https://www.nuget.org/packages/IconifyBundle.GravityUi) | [gravity-ui](https://icon-sets.iconify.design/gravity-ui/) | [MIT](https://github.com/gravity-ui/icons/blob/main/LICENSE) | 467.8 KB | 532 KB |
| [IconifyBundle.GrommetIcons](https://www.nuget.org/packages/IconifyBundle.GrommetIcons) | [grommet-icons](https://icon-sets.iconify.design/grommet-icons/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 332.8 KB | 306.5 KB |
| [IconifyBundle.Guidance](https://www.nuget.org/packages/IconifyBundle.Guidance) | [guidance](https://icon-sets.iconify.design/guidance/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 199.3 KB | 180 KB |
| [IconifyBundle.Healthicons](https://www.nuget.org/packages/IconifyBundle.Healthicons) | [healthicons](https://icon-sets.iconify.design/healthicons/) | [MIT](https://github.com/resolvetosavelives/healthicons/blob/main/LICENSE) | 2.6 MB | 3.3 MB |
| [IconifyBundle.Heroicons](https://www.nuget.org/packages/IconifyBundle.Heroicons) | [heroicons](https://icon-sets.iconify.design/heroicons/) | [MIT](https://github.com/tailwindlabs/heroicons/blob/master/LICENSE) | 678.3 KB | 787.5 KB |
| [IconifyBundle.HeroiconsOutline](https://www.nuget.org/packages/IconifyBundle.HeroiconsOutline) | [heroicons-outline](https://icon-sets.iconify.design/heroicons-outline/) | [MIT](https://github.com/tailwindlabs/heroicons/blob/master/LICENSE) | 175.7 KB | 186 KB |
| [IconifyBundle.HeroiconsSolid](https://www.nuget.org/packages/IconifyBundle.HeroiconsSolid) | [heroicons-solid](https://icon-sets.iconify.design/heroicons-solid/) | [MIT](https://github.com/tailwindlabs/heroicons/blob/master/LICENSE) | 190 KB | 200 KB |
| [IconifyBundle.Hugeicons](https://www.nuget.org/packages/IconifyBundle.Hugeicons) | [hugeicons](https://icon-sets.iconify.design/hugeicons/) | MIT | 3.1 MB | 3.6 MB |
| [IconifyBundle.Humbleicons](https://www.nuget.org/packages/IconifyBundle.Humbleicons) | [humbleicons](https://icon-sets.iconify.design/humbleicons/) | [MIT](https://github.com/zraly/humbleicons/blob/master/license) | 121.3 KB | 121 KB |
| [IconifyBundle.Ic](https://www.nuget.org/packages/IconifyBundle.Ic) | [ic](https://icon-sets.iconify.design/ic/) | [Apache 2.0](https://github.com/material-icons/material-icons/blob/master/LICENSE) | 5.5 MB | 5.4 MB |
| [IconifyBundle.Iconamoon](https://www.nuget.org/packages/IconifyBundle.Iconamoon) | [iconamoon](https://icon-sets.iconify.design/iconamoon/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 732.7 KB | 923.5 KB |
| [IconifyBundle.Iconoir](https://www.nuget.org/packages/IconifyBundle.Iconoir) | [iconoir](https://icon-sets.iconify.design/iconoir/) | [MIT](https://github.com/iconoir-icons/iconoir/blob/main/LICENSE) | 775.8 KB | 893.5 KB |
| [IconifyBundle.IconPark](https://www.nuget.org/packages/IconifyBundle.IconPark) | [icon-park](https://icon-sets.iconify.design/icon-park/) | [Apache 2.0](https://github.com/bytedance/IconPark/blob/master/LICENSE) | 1.5 MB | 2.1 MB |
| [IconifyBundle.IconParkOutline](https://www.nuget.org/packages/IconifyBundle.IconParkOutline) | [icon-park-outline](https://icon-sets.iconify.design/icon-park-outline/) | [Apache 2.0](https://github.com/bytedance/IconPark/blob/master/LICENSE) | 1.2 MB | 1.4 MB |
| [IconifyBundle.IconParkSolid](https://www.nuget.org/packages/IconifyBundle.IconParkSolid) | [icon-park-solid](https://icon-sets.iconify.design/icon-park-solid/) | [Apache 2.0](https://github.com/bytedance/IconPark/blob/master/LICENSE) | 1 MB | 1.4 MB |
| [IconifyBundle.IconParkTwotone](https://www.nuget.org/packages/IconifyBundle.IconParkTwotone) | [icon-park-twotone](https://icon-sets.iconify.design/icon-park-twotone/) | [Apache 2.0](https://github.com/bytedance/IconPark/blob/master/LICENSE) | 1.1 MB | 1.5 MB |
| [IconifyBundle.Icons8](https://www.nuget.org/packages/IconifyBundle.Icons8) | [icons8](https://icon-sets.iconify.design/icons8/) | MIT | 136.6 KB | 119.5 KB |
| [IconifyBundle.Il](https://www.nuget.org/packages/IconifyBundle.Il) | [il](https://icon-sets.iconify.design/il/) | MIT | 49.3 KB | 41 KB |
| [IconifyBundle.Ion](https://www.nuget.org/packages/IconifyBundle.Ion) | [ion](https://icon-sets.iconify.design/ion/) | [MIT](https://github.com/ionic-team/ionicons/blob/main/LICENSE) | 1.5 MB | 1.7 MB |
| [IconifyBundle.Iwwa](https://www.nuget.org/packages/IconifyBundle.Iwwa) | [iwwa](https://icon-sets.iconify.design/iwwa/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 125.4 KB | 147.5 KB |
| [IconifyBundle.Ix](https://www.nuget.org/packages/IconifyBundle.Ix) | [ix](https://icon-sets.iconify.design/ix/) | [MIT](https://github.com/siemens/ix-icons/blob/main/LICENSE.md) | 937.5 KB | 972.5 KB |
| [IconifyBundle.Jam](https://www.nuget.org/packages/IconifyBundle.Jam) | [jam](https://icon-sets.iconify.design/jam/) | [MIT](https://github.com/cyberalien/jam-backup/blob/main/LICENSE) | 482.5 KB | 491 KB |
| [IconifyBundle.La](https://www.nuget.org/packages/IconifyBundle.La) | [la](https://icon-sets.iconify.design/la/) | [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) | 1.1 MB | 1.1 MB |
| [IconifyBundle.LetsIcons](https://www.nuget.org/packages/IconifyBundle.LetsIcons) | [lets-icons](https://icon-sets.iconify.design/lets-icons/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 912 KB | 1.1 MB |
| [IconifyBundle.Lineicons](https://www.nuget.org/packages/IconifyBundle.Lineicons) | [lineicons](https://icon-sets.iconify.design/lineicons/) | [MIT](https://github.com/LineiconsHQ/Lineicons/blob/main/LICENSE.md) | 808.8 KB | 877 KB |
| [IconifyBundle.LineMd](https://www.nuget.org/packages/IconifyBundle.LineMd) | [line-md](https://icon-sets.iconify.design/line-md/) | [MIT](https://github.com/cyberalien/line-md/blob/main/license.txt) | 776.9 KB | 2.1 MB |
| [IconifyBundle.Logos](https://www.nuget.org/packages/IconifyBundle.Logos) | [logos](https://icon-sets.iconify.design/logos/) | [CC0](https://raw.githubusercontent.com/gilbarbara/logos/master/LICENSE.txt) | 5.8 MB | 7.7 MB |
| [IconifyBundle.Ls](https://www.nuget.org/packages/IconifyBundle.Ls) | [ls](https://icon-sets.iconify.design/ls/) | [Open Font License](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 234.5 KB | 211.5 KB |
| [IconifyBundle.Lsicon](https://www.nuget.org/packages/IconifyBundle.Lsicon) | [lsicon](https://icon-sets.iconify.design/lsicon/) | [MIT](https://github.com/wisdesignsystem/lsicon/blob/main/LICENSE) | 320.5 KB | 304.5 KB |
| [IconifyBundle.Lucide](https://www.nuget.org/packages/IconifyBundle.Lucide) | [lucide](https://icon-sets.iconify.design/lucide/) | [ISC](https://github.com/lucide-icons/lucide/blob/main/LICENSE) | 713.9 KB | 827 KB |
| [IconifyBundle.LucideLab](https://www.nuget.org/packages/IconifyBundle.LucideLab) | [lucide-lab](https://icon-sets.iconify.design/lucide-lab/) | [ISC](https://github.com/lucide-icons/lucide-lab/blob/main/LICENSE) | 168.5 KB | 190 KB |
| [IconifyBundle.Mage](https://www.nuget.org/packages/IconifyBundle.Mage) | [mage](https://icon-sets.iconify.design/mage/) | [Apache 2.0](https://github.com/Mage-Icons/mage-icons/blob/main/License.txt) | 618.7 KB | 717 KB |
| [IconifyBundle.Majesticons](https://www.nuget.org/packages/IconifyBundle.Majesticons) | [majesticons](https://icon-sets.iconify.design/majesticons/) | [MIT](https://github.com/halfmage/majesticons/blob/main/LICENSE) | 485.3 KB | 557.5 KB |
| [IconifyBundle.Maki](https://www.nuget.org/packages/IconifyBundle.Maki) | [maki](https://icon-sets.iconify.design/maki/) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | 237.3 KB | 222 KB |
| [IconifyBundle.Map](https://www.nuget.org/packages/IconifyBundle.Map) | [map](https://icon-sets.iconify.design/map/) | [Open Font License](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 159.2 KB | 152 KB |
| [IconifyBundle.Marketeq](https://www.nuget.org/packages/IconifyBundle.Marketeq) | [marketeq](https://icon-sets.iconify.design/marketeq/) | MIT | 284 KB | 361.5 KB |
| [IconifyBundle.MaterialIconTheme](https://www.nuget.org/packages/IconifyBundle.MaterialIconTheme) | [material-icon-theme](https://icon-sets.iconify.design/material-icon-theme/) | [MIT](https://github.com/material-extensions/vscode-material-icon-theme/blob/main/LICENSE) | 882.5 KB | 1 MB |
| [IconifyBundle.MaterialSymbols](https://www.nuget.org/packages/IconifyBundle.MaterialSymbols) | [material-symbols](https://icon-sets.iconify.design/material-symbols/) | [Apache 2.0](https://github.com/google/material-design-icons/blob/master/LICENSE) | 8.4 MB | 9.4 MB |
| [IconifyBundle.MaterialSymbolsLight](https://www.nuget.org/packages/IconifyBundle.MaterialSymbolsLight) | [material-symbols-light](https://icon-sets.iconify.design/material-symbols-light/) | [Apache 2.0](https://github.com/google/material-design-icons/blob/master/LICENSE) | 9.7 MB | 11.2 MB |
| [IconifyBundle.Mdi](https://www.nuget.org/packages/IconifyBundle.Mdi) | [mdi](https://icon-sets.iconify.design/mdi/) | [Apache 2.0](https://github.com/Templarian/MaterialDesign/blob/master/LICENSE) | 3.6 MB | 3.4 MB |
| [IconifyBundle.MdiLight](https://www.nuget.org/packages/IconifyBundle.MdiLight) | [mdi-light](https://icon-sets.iconify.design/mdi-light/) | [Open Font License](https://github.com/Templarian/MaterialDesignLight/blob/master/LICENSE.md) | 135.7 KB | 119.5 KB |
| [IconifyBundle.MedicalIcon](https://www.nuget.org/packages/IconifyBundle.MedicalIcon) | [medical-icon](https://icon-sets.iconify.design/medical-icon/) | [MIT](https://github.com/samcome/webfont-medical-icons/blob/master/LICENSE) | 205.1 KB | 222 KB |
| [IconifyBundle.Memory](https://www.nuget.org/packages/IconifyBundle.Memory) | [memory](https://icon-sets.iconify.design/memory/) | [Apache 2.0](https://github.com/Pictogrammers/Memory/blob/main/LICENSE) | 251.4 KB | 242.5 KB |
| [IconifyBundle.Meteocons](https://www.nuget.org/packages/IconifyBundle.Meteocons) | [meteocons](https://icon-sets.iconify.design/meteocons/) | [MIT](https://github.com/basmilius/weather-icons/blob/dev/LICENSE) | 563.3 KB | 1.7 MB |
| [IconifyBundle.MeteorIcons](https://www.nuget.org/packages/IconifyBundle.MeteorIcons) | [meteor-icons](https://icon-sets.iconify.design/meteor-icons/) | [MIT](https://github.com/zkreations/icons/blob/main/LICENSE) | 117.9 KB | 123 KB |
| [IconifyBundle.Mi](https://www.nuget.org/packages/IconifyBundle.Mi) | [mi](https://icon-sets.iconify.design/mi/) | [MIT](https://github.com/mono-company/mono-icons/blob/master/LICENSE.md) | 85 KB | 81 KB |
| [IconifyBundle.Mingcute](https://www.nuget.org/packages/IconifyBundle.Mingcute) | [mingcute](https://icon-sets.iconify.design/mingcute/) | [Apache 2.0](https://github.com/Richard9394/MingCute/blob/main/LICENSE) | 2.7 MB | 3.9 MB |
| [IconifyBundle.MonoIcons](https://www.nuget.org/packages/IconifyBundle.MonoIcons) | [mono-icons](https://icon-sets.iconify.design/mono-icons/) | [MIT](https://github.com/mono-company/mono-icons/blob/master/LICENSE.md) | 85.1 KB | 81 KB |
| [IconifyBundle.Mynaui](https://www.nuget.org/packages/IconifyBundle.Mynaui) | [mynaui](https://icon-sets.iconify.design/mynaui/) | [MIT](https://github.com/praveenjuge/mynaui-icons/blob/main/LICENSE) | 1.4 MB | 1.7 MB |
| [IconifyBundle.Nimbus](https://www.nuget.org/packages/IconifyBundle.Nimbus) | [nimbus](https://icon-sets.iconify.design/nimbus/) | [MIT](https://github.com/cyberalien/nimbus-icons/blob/main/LICENSE) | 88.4 KB | 90 KB |
| [IconifyBundle.Nonicons](https://www.nuget.org/packages/IconifyBundle.Nonicons) | [nonicons](https://icon-sets.iconify.design/nonicons/) | [MIT](https://github.com/yamatsum/nonicons/blob/master/LICENSE) | 65.1 KB | 69 KB |
| [IconifyBundle.Noto](https://www.nuget.org/packages/IconifyBundle.Noto) | [noto](https://icon-sets.iconify.design/noto/) | [Apache 2.0](https://github.com/googlefonts/noto-emoji/blob/main/svg/LICENSE) | 12.6 MB | 28.9 MB |
| [IconifyBundle.NotoV1](https://www.nuget.org/packages/IconifyBundle.NotoV1) | [noto-v1](https://icon-sets.iconify.design/noto-v1/) | [Apache 2.0](https://github.com/googlefonts/noto-emoji/blob/main/svg/LICENSE) | 5.2 MB | 9.1 MB |
| [IconifyBundle.Nrk](https://www.nuget.org/packages/IconifyBundle.Nrk) | [nrk](https://icon-sets.iconify.design/nrk/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 121.2 KB | 111.5 KB |
| [IconifyBundle.Octicon](https://www.nuget.org/packages/IconifyBundle.Octicon) | [octicon](https://icon-sets.iconify.design/octicon/) | [MIT](https://github.com/primer/octicons/blob/main/LICENSE) | 527 KB | 568 KB |
| [IconifyBundle.Oi](https://www.nuget.org/packages/IconifyBundle.Oi) | [oi](https://icon-sets.iconify.design/oi/) | [MIT](https://github.com/iconic/open-iconic/blob/master/ICON-LICENSE) | 90.5 KB | 71 KB |
| [IconifyBundle.Ooui](https://www.nuget.org/packages/IconifyBundle.Ooui) | [ooui](https://icon-sets.iconify.design/ooui/) | [MIT](https://github.com/wikimedia/oojs-ui/blob/master/LICENSE-MIT) | 177 KB | 159 KB |
| [IconifyBundle.Openmoji](https://www.nuget.org/packages/IconifyBundle.Openmoji) | [openmoji](https://icon-sets.iconify.design/openmoji/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 5.3 MB | 10.5 MB |
| [IconifyBundle.Oui](https://www.nuget.org/packages/IconifyBundle.Oui) | [oui](https://icon-sets.iconify.design/oui/) | [Apache 2.0](https://github.com/opensearch-project/oui/blob/main/LICENSE.txt) | 270.2 KB | 273 KB |
| [IconifyBundle.Pajamas](https://www.nuget.org/packages/IconifyBundle.Pajamas) | [pajamas](https://icon-sets.iconify.design/pajamas/) | [MIT](https://gitlab.com/gitlab-org/gitlab-svgs/-/blob/main/LICENSE) | 222.8 KB | 231 KB |
| [IconifyBundle.Pepicons](https://www.nuget.org/packages/IconifyBundle.Pepicons) | [pepicons](https://icon-sets.iconify.design/pepicons/) | [CC BY 4.0](https://github.com/CyCraft/pepicons/blob/dev/LICENSE) | 349.7 KB | 480.5 KB |
| [IconifyBundle.PepiconsPencil](https://www.nuget.org/packages/IconifyBundle.PepiconsPencil) | [pepicons-pencil](https://icon-sets.iconify.design/pepicons-pencil/) | [CC BY 4.0](https://github.com/CyCraft/pepicons/blob/dev/LICENSE) | 842.7 KB | 1.4 MB |
| [IconifyBundle.PepiconsPop](https://www.nuget.org/packages/IconifyBundle.PepiconsPop) | [pepicons-pop](https://icon-sets.iconify.design/pepicons-pop/) | [CC BY 4.0](https://github.com/CyCraft/pepicons/blob/dev/LICENSE) | 832.3 KB | 1.4 MB |
| [IconifyBundle.PepiconsPrint](https://www.nuget.org/packages/IconifyBundle.PepiconsPrint) | [pepicons-print](https://icon-sets.iconify.design/pepicons-print/) | [CC BY 4.0](https://github.com/CyCraft/pepicons/blob/dev/LICENSE) | 1.1 MB | 2.2 MB |
| [IconifyBundle.Ph](https://www.nuget.org/packages/IconifyBundle.Ph) | [ph](https://icon-sets.iconify.design/ph/) | [MIT](https://github.com/phosphor-icons/core/blob/main/LICENSE) | 4.9 MB | 5.4 MB |
| [IconifyBundle.Picon](https://www.nuget.org/packages/IconifyBundle.Picon) | [picon](https://icon-sets.iconify.design/picon/) | [Open Font License](https://github.com/yne/picon/blob/master/OFL.txt) | 250.2 KB | 166.5 KB |
| [IconifyBundle.Pinhead](https://www.nuget.org/packages/IconifyBundle.Pinhead) | [pinhead](https://icon-sets.iconify.design/pinhead/) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | 1.3 MB | 1.3 MB |
| [IconifyBundle.Pixel](https://www.nuget.org/packages/IconifyBundle.Pixel) | [pixel](https://icon-sets.iconify.design/pixel/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 236 KB | 229 KB |
| [IconifyBundle.Pixelarticons](https://www.nuget.org/packages/IconifyBundle.Pixelarticons) | [pixelarticons](https://icon-sets.iconify.design/pixelarticons/) | [MIT](https://github.com/halfmage/pixelarticons/blob/master/LICENSE) | 395.5 KB | 344 KB |
| [IconifyBundle.Prime](https://www.nuget.org/packages/IconifyBundle.Prime) | [prime](https://icon-sets.iconify.design/prime/) | [MIT](https://github.com/primefaces/primeicons/blob/master/LICENSE) | 176 KB | 192 KB |
| [IconifyBundle.Proicons](https://www.nuget.org/packages/IconifyBundle.Proicons) | [proicons](https://icon-sets.iconify.design/proicons/) | [MIT](https://github.com/ProCode-Software/proicons/blob/main/LICENSE) | 283.2 KB | 332.5 KB |
| [IconifyBundle.QlementineIcons](https://www.nuget.org/packages/IconifyBundle.QlementineIcons) | [qlementine-icons](https://icon-sets.iconify.design/qlementine-icons/) | [MIT](https://github.com/oclero/qlementine-icons/blob/master/LICENSE) | 753.4 KB | 935.5 KB |
| [IconifyBundle.Quill](https://www.nuget.org/packages/IconifyBundle.Quill) | [quill](https://icon-sets.iconify.design/quill/) | [MIT](https://github.com/yourtempo/tempo-quill-icons/blob/main/LICENSE) | 70.6 KB | 72.5 KB |
| [IconifyBundle.RadixIcons](https://www.nuget.org/packages/IconifyBundle.RadixIcons) | [radix-icons](https://icon-sets.iconify.design/radix-icons/) | [MIT](https://github.com/radix-ui/icons/blob/master/LICENSE) | 199.8 KB | 227.5 KB |
| [IconifyBundle.Raphael](https://www.nuget.org/packages/IconifyBundle.Raphael) | [raphael](https://icon-sets.iconify.design/raphael/) | MIT | 233.2 KB | 228 KB |
| [IconifyBundle.Ri](https://www.nuget.org/packages/IconifyBundle.Ri) | [ri](https://icon-sets.iconify.design/ri/) | [Apache 2.0](https://github.com/cyberalien/RemixIcon/blob/master/License) | 1.4 MB | 1.3 MB |
| [IconifyBundle.RivetIcons](https://www.nuget.org/packages/IconifyBundle.RivetIcons) | [rivet-icons](https://icon-sets.iconify.design/rivet-icons/) | [BSD 3-Clause](https://github.com/indiana-university/rivet-icons/blob/develop/LICENSE) | 89 KB | 75 KB |
| [IconifyBundle.Roentgen](https://www.nuget.org/packages/IconifyBundle.Roentgen) | [roentgen](https://icon-sets.iconify.design/roentgen/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 309.8 KB | 339 KB |
| [IconifyBundle.Si](https://www.nuget.org/packages/IconifyBundle.Si) | [si](https://icon-sets.iconify.design/si/) | [MIT](https://github.com/planetabhi/sargam-icons/blob/main/LICENSE.txt) | 641.8 KB | 818.5 KB |
| [IconifyBundle.Sidekickicons](https://www.nuget.org/packages/IconifyBundle.Sidekickicons) | [sidekickicons](https://icon-sets.iconify.design/sidekickicons/) | [MIT](https://github.com/ndri/sidekickicons/blob/master/LICENSE) | 131.5 KB | 153.5 KB |
| [IconifyBundle.SiGlyph](https://www.nuget.org/packages/IconifyBundle.SiGlyph) | [si-glyph](https://icon-sets.iconify.design/si-glyph/) | [CC BY SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 543.8 KB | 533.5 KB |
| [IconifyBundle.SimpleIcons](https://www.nuget.org/packages/IconifyBundle.SimpleIcons) | [simple-icons](https://icon-sets.iconify.design/simple-icons/) | [CC0 1.0](https://github.com/simple-icons/simple-icons/blob/develop/LICENSE.md) | 4.6 MB | 4.8 MB |
| [IconifyBundle.SimpleLineIcons](https://www.nuget.org/packages/IconifyBundle.SimpleLineIcons) | [simple-line-icons](https://icon-sets.iconify.design/simple-line-icons/) | [MIT](https://github.com/thesabbir/simple-line-icons/blob/master/LICENSE.md) | 212.5 KB | 215 KB |
| [IconifyBundle.SkillIcons](https://www.nuget.org/packages/IconifyBundle.SkillIcons) | [skill-icons](https://icon-sets.iconify.design/skill-icons/) | [MIT](https://github.com/tandpfun/skill-icons/blob/main/LICENSE) | 1.2 MB | 1.8 MB |
| [IconifyBundle.Solar](https://www.nuget.org/packages/IconifyBundle.Solar) | [solar](https://icon-sets.iconify.design/solar/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 5.4 MB | 7.5 MB |
| [IconifyBundle.Stash](https://www.nuget.org/packages/IconifyBundle.Stash) | [stash](https://icon-sets.iconify.design/stash/) | [MIT](https://github.com/stash-ui/icons/blob/master/LICENSE) | 905.9 KB | 1.2 MB |
| [IconifyBundle.Streamline](https://www.nuget.org/packages/IconifyBundle.Streamline) | [streamline](https://icon-sets.iconify.design/streamline/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 2.3 MB | 2.5 MB |
| [IconifyBundle.StreamlineBlock](https://www.nuget.org/packages/IconifyBundle.StreamlineBlock) | [streamline-block](https://icon-sets.iconify.design/streamline-block/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 118.6 KB | 102.5 KB |
| [IconifyBundle.StreamlineColor](https://www.nuget.org/packages/IconifyBundle.StreamlineColor) | [streamline-color](https://icon-sets.iconify.design/streamline-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1.1 MB | 1.5 MB |
| [IconifyBundle.StreamlineCyber](https://www.nuget.org/packages/IconifyBundle.StreamlineCyber) | [streamline-cyber](https://icon-sets.iconify.design/streamline-cyber/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 243.9 KB | 264 KB |
| [IconifyBundle.StreamlineCyberColor](https://www.nuget.org/packages/IconifyBundle.StreamlineCyberColor) | [streamline-cyber-color](https://icon-sets.iconify.design/streamline-cyber-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 324 KB | 493 KB |
| [IconifyBundle.StreamlineEmojis](https://www.nuget.org/packages/IconifyBundle.StreamlineEmojis) | [streamline-emojis](https://icon-sets.iconify.design/streamline-emojis/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1.2 MB | 3 MB |
| [IconifyBundle.StreamlineFlex](https://www.nuget.org/packages/IconifyBundle.StreamlineFlex) | [streamline-flex](https://icon-sets.iconify.design/streamline-flex/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1.3 MB | 1.4 MB |
| [IconifyBundle.StreamlineFlexColor](https://www.nuget.org/packages/IconifyBundle.StreamlineFlexColor) | [streamline-flex-color](https://icon-sets.iconify.design/streamline-flex-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 856.3 KB | 1.1 MB |
| [IconifyBundle.StreamlineFreehand](https://www.nuget.org/packages/IconifyBundle.StreamlineFreehand) | [streamline-freehand](https://icon-sets.iconify.design/streamline-freehand/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 2.3 MB | 2.7 MB |
| [IconifyBundle.StreamlineFreehandColor](https://www.nuget.org/packages/IconifyBundle.StreamlineFreehandColor) | [streamline-freehand-color](https://icon-sets.iconify.design/streamline-freehand-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 2.4 MB | 2.8 MB |
| [IconifyBundle.StreamlineKameleonColor](https://www.nuget.org/packages/IconifyBundle.StreamlineKameleonColor) | [streamline-kameleon-color](https://icon-sets.iconify.design/streamline-kameleon-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 473.9 KB | 744 KB |
| [IconifyBundle.StreamlineLogos](https://www.nuget.org/packages/IconifyBundle.StreamlineLogos) | [streamline-logos](https://icon-sets.iconify.design/streamline-logos/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 909.5 KB | 944 KB |
| [IconifyBundle.StreamlinePixel](https://www.nuget.org/packages/IconifyBundle.StreamlinePixel) | [streamline-pixel](https://icon-sets.iconify.design/streamline-pixel/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 487.8 KB | 711 KB |
| [IconifyBundle.StreamlinePlump](https://www.nuget.org/packages/IconifyBundle.StreamlinePlump) | [streamline-plump](https://icon-sets.iconify.design/streamline-plump/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1.6 MB | 1.8 MB |
| [IconifyBundle.StreamlinePlumpColor](https://www.nuget.org/packages/IconifyBundle.StreamlinePlumpColor) | [streamline-plump-color](https://icon-sets.iconify.design/streamline-plump-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1.1 MB | 1.6 MB |
| [IconifyBundle.StreamlineSharp](https://www.nuget.org/packages/IconifyBundle.StreamlineSharp) | [streamline-sharp](https://icon-sets.iconify.design/streamline-sharp/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 762.6 KB | 726.5 KB |
| [IconifyBundle.StreamlineSharpColor](https://www.nuget.org/packages/IconifyBundle.StreamlineSharpColor) | [streamline-sharp-color](https://icon-sets.iconify.design/streamline-sharp-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 503.3 KB | 583.5 KB |
| [IconifyBundle.StreamlineStickiesColor](https://www.nuget.org/packages/IconifyBundle.StreamlineStickiesColor) | [streamline-stickies-color](https://icon-sets.iconify.design/streamline-stickies-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 374.6 KB | 539 KB |
| [IconifyBundle.StreamlineUltimate](https://www.nuget.org/packages/IconifyBundle.StreamlineUltimate) | [streamline-ultimate](https://icon-sets.iconify.design/streamline-ultimate/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 1.3 MB | 1.5 MB |
| [IconifyBundle.StreamlineUltimateColor](https://www.nuget.org/packages/IconifyBundle.StreamlineUltimateColor) | [streamline-ultimate-color](https://icon-sets.iconify.design/streamline-ultimate-color/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 845.3 KB | 1.5 MB |
| [IconifyBundle.Subway](https://www.nuget.org/packages/IconifyBundle.Subway) | [subway](https://icon-sets.iconify.design/subway/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 151.4 KB | 135.5 KB |
| [IconifyBundle.SvgSpinners](https://www.nuget.org/packages/IconifyBundle.SvgSpinners) | [svg-spinners](https://icon-sets.iconify.design/svg-spinners/) | [MIT](https://github.com/n3r4zzurr0/svg-spinners/blob/main/LICENSE) | 33.7 KB | 88 KB |
| [IconifyBundle.SystemUicons](https://www.nuget.org/packages/IconifyBundle.SystemUicons) | [system-uicons](https://icon-sets.iconify.design/system-uicons/) | [Unlicense](https://github.com/CoreyGinnivan/system-uicons/blob/master/LICENSE) | 178.9 KB | 198 KB |
| [IconifyBundle.Tabler](https://www.nuget.org/packages/IconifyBundle.Tabler) | [tabler](https://icon-sets.iconify.design/tabler/) | [MIT](https://github.com/tabler/tabler-icons/blob/master/LICENSE) | 2.6 MB | 2.9 MB |
| [IconifyBundle.Tdesign](https://www.nuget.org/packages/IconifyBundle.Tdesign) | [tdesign](https://icon-sets.iconify.design/tdesign/) | [MIT](https://github.com/Tencent/tdesign-icons/blob/main/LICENSE) | 1.1 MB | 1.1 MB |
| [IconifyBundle.Teenyicons](https://www.nuget.org/packages/IconifyBundle.Teenyicons) | [teenyicons](https://icon-sets.iconify.design/teenyicons/) | [MIT](https://github.com/teenyicons/teenyicons/blob/master/LICENSE) | 572.3 KB | 565.5 KB |
| [IconifyBundle.Temaki](https://www.nuget.org/packages/IconifyBundle.Temaki) | [temaki](https://icon-sets.iconify.design/temaki/) | [CC0](https://creativecommons.org/publicdomain/zero/1.0/) | 369.8 KB | 373 KB |
| [IconifyBundle.Token](https://www.nuget.org/packages/IconifyBundle.Token) | [token](https://icon-sets.iconify.design/token/) | [MIT](https://github.com/0xa3k5/web3icons/blob/main/LICENCE) | 2 MB | 2.2 MB |
| [IconifyBundle.TokenBranded](https://www.nuget.org/packages/IconifyBundle.TokenBranded) | [token-branded](https://icon-sets.iconify.design/token-branded/) | [MIT](https://github.com/0xa3k5/web3icons/blob/main/LICENCE) | 4.4 MB | 6 MB |
| [IconifyBundle.Topcoat](https://www.nuget.org/packages/IconifyBundle.Topcoat) | [topcoat](https://icon-sets.iconify.design/topcoat/) | [Apache 2.0](https://github.com/topcoat/icons/blob/master/LICENSE) | 60.4 KB | 52 KB |
| [IconifyBundle.Twemoji](https://www.nuget.org/packages/IconifyBundle.Twemoji) | [twemoji](https://icon-sets.iconify.design/twemoji/) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) | 6.3 MB | 11.3 MB |
| [IconifyBundle.Typcn](https://www.nuget.org/packages/IconifyBundle.Typcn) | [typcn](https://icon-sets.iconify.design/typcn/) | [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) | 222.6 KB | 221 KB |
| [IconifyBundle.Uil](https://www.nuget.org/packages/IconifyBundle.Uil) | [uil](https://icon-sets.iconify.design/uil/) | [Apache 2.0](https://github.com/Iconscout/unicons/blob/master/LICENSE) | 643.8 KB | 690.5 KB |
| [IconifyBundle.Uim](https://www.nuget.org/packages/IconifyBundle.Uim) | [uim](https://icon-sets.iconify.design/uim/) | [Apache 2.0](https://github.com/Iconscout/unicons/blob/master/LICENSE) | 177.2 KB | 225 KB |
| [IconifyBundle.Uis](https://www.nuget.org/packages/IconifyBundle.Uis) | [uis](https://icon-sets.iconify.design/uis/) | [Apache 2.0](https://github.com/Iconscout/unicons/blob/master/LICENSE) | 89.6 KB | 92 KB |
| [IconifyBundle.Uit](https://www.nuget.org/packages/IconifyBundle.Uit) | [uit](https://icon-sets.iconify.design/uit/) | [Apache 2.0](https://github.com/Iconscout/unicons/blob/master/LICENSE) | 128 KB | 136.5 KB |
| [IconifyBundle.Uiw](https://www.nuget.org/packages/IconifyBundle.Uiw) | [uiw](https://icon-sets.iconify.design/uiw/) | [MIT](https://github.com/uiwjs/icons/blob/master/LICENSE) | 160.6 KB | 158 KB |
| [IconifyBundle.Unjs](https://www.nuget.org/packages/IconifyBundle.Unjs) | [unjs](https://icon-sets.iconify.design/unjs/) | Apache 2.0 | 182.5 KB | 255 KB |
| [IconifyBundle.Vaadin](https://www.nuget.org/packages/IconifyBundle.Vaadin) | [vaadin](https://icon-sets.iconify.design/vaadin/) | Apache 2.0 | 288 KB | 254.5 KB |
| [IconifyBundle.Vs](https://www.nuget.org/packages/IconifyBundle.Vs) | [vs](https://icon-sets.iconify.design/vs/) | [Open Font License](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 124.7 KB | 117.5 KB |
| [IconifyBundle.VscodeIcons](https://www.nuget.org/packages/IconifyBundle.VscodeIcons) | [vscode-icons](https://icon-sets.iconify.design/vscode-icons/) | [MIT](https://github.com/vscode-icons/vscode-icons/blob/master/LICENSE) | 2.6 MB | 4 MB |
| [IconifyBundle.Websymbol](https://www.nuget.org/packages/IconifyBundle.Websymbol) | [websymbol](https://icon-sets.iconify.design/websymbol/) | [Open Font License](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 46.5 KB | 37.5 KB |
| [IconifyBundle.Weui](https://www.nuget.org/packages/IconifyBundle.Weui) | [weui](https://icon-sets.iconify.design/weui/) | MIT | 86.2 KB | 82 KB |
| [IconifyBundle.Whh](https://www.nuget.org/packages/IconifyBundle.Whh) | [whh](https://icon-sets.iconify.design/whh/) | [Open Font License](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 1.3 MB | 1.3 MB |
| [IconifyBundle.Wi](https://www.nuget.org/packages/IconifyBundle.Wi) | [wi](https://icon-sets.iconify.design/wi/) | [Open Font License](https://scripts.sil.org/cms/scripts/page.php?site_id=nrsi&id=OFL) | 256 KB | 302 KB |
| [IconifyBundle.Wpf](https://www.nuget.org/packages/IconifyBundle.Wpf) | [wpf](https://icon-sets.iconify.design/wpf/) | MIT | 166 KB | 167.5 KB |
| [IconifyBundle.Zmdi](https://www.nuget.org/packages/IconifyBundle.Zmdi) | [zmdi](https://icon-sets.iconify.design/zmdi/) | Open Font License | 359.9 KB | 310 KB |
| [IconifyBundle.Zondicons](https://www.nuget.org/packages/IconifyBundle.Zondicons) | [zondicons](https://icon-sets.iconify.design/zondicons/) | [MIT](https://github.com/dukestreetstudio/zondicons/blob/master/LICENSE) | 112.6 KB | 89.5 KB |
<!-- endInclude -->


## Icon

[Pattern](https://thenounproject.com/icon/pattern-42427/) designed by [gira Park](https://thenounproject.com/creator/gila.bag) from [The Noun Project](https://thenounproject.com).


