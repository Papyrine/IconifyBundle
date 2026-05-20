# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

Iconistic provides strongly typed [Iconify](https://iconify.design) icons for .NET. A source
generator reads `[assembly: IconPack(...)]` declarations, downloads the icons from the Iconify API
(caching to disk), and emits a typed `Icons` API. Ships three packages: `Iconistic` (runtime +
generator), `Iconistic.Api` (Iconify API wrapper) and `Iconistic.Blazor` (`<Icon>` component).

## Build & Test

Requires .NET SDK 10 (see `global.json`). From `src/`:

```bash
dotnet build Iconistic.slnx -c Release          # builds all projects, packs nupkgs to ../nugets
dotnet run --project Tests -c Release            # TUnit: generator, runtime, SVG, API
dotnet test Iconistic.Web.Tests -c Release --filter "TestCategory!=Playwright"   # bUnit
```

Integration tests consume the **packed** nupkgs (run after a Release build of `src`):

```bash
cd IntegrationTests
dotnet run --project IntegrationTests -c Release
```

Playwright visual snapshots live in `Iconistic.Web.Tests` (category `Playwright`). They need
`playwright install` and accepted image baselines, so they are excluded from the default run.

There are two test stacks, by design:
- **TUnit** (`Tests`, `IntegrationTests`) — `OutputType=Exe`, run with `dotnet run`.
- **NUnit + bUnit + Playwright + Verify** (`Iconistic.Web.Tests`) — run with `dotnet test`.

`global.json` deliberately does **not** set a `Microsoft.Testing.Platform` runner, so NUnit's VSTest
runner keeps working. Verify snapshots: failures write `.received.*`; accept by renaming to
`.verified.*`.

## Architecture

### `src/Iconistic` (runtime, `netstandard2.0;net8.0;net9.0;net10.0`)

The public surface every consumer and the generated code use:
- `IconPackAttribute` — the assembly-level declaration.
- `IconisticIcon` — immutable record struct (`Body`, `Width`, `Height`, transform), with `ToSvg`.
- `SvgComposer` — builds the `<svg>` string; mirrors Iconify's flip/rotate transform algorithm.
- `IconPack` / `IconPackLoader` — runtime parsing for `EmbeddedResource` / `DeployedFile` modes.
- `build/Iconistic.props` — exposes MSBuild settings to the generator (`CompilerVisibleProperty`).

This project also **packs the generator** into the `Iconistic` nupkg under `analyzers/dotnet/cs`
(see the `BuildGenerator` / `PackGenerator` targets in `Iconistic.csproj`). `BuildGenerator` runs on
the outer (TFM-less) build so the multitargeted `TargetFramework` is not propagated into the
netstandard2.0 generator.

### `src/Iconistic.Generator` (`netstandard2.0`, Roslyn)

An `IIncrementalGenerator` that is intentionally I/O-bound (`RS1035` is suppressed):
- `IconPackGenerator` — reads attributes (`CompilationProvider`), reads settings
  (`AnalyzerConfigOptionsProvider`), and emits via `RegisterSourceOutput`. Pack specs are wrapped in
  `EquatableArray` so Roslyn caches output and the network is only touched when packs change.
- `IconifyDownloader` — HTTP + on-disk cache keyed by a hash of the prefix and sorted icon names.
- `MiniJson` — a dependency-free JSON reader (the generator can't assume System.Text.Json is in the
  Roslyn host).
- `IconResolver` — follows alias chains, combining transforms.
- `SourceEmitter` — emits `BakedIn` literals, `EmbeddedResource` blobs, or writes `DeployedFile`
  JSON and emits an `InitializeAsync`.
- `NameMangler` — kebab/symbol names → unique PascalCase identifiers.

### `src/Iconistic.Api` (runtime, multi-target)

`IconifyClient` over `HttpClient` using System.Text.Json. `IconDataParser` resolves icon data into
`IconisticIcon` (the same alias/transform logic as the generator, but using System.Text.Json).

### `src/Iconistic.Blazor` (Razor class library)

Code-only `Icon : ComponentBase` rendering `Value.ToSvg(...)` via `AddMarkupContent`. References
`Microsoft.AspNetCore.Components.Web` with per-TFM `VersionOverride` so it works in Blazor WASM
across runtimes (a `FrameworkReference` to `Microsoft.AspNetCore.App` does **not** work for
`browser-wasm`).

### `src/Iconistic.Web` + `src/Iconistic.Web.Tests`

A Blazor WASM sample gallery and its tests. `Iconistic.Web.Tests` publishes the app during build
(`PublishBlazorForTests`) and serves it from a `WebApplication` for the Playwright tests.

## Key patterns / gotchas

- The generated `Icons` class lands in the consuming project's `RootNamespace`.
- A source generator can only add source, not MSBuild items — hence `EmbeddedResource` mode bakes a
  JSON string constant (rather than a real `.resources`), and `DeployedFile` writes to `wwwroot`.
- `TreatWarningsAsErrors` is on everywhere; `LangVersion` is `preview`.
- The shared `ProjectDefaults` package enables packing on Release for `IsPackageProject=true`
  projects and expects `src/icon.png` (the `PackageIcon`).
