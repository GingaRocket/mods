# GTA IV Collectibles Map

Shows the **Flying Rats** (200 pigeons) and **Unique Stunt Jumps** (50) you have not yet
collected directly on the in-game pause map and radar, live and without a save-file round trip.

It covers **GTA IV**, **The Lost and Damned**, and **The Ballad of Gay Tony**. The episodes'
seagulls replace pigeons automatically based on the running episode.

> **Runtime status:** validated in-game on Complete Edition 1.2.0.59. Both memory locators,
> pause-map markers, proximity-filtered minimap markers, category names, and faded `ShowAll`
> markers have been exercised successfully.

## How it works

The mod reads live game state rather than parsing saves:

- **Birds are pickups.** Collecting one deletes it from the pickup array, so every matching
  entry still present is uncollected. Pickup type 3 (`PICKUP_TYPE_PIGEON`) covers pigeons and
  both episodes' seagulls.
- **Stunt jumps** remain in a 50-entry game array with a `passed` flag.

ScriptHookDotNet does not expose either array. On Complete Edition the game adapter locates
them structurally at runtime:

- A pickup-array candidate must contain exactly the number of birds implied by the game's
  pigeon statistic, and every bird must match a unique bundled coordinate for the episode.
- A stunt-array candidate must begin with three canonical launch boxes at the expected
  `CStuntJump` stride. All flags must be boolean and the decoded passed count must agree with
  the game's completed-stunt statistic.

There are no fixed 1.2.0.59 addresses. A failed check disables only that tracker and reports
the reason in the Status panel rather than drawing potentially wrong markers.

Collected birds no longer have runtime positions, so `ShowAll` uses the bundled full coordinate
tables. Stunt jumps do not need a table because their entries remain in memory.

## Requirements

- **Grand Theft Auto IV: Complete Edition 1.2.0.59**
- **ScriptHookDotNet 1.7.1.8**
- A Complete Edition-compatible native hook. The current working installation uses Aru
  ScriptHook 0.5.1 plus `aCompleteEditionHook`.

## Installing

The deployable script statically includes the Core, Features, and Game layers, so only one DLL
is loaded at runtime:

```text
GTAIV/
`-- scripts/
    |-- GtaCollectiblesMap.net.dll
    `-- GtaCollectiblesMap.ini
```

Every build stages exactly this tree under `artifacts/<Configuration>/GTAIV`; copy the contents
of that `GTAIV` directory over the real game directory. If upgrading from an older split build,
remove `GtaCollectiblesMap.Core.dll`, `GtaCollectiblesMap.Features.dll`, and
`GtaCollectiblesMap.Game.dll` from the game root; the combined script does not use them.

Do not copy or rename `ScriptHookDotNet.asi`; use the copy installed at the game root. The
build references it for compilation but deliberately does not emit another copy.

## Using it

Press **F7** (configurable in the INI) for the settings panel. Changes are applied live and
written back to `GtaCollectiblesMap.ini` while preserving its comments.

The Status section reports the current episode, active marker count, remaining count per
category, and tracker failures. This makes a failed pool locator distinguishable from having
collected everything.

- **Pause map:** every uncollected item.
- **Radar:** nearby items within `RadarRadius`.
- **ShowAll:** already-collected items are faded and smaller, with an option to put them on the
  radar too.

GTA IV uses a fixed 20-colour blip palette rather than RGB. The panel excludes colours already
claimed by missions, waypoints, enemies and gangs. Blue is also excluded because the game
forces a blue blip's hover text to `Friend`. Defaults are **Teal** for birds and **DarkOrange**
for stunt jumps.

## Building

Requires the **.NET 10 SDK** and **.NET Framework 4.8 targeting pack**. Set `GTAIV_DIR` to the
Complete Edition directory containing `ScriptHookDotNet.asi`, or pass the equivalent MSBuild
property:

```powershell
dotnet build GtaCollectiblesMap.slnx -p:GtaIvDirectory="B:\Apps\Steam\steamapps\common\Grand Theft Auto IV\GTAIV"
dotnet test GtaCollectiblesMap.slnx
```

The known installation above is detected automatically on this machine. The runtime is .NET
Framework 4.8, with C# 13 enabled by `LangVersion` and
[PolySharp](https://github.com/Sergio0694/PolySharp). PolySharp is analyzer-only and adds no
runtime DLL. Projects touching ScriptHookDotNet target **x86** because GTA IV and the mixed-mode
ASI are 32-bit.

## Architecture

Dependencies point inward, and the game-independent boundary is enforced by architecture
tests.

| Project | Role | May reference |
|---|---|---|
| `GtaCollectiblesMap.Core` | Models, abstractions, settings | Framework only |
| `GtaCollectiblesMap.Game` | ScriptHookDotNet adapters and guarded memory readers | Core |
| `GtaCollectiblesMap.Features` | Tracker orchestration and coordinate tables | Core |
| `GtaCollectiblesMap` | Composition root, script entry, forms UI | All above |

`Core` and `Features` have no game-SDK references, so snapshot diffing, `ShowAll`
set-subtraction, position matching, proximity promotion, throttling, debounced persistence,
episode rebuilding and per-tracker fault isolation run as ordinary unit tests.

`CollectiblesScript` hand-wires the graph without a DI container. `ISettingsUi` keeps the
presentation behind a toolkit-free view model; its current implementation uses
ScriptHookDotNet's built-in forms controls and adds no runtime dependency.

## Attribution

Bird coordinate tables are derived from
[whampson/pigeon-locator](https://github.com/whampson/pigeon-locator), MIT licensed,
Copyright (c) 2018-2026 Wes Hampson. The full notice is retained in each generated data file
under `src/GtaCollectiblesMap.Features/Data/`.
