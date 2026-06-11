# ScanValue

Client-side Lethal Company mod for scan-based scrap value labels, item names, and value-based highlights.

ScanValue mirrors the vanilla right-click scan result flow. It keeps vanilla total value behavior, suppresses only the original scrap item scan boxes, and renders its own pooled world-space labels above scanned scrap.

## Architecture

- `ScanValue.Core`: pure, testable visibility rules and price formatting.
- `ScanValue.Configuration`: BepInEx config binding and clamped runtime settings.
- `ScanValue.Game`: client-only registration of `GrabbableObject` scrap and explicit Harmony patches for vanilla scan nodes.
- `ScanValue.Runtime`: throttled update loop that mirrors the current vanilla scan UI nodes, with an optional legacy always-on radius mode.
- `ScanValue.Presentation`: pooled TextMeshPro world-space labels.

## Performance rules

- No per-frame scene-wide searches.
- No runtime `.GetType()` discovery.
- Default `VanillaScan` mode reuses HUDManager's existing scan results instead of running extra physics queries.
- No Harmony `PatchAll`; patches are registered by explicit method name at startup.
- Legacy `Always` mode uses squared distance and runs on a configurable interval.
- Labels are pooled and capped by `MaxVisibleLabels`.

## Build

ScanValue references Lethal Company and BepInEx assemblies from a local game/mod profile install. Provide those paths through MSBuild properties or environment variables.

```powershell
dotnet build src\ScanValue\ScanValue.csproj -c Release `
  -p:GameRoot="D:\Steam\steamapps\common\Lethal Company" `
  -p:BepInExCoreDir="D:\path\to\BepInEx\core"
```

Alternatively set `LETHAL_COMPANY_GAME_ROOT` and `BEPINEX_CORE_DIR` before building.

## Config Language

The plugin detects `Auuueser/LC-Chinese-Project` by loaded plugin metadata, existing Chinese config sections, or package manifest identity. It does not depend on a fixed LC Chinese Project version.

When LC Chinese Project is detected, generated config sections and descriptions are Chinese. Setting keys remain stable English names so existing values survive language or localization package changes. All settings reload while the game is running after the config file is saved.

## Value Colors

`EnableValueBasedColors` defaults to `true`. When enabled, the scanned scrap value drives both label text color and scan outline color using configurable value bands: `0-39`, `40-79`, `80-119`, `120-169`, and `170+`. Unknown scan values such as docked Apparatus `???` use `UnknownValueColor`.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

ScanValue is licensed under the GPL-3.0 license.
