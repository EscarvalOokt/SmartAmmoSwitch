# SmartAmmoSwitch architecture

This document records the current production architecture and implementation boundaries of SmartAmmoSwitch 1.2.0.

## Sources of truth

1. The installed Terraria and TerrariaModder runtime define external runtime behavior and API contracts.
2. The production source defines behavior implemented in the mod.
3. This document records accepted SmartAmmoSwitch architecture decisions supported by that source.
4. [Testing policy](testing.md) defines the boundary between automated checks and in-game acceptance.

When documentation and production code disagree about current project behavior, production code is the source of truth until the discrepancy is resolved explicitly.

## Runtime baseline and dependencies

The current production version is **SmartAmmoSwitch 1.2.0**, targeting Terraria **1.4.5** and .NET Framework **4.8**. The manifest declares TerrariaModder Core **`>=0.4.1`**.

In-game validation was performed using Core **0.4.2**, including interaction with an independent TerrariaWindowKit consumer. Multiplayer behavior is unverified.

TerrariaWindowKit **1.1.0** is integrated as source-only code through `TerrariaWindowKit.Source.projitems` in a sibling checkout. No separate TerrariaWindowKit DLL is produced or required. The relative import selects files from that checkout but does not itself pin its Git revision.

The production build references Terraria, TerrariaModder Core, Harmony (`0Harmony.dll`), and the required XNA assemblies. SmartAmmoSwitch does not call Harmony APIs directly; the reference is required by the imported toolkit. Machine-specific paths belong in the ignored `src/SmartAmmoSwitch.Local.props`, with a portable example in `docs/SmartAmmoSwitch.Local.props.example`.

## Project boundaries

- Production remains one `SmartAmmoSwitch` project and assembly.
- TerrariaModder infrastructure owns configuration, keybind registration, logging, and mod lifecycle integration.
- TerrariaWindowKit owns root-window state, geometry, activation, input coordination, and Escape arbitration.
- TerrariaModder Core provides the Filter Editor's content widgets, rendering, tooltips, and UI input primitives.
- SmartAmmoSwitch owns ammo selection/filtering behavior and the Filter Editor content, layout, and consumer-side hit tests.

No parallel root-window, pin-state, or input-coordination abstraction is maintained by SmartAmmoSwitch.

## Entry point and lifecycle

`SmartAmmoSwitchMod : IMod, IModLifecycle` is the entry point. It resolves `SmartAmmoSwitchConfig` from the shared `ModContext`, initializes filtering and the editor, registers Next Ammo, Previous Ammo, and Open Filter UI keybinds, and processes config/world lifecycle notifications.

`FilterUiController.Initialize()` prepares configuration and the Search widget. The `CoreWindowBackend` and `WindowHost` are created lazily on the UI thread when the editor is first opened, after the ammo catalog can be built. The host keeps the window registration while it is closed and reuses it for later opens.

On mod unload, `SmartAmmoSwitchMod.Unload()` delegates to `FilterUiController.Unload()`. The controller attempts `WindowHost.Dispose()` before disposing `CoreWindowBackend`, then releases its references and cached UI lists. The mod clears its catalog/filter state and context. WindowKit's Core integration coordinates window closure on menu/world transitions.

Individual mod unloading via `IMod.Unload()` has not been verified in game.

## Configuration

`SmartAmmoSwitchConfig : ModConfig` is the typed configuration model. Its schema `Version` remains **1**, independently of mod version 1.2.0.

The configuration surface contains:

- `ShowMessages`;
- `SearchMode`;
- `FilterMode`;
- `FilteredAmmoItemIds`.

Legacy keys are mapped with `FormerlySerializedAs`. `SmartAmmoSwitchMod` loads the typed configuration and `FilterUiController` persists filter mode and item IDs through the same configuration object. `AmmoFilterSettings` is the shared source of runtime filter state.

## Ammo switching flow

The main interaction path is:

```text
keybind
  -> SmartAmmoSwitchMod.Switch
  -> discover and filter candidates (AmmoScanner, AmmoFilterPipeline)
  -> build AmmoSwitchContext with candidates and actual active ammo
  -> AmmoCycleService.Synchronize
  -> select next/previous ammo type
  -> swap selected stack with the actual active slot
  -> verify selected type is active; roll back the swap on failure
  -> remember resulting candidate layout on success
```

`AmmoScanner` owns candidate discovery within the configured search area. `AmmoFilterPipeline` restricts the candidates available for cycling. `SmartAmmoSwitchMod.GetPreferredAmmoSlot` identifies the physically active compatible ammo independently of that filter, so a blocked stack can still be the slot that must receive the selected allowed stack. `AmmoSwitchContext` carries the candidates, actual active type, and snapshots prepared for one switch operation.

`AmmoSlotPriority` provides the shared slot ranking for the initial type order, target-stack selection, and actual active-slot lookup. Regular ammunition prioritizes ammo slots **54-57**, followed by main inventory slots **0-49**. Coin ammunition prioritizes coin slots **50-53**, followed by main inventory slots **0-49**. Slots outside the corresponding ranges are excluded.

`AmmoCycleService.Synchronize` preserves the established type order as stacks appear or disappear, removes unavailable types, and appends newly available types. Manual movement of a remaining type rebuilds the order from the current candidate layout; a changed weapon or ammo category, or an active type absent from the allowed cycle, also resets the state. When the actual active type remains in the cycle, synchronization anchors the current index to it, including when consuming a stack exposes another active type without changing the set of available types.

After swapping, `SmartAmmoSwitchMod` verifies that the selected type occupies the actual active slot. A failed verification reverses the swap. The resulting candidate layout is remembered after a successful switch.

## Filtering and catalog

Filtering uses `IAmmoFilter` and `AmmoFilterPipeline`. The registered `ItemTypeAmmoFilter` delegates item-level policy to `AmmoFilterSettings`, supporting Disabled, Blacklist, and Whitelist. The editor and switching pipeline use the same selected item IDs; the editor persists changes through `SmartAmmoSwitchConfig`.

`SupportedAmmoCatalog` lazily builds runtime metadata for valid ammo item IDs; `SupportedAmmoClassifier` assigns supported categories. The Filter Editor filters catalog and selected-item views by search text and category, without maintaining a second independent filter store. Bulk actions operate on the corresponding filtered result collections, not just visible scroll rows.

## Filter Editor window and input

`FilterUiController` creates one `WindowHost` with `CoreWindowBackend`. The window is draggable and pinnable, but not resizable. Its base size is **520 × 420 UI units**; width increases when needed to keep the complete mode labels readable. The controller supplies that calculated width as `MinimumWidth` and **420 UI units** as `MinimumHeight`. The host fits the bounds to the viewport; a viewport smaller than those minimums takes precedence. When the viewport grows again, the configured minimums restore the required content size.

`WindowHost` owns root open/close, Pin state, bounds, drag operation, activation, and Escape arbitration. The controller draws the header and close button, tests header/button pointer hits, starts drag with `BeginDrag`, and uses `CorePinButtonRenderer` for Pin/Unpin presentation. The renderer does not own pin state. Pin prevents root closing by Escape while active; closing and reopening the window resets pin.

The host's consumer callbacks include:

- `Draw` — draw the Core widgets and advance the pointer operation;
- `HasTextFocus` and `ReleaseTextFocus` — expose/release Search `TextInput` focus for Escape handling;
- `WheelAt` — deliver wheel input to the list viewport and pass through outside it.

Search click-away is handled after `TextInput.Draw()`. `FilterEditorClickAwayPolicy.IsEligiblePress()` accepts a nonblocked click signal or the physical rising edge of the left mouse button while Search has focus, allowing focus release even if another widget consumed the click. `FilterUiController` tracks the preceding mouse state and checks whether the pointer is outside Search before unfocusing it. This deterministic decision is covered by project NUnit tests; actual input delivery remains a runtime acceptance concern.