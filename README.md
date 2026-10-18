# Smart Ammo Switch

![Smart Ammo Switch preview](assets/SmartAmmoSwitch_Preview.png)

**Cycle through compatible ammo types without manually rearranging your inventory.**

[Download on Nexus Mods](https://www.nexusmods.com/terraria/mods/170)

Smart Ammo Switch is a quality-of-life mod for Terraria 1.4.5. It lets you cycle forward and backward through ammunition compatible with your currently held weapon and manage which ammo types are allowed with an in-game Filter Editor.

## Features

- Cycle between compatible ammunition types with dedicated hotkeys.
- Search the four dedicated ammo slots or the whole inventory.
- Keep a stable cycling order while responding to inventory and manual layout changes.
- Use Disabled, Blacklist, or Whitelist filtering.
- Manage ammunition in a searchable catalog with category icons, item previews, and bulk actions.
- Drag or pin the Filter Editor window.
- Optionally show the selected ammo type in chat.

## How it works

When an ammo-switch keybind is pressed, Smart Ammo Switch:

1. Finds ammunition compatible with the currently held weapon in the configured search area.
2. Excludes ammunition blocked by the active filter.
3. Chooses the next or previous available ammo type.
4. Swaps the chosen stack with the ammunition stack Terraria currently selects for the held weapon, when needed.

For whole-inventory search, the initial cycling order prioritizes the dedicated ammo slots for regular ammunition and the coin slots for the Coin Gun. Coin Gun cycling requires whole-inventory search. Cycling can rearrange ammo stacks to make the selected type active.

The mod respects Terraria's ammo cycling accessories. Further shots or pauses can change which ammo Terraria selects according to the game's normal rules.

No switch occurs when the held weapon does not use ammunition, no compatible ammo is available, the filter blocks all compatible types, or only one usable type remains.

## Ammo filtering

Open the **Filter Editor** with **Alt + C** by default. The editor offers three modes:

- **Disabled** — all compatible ammunition is allowed; saved selections are retained.
- **Blacklist** — selected ammunition is excluded from cycling.
- **Whitelist** — only selected ammunition is allowed. An empty whitelist blocks ammo switching by the mod.

The active mode button displays the selected-item count. In Disabled mode, the count is shown as saved selections.

The editor provides two views: **Catalog** for supported ammunition and **Selected** for items already in the filter. Search by name, item ID, or category, then use the category tabs (arrows, bullets, rockets, darts, coins, flares, sand, solution, or other) to narrow the results. Categories use item icons where available. Rows show an item icon and selection checkbox, with item details in tooltips.

Bulk actions apply to search and category results, not just rows currently visible in the scroll area:

- **Select results** (Catalog) adds every matching item to the selection.
- **Clear results** (Selected) removes the matching selected items.
- **Clear all** removes all selections, regardless of the current search or category.

You can drag the Filter Editor by its header or use **Pin** to prevent Escape from closing the window. When Search has keyboard focus, Escape releases that focus first. You can still close a pinned window with its close button or the Filter Editor keybind. Pin resets when the window is closed and reopened.

Filter mode and selected ammunition IDs are saved through TerrariaModder configuration.

## Controls

Default keybinds:

- **X** — Next Ammo.
- **Z** — Previous Ammo.
- **Alt + C** — Open Filter UI.

Keybinds can be configured through TerrariaModder.

## Requirements

- Terraria 1.4.5.
- TerrariaModder Core 0.4.1 or newer.

## Compatibility

The mod does not add persistent items, NPCs, tiles, or world objects, so it does not require a new character or world. Multiplayer behavior has not been tested.

## Installation

1. Install and configure TerrariaModder.
2. Launch Terraria through `TerrariaInjector.exe` at least once to initialize TerrariaModder and its `mods` directory.
3. Obtain a Smart Ammo Switch distribution containing `SmartAmmoSwitch.dll` and `manifest.json`.
4. Place the `smart-ammo-switch` folder in `Terraria/TerrariaModder/mods/`, with both files directly inside that folder.
5. Launch Terraria through `TerrariaInjector.exe`.

The installed directory should look like this:

```text
Terraria/
└── TerrariaModder/
    └── mods/
        └── smart-ammo-switch/
            ├── SmartAmmoSwitch.dll
            └── manifest.json
```

## Development

### Build prerequisites and local configuration

The production project targets .NET Framework 4.8. Building from source requires the appropriate .NET development tools, Terraria, TerrariaModder Core, Harmony, and XNA references.

TerrariaWindowKit [1.1.0](https://github.com/EscarvalOokt/TerrariaWindowKit/releases/tag/v1.1.0) is imported as **source-only**, not as a separate DLL. From the directory containing the `SmartAmmoSwitch` repository, clone the `v1.1.0` tag into a sibling `TerrariaWindowKit` directory:

```bash
git clone --branch v1.1.0 --depth 1 https://github.com/EscarvalOokt/TerrariaWindowKit.git TerrariaWindowKit
```

The project expects this path relative to the SmartAmmoSwitch root:

```text
../TerrariaWindowKit/src/TerrariaWindowKit.Source.projitems
```

Copy `docs/SmartAmmoSwitch.Local.props.example` to `src/SmartAmmoSwitch.Local.props` and configure:

- `TerrariaDirectory` — root of the local Terraria installation; the project resolves Terraria, Core, and the default `0Harmony.dll` path from it.
- `XnaFrameworkDll` — local `Microsoft.Xna.Framework.dll` reference.
- `XnaFrameworkGameDll` — local `Microsoft.Xna.Framework.Game.dll` reference.
- `DeployToGame` — set to `true` to copy build output to the game after a successful build.
- `TerrariaModDirectory` — destination `TerrariaModder/mods/smart-ammo-switch` directory when deployment is enabled.

The local props file contains machine-specific paths and is intentionally excluded from Git. `HarmonyDll` can be supplied explicitly if it differs from the default derived from `TerrariaDirectory`.

### Build

Build the production solution in Release configuration:

```bash
dotnet build src/SmartAmmoSwitch.sln -c Release
```

When `DeployToGame` is enabled, the project copies `SmartAmmoSwitch.dll` and `mod/manifest.json` to the configured mod directory.

### Tests

Run the NUnit test project:

```bash
dotnet test tests/SmartAmmoSwitch.Tests/SmartAmmoSwitch.Tests.csproj -c Release
```

The automated suite covers ammo slot priorities, cycle state synchronization, and Filter Editor Search click-away. Behavior that depends on the complete Terraria, TerrariaModder, and XNA runtime is validated separately in game; see the [testing policy](docs/testing.md).

## Repository structure

```text
docs/    Project documentation and development configuration examples
mod/     TerrariaModder package metadata
src/     Production source and solution
tests/   Automated tests
```

## Technical documentation

- [Architecture](docs/architecture.md)
- [Testing policy](docs/testing.md)

## Links

- [Nexus Mods](https://www.nexusmods.com/terraria/mods/170)
- [GitHub Releases](https://github.com/EscarvalOokt/SmartAmmoSwitch/releases)