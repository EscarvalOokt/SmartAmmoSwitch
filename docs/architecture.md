# SmartAmmoSwitch architecture

This document records the current production architecture and implementation boundaries of SmartAmmoSwitch.

It is an architecture specification, not a RoadMap or implementation guide.

## Sources of truth

1. The installed Terraria and TerrariaModder runtime define external runtime behavior and API contracts.
2. The current production implementation defines behavior actually available in the built mod.
3. This document records accepted SmartAmmoSwitch architecture decisions confirmed by the current source.
4. `docs/testing.md` defines automated testing and runtime acceptance boundaries.

When documentation and production code disagree about current project behavior, production code is the source of truth until the discrepancy is resolved explicitly.

## Runtime baseline

The current release baseline is SmartAmmoSwitch 1.1.0.

The manifest declares:

- Terraria 1.4.5.
- TerrariaModder framework version `>=0.4.0`.

The production project targets .NET Framework 4.8. The current 1.1.0 project builds successfully, its automated smoke test passes, and runtime validation completed without identified issues.

## Project boundaries

- Production code remains a single `SmartAmmoSwitch` project.
- Existing conceptual subsystems remain separate classes but are not turned into additional projects or directory layers without a concrete need.
- Existing TerrariaModder infrastructure is reused for lifecycle, configuration, keybind registration, logging, and UI widgets.
- Machine-specific Terraria/framework paths belong only in `src/SmartAmmoSwitch.Local.props` and must not be committed.
- Harmony is not referenced because the current production source does not use Harmony APIs.

## Entry point and lifecycle

`SmartAmmoSwitchMod : IMod, IModLifecycle` is the production entry point.

It owns the shared `ModContext`, resolves `SmartAmmoSwitchConfig`, initializes the filter UI, registers keybind callbacks, coordinates ammo switching, and handles the supported TerrariaModder lifecycle callbacks used by the current implementation.

## Configuration

`SmartAmmoSwitchConfig : ModConfig` is the typed configuration model.

The current configuration surface contains:

- `ShowMessages`;
- `SearchMode`;
- `FilterMode`;
- `FilteredAmmoItemIds`.

Legacy configuration keys are mapped with `FormerlySerializedAs` so existing settings can be migrated to the typed configuration model.

`SmartAmmoSwitchMod` resolves configuration through `ModContext.GetConfig<SmartAmmoSwitchConfig>()`. `FilterUiController` uses the same typed configuration object when persisting filter mode and selected item IDs.

Runtime filtering state remains centralized in `AmmoFilterSettings`.

## Ammo switching flow

The main interaction path is:

```text
keybind
  -> SmartAmmoSwitchMod.Switch
  -> build AmmoSwitchContext
  -> AmmoScanner
  -> AmmoFilterPipeline
  -> AmmoCycleService state synchronization
  -> select next/previous ammo type
  -> swap inventory slots
  -> remember resulting candidate layout
```

`AmmoScanner` owns candidate discovery. `AmmoCycleService` owns cycle ordering state. `AmmoSwitchContext` carries the prepared values needed for one switch operation.

## Filtering

Filtering is separated behind `IAmmoFilter` and `AmmoFilterPipeline`.

The current pipeline registers one implementation, `ItemTypeAmmoFilter`, which delegates item-level policy to the shared static `AmmoFilterSettings` state. Supported modes are Disabled, Blacklist, and Whitelist.

The same `AmmoFilterSettings` state is used by switching logic and the in-game filter editor, so configuration and runtime filtering share one source of runtime state.

## Supported ammo catalog and UI

`SupportedAmmoCatalog` lazily builds runtime metadata for valid ammo item IDs and uses `SupportedAmmoClassifier` to group entries into supported categories.

`FilterUiController` uses TerrariaModder.Core UI infrastructure, including shared panels, layouts, widgets, renderer/input helpers, and colors. Project-specific replacement UI abstractions should not be introduced unless future requirements justify them.

## Shared project mechanisms

The current reusable project-owned mechanisms are:

- `AmmoUtils` for ammo validity, inventory signatures, and candidate-layout snapshots.
- `IAmmoFilter` / `AmmoFilterPipeline` for composable filtering.
- `AmmoFilterSettings` for runtime filter mode and selected item IDs.
- `SupportedAmmoCatalog` / `SupportedAmmoClassifier` for catalog metadata.
- Small data carriers such as `AmmoCandidate`, `AmmoLayoutEntry`, `AmmoSwitchContext`, and `SupportedAmmoInfo`.

These mechanisms should be reused rather than duplicated when future changes operate in the same domains.