# Smart Ammo Switch

*Cycle through compatible ammo types without manually rearranging your inventory.*

Smart Ammo Switch is a quality-of-life mod for Terraria that lets you cycle forward and backward through ammunition compatible with your currently held weapon.

The mod can search either only the dedicated ammo slots or your whole inventory, with ammo slots taking priority when whole-inventory search is enabled.

A built-in Filter Editor lets you exclude unwanted ammunition with a blacklist or restrict switching to a selected whitelist.

## Features

- Cycle forward and backward through compatible ammunition types.
- Search only the four dedicated ammo slots or the entire inventory.
- Prioritize dedicated ammo slots when whole-inventory search is enabled.
- Preserve a stable cycling order while reacting to inventory and layout changes.
- Choose between Disabled, Blacklist, and Whitelist filtering modes.
- Manage filtered ammunition through an in-game Filter Editor.
- Search supported ammunition by name, item ID, or category.
- Browse currently selected ammunition separately from the full catalog.
- Select or clear multiple visible items at once.
- Optionally show the currently selected ammo type in chat.

## How it works

When an ammo-switch keybind is pressed, Smart Ammo Switch checks the ammunition compatible with the currently held weapon.

1. Compatible ammunition is collected from the configured search area.
2. The current item filter is applied.
3. The next or previous available ammo type is selected.
4. The selected ammo stack is moved into the preferred active position when necessary.

When whole-inventory search is enabled, dedicated ammo slots are checked before normal inventory slots.

No switch is performed when:

- No compatible ammunition is available.
- The active filter blocks every compatible ammo type.
- Only one usable ammo type remains.
- The held item does not use ammunition.

Cycling may rearrange ammo stacks in the inventory so the selected ammunition becomes the active type.

## Ammo filtering

The built-in Filter Editor controls which supported ammunition can participate in cycling.

Open it with the **Open Filter UI** keybind, which defaults to **Alt + C**.

### Disabled

Filtering is ignored and all compatible ammunition may be used.

### Blacklist

Selected ammunition is excluded from cycling.

### Whitelist

Only selected ammunition may be used.

The Filter Editor includes:

- A searchable supported-ammo catalog.
- Category tabs for arrows, bullets, rockets, darts, coins, flares, sand, solution, and other supported ammo.
- A separate list of currently selected items.
- Bulk selection and clearing actions.
- In-game item previews.

Filter settings are saved through the TerrariaModder configuration system.

## Controls

Default keybinds:

- **X** — Next Ammo.
- **Z** — Previous Ammo.
- **Alt + C** — Open Filter UI.

Keybinds can be configured through TerrariaModder.

## Requirements

- Terraria 1.4.5
- TerrariaModder 0.4.0 or newer

A .NET Framework 4.8 development environment is additionally required when building the project from source.

## Compatibility

Smart Ammo Switch is designed for Terraria 1.4.5 and the standard Terraria ammunition system used by the current project baseline.

The mod can be safely added to or removed from existing characters and worlds. No preparation or new character/world is required.

It does not add persistent items, NPCs, tiles, world objects, or other save-dependent content.

Multiplayer behavior has not been verified and should currently be considered unconfirmed.

## Installation

1. Install and configure TerrariaModder.
2. Launch Terraria through `TerrariaInjector.exe` at least once so TerrariaModder can initialize its files and create the `mods` directory if necessary.
3. Copy the `smart-ammo-switch` folder into:

```text
Terraria/TerrariaModder/mods/
````

The installed mod directory should contain:

```text
smart-ammo-switch/
├── SmartAmmoSwitch.dll
└── manifest.json
```

Launch the game through `TerrariaInjector.exe`.

## Development

### Local configuration

Copy:

```text
docs/SmartAmmoSwitch.Local.props.example
```

to:

```text
src/SmartAmmoSwitch.Local.props
```

Then configure:

* `TerrariaDirectory` — root directory of the local Terraria installation.
* `XnaFrameworkGameDll` — local `Microsoft.Xna.Framework.Game.dll` reference used to compile against Terraria.
* `DeployToGame` — set to `true` to enable post-build deployment.
* `TerrariaModDirectory` — target `TerrariaModder/mods/smart-ammo-switch` directory.

The local props file contains machine-specific paths and is intentionally excluded from Git.

### Build

Build the production solution in Release configuration:

```bash
dotnet build src/SmartAmmoSwitch.sln -c Release
```

When `DeployToGame` is enabled, the production project copies `SmartAmmoSwitch.dll` and `mod/manifest.json` to the configured TerrariaModder mod directory after a successful build.

### Tests

Run the NUnit test project with:

```bash
dotnet test tests/SmartAmmoSwitch.Tests/SmartAmmoSwitch.Tests.csproj -c Release
```

The current automated suite contains infrastructure smoke coverage for the test pipeline.

Deterministic project-owned behavior can be covered with automated tests, while behavior that depends on the complete Terraria, TerrariaModder, or XNA runtime is validated separately in game.

## Repository structure

```text
docs/    Project documentation and development configuration examples
mod/     TerrariaModder package metadata
src/     Production source and solution
tests/   Automated tests
```

## Technical documentation

* [Architecture](docs/architecture.md)
* [Testing policy](docs/testing.md)

## Links

* [Nexus Mods](https://www.nexusmods.com/terraria/mods/170)
* [GitHub Releases](https://github.com/EscarvalOokt/SmartAmmoSwitch/releases)