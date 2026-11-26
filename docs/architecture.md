# Smart Ammo Switch architecture

This document describes the runtime behavior of Smart Ammo Switch and the responsibilities of its components.

## Sources of truth

- The [manifest](../mod/manifest.json) declares runtime requirements. The [production project](../src/SmartAmmoSwitch/SmartAmmoSwitch.csproj) defines the build target and references.
- Terraria and TerrariaModder define the external APIs. The production source defines how the mod uses them, and the [configuration model](../src/SmartAmmoSwitch/SmartAmmoSwitchConfig.cs) defines its settings.
- The [development guide](development.md) covers local setup, build, and test commands. The [testing policy](testing.md) describes automated coverage and in-game checks.

## Runtime baseline

- The manifest for version `1.3.0` specifies Terraria `1.4.6` and TerrariaModder Core `>=0.4.3`.
- The production project targets .NET Framework 4.8.
- TerrariaModder Core provides lifecycle callbacks, configuration, keybind registration, logging, and UI services.
- The production project imports TerrariaWindowKit sources with Core integration enabled. The build uses the revision present in the sibling checkout and compiles it into `SmartAmmoSwitch.dll`. The imported toolkit uses Harmony.

## Runtime flow

1. A Next Ammo or Prev Ammo keybind calls `SmartAmmoSwitchMod.Switch`. `AmmoScanner` scans the configured slots. The mod keeps stacks compatible with the held weapon and allowed by `AmmoFilterPipeline`. Switching requires at least two distinct allowed item types.
2. The mod asks Terraria which stack is active, independently of the filter. In `AmmoSlotsOnly` mode, it stops before advancing the cycle or moving a stack if the active slot falls outside slots 54–57. Otherwise, `AmmoSwitchContext` records the active slot and type, filtered candidates, and current layout.
3. `AmmoCycleService.Synchronize` updates the type order and selects the starting index. The mod advances in the requested direction and picks a stack of the selected type.
4. The mod swaps that stack with the one in the recorded active slot and asks Terraria for the active ammo again. If the selector fails or returns another type, the mod reverses the exchange. A successful switch records the resulting candidate layout for the next keypress.

## Ammo selection and cycle order

`AmmoSlotPriority` ranks both the initial type order and the choice among stacks of the same type. Regular ammunition prioritizes ammo slots 54–57, followed by main inventory slots 0–49. Coins prioritize coin slots 50–53, then slots 0–49. The search mode restricts which of these slots are scanned.

`GetPreferredAmmoSlot` calls Terraria's private `Player.PickAmmo_PickAmmoItem(Item)` selector through a `MethodInfo` cached at initialization. It locates the returned item in the inventory by reference identity. The lookup reads the weapon's current ammo choice in Terraria's None, AmmoSlots, FullInventory, and Random modes and leaves the cycling offset, timer, and ammunition intact. If the first lookup fails, the mod leaves the stacks in place. A stack blocked by the filter can still occupy the active slot and receive an allowed stack.

When types appear or disappear, `AmmoCycleService` preserves the order of remaining types and appends new ones. It compares the actual type sets when inventory signatures match, so hash collisions cannot hide a change. Moving a remaining type to a new slot, changing weapon type or ammo category, or finding the active type outside the allowed cycle rebuilds the order from the current layout. If the active type is outside the cycle, Next selects the first allowed type and Prev the last. Otherwise the cycle anchors to the active type, including when consuming one stack exposes another.

## Filtering and catalog

`ItemTypeAmmoFilter` applies `AmmoFilterSettings` in the switching pipeline. The Filter Editor uses the same filter mode and selected item IDs. `SupportedAmmoCatalog` builds and caches the ammo metadata and `SupportedAmmoClassifier` assigns categories.

Search and the active category narrow the Catalog and Selected views. The editor keeps the active category when a view becomes empty and disables its scoped bulk action until there are matches. **Clear all** applies to the entire selection.

## Filter Editor window and input

`FilterUiController` creates the `CoreWindowBackend` and `WindowHost` on the UI thread the first time the editor opens. Later opens reuse the host. Users can drag and pin the window. Its size is set by the layout, starting at 520 × 420 UI units. The controller increases the minimum width for longer mode labels, and the host fits the window into smaller viewports and restores its minimum size when space permits.

The controller draws the header and Pin control and handles hit testing. The host manages window activation, dragging, pin state, and Escape. A pin keeps Escape from closing the window and resets on reopening. The controller exposes Search focus to the host and handles the mouse wheel over the list viewport.

When Search has focus, a new physical left-button press over a higher-priority Core panel releases focus before Search updates or draws. The receiving panel can handle the click and acquire text input. An outside click can also release Search focus when another editor widget has already consumed the click signal.

## Configuration

`SmartAmmoSwitchConfig` uses schema version 1 and maps legacy setting names with `FormerlySerializedAs`. `SmartAmmoSwitchMod` reads message and search preferences and loads the filter mode and selected item IDs into `AmmoFilterSettings`. Unrecognized filter modes, including undefined numeric values, resolve to Disabled. The editor saves its changes through the same configuration object. The [settings reference](../README.md#settings) describes the player-facing options.

## Lifecycle

During initialization, `SmartAmmoSwitchMod` resolves the native ammo selector, loads the configuration, prepares the filter and editor, and registers the keybinds. On each world load it clears the previous cycle's order, index, and inventory snapshot, invalidates the ammo catalog, and refreshes the editor. Configuration changes reload the settings and refresh the editor.

On mod unload the controller disposes the window host before its backend and clears its cached lists. The mod invalidates the catalog, resets the filter, and releases the configuration, selector, and context references. WindowKit coordinates window closure on menu and world transitions.

## Project boundaries

- Terraria determines which stack the held weapon would use. Smart Ammo Switch owns the allowed cycle, the exchange of inventory stacks, and verification of the resulting selection.
- TerrariaModder Core supplies the widgets and rendering, and TerrariaWindowKit coordinates windows and input between mods. Smart Ammo Switch owns the editor's content, layout, search and selection behavior.