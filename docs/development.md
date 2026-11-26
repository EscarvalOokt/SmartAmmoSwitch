# Smart Ammo Switch development guide

## Prerequisites

- A .NET SDK with a C# 12 or later compiler, such as .NET SDK 8, and the .NET Framework 4.8 development tools for building the `net48` solution on Windows.
- A local installation of [Terraria and TerrariaModder Core](../README.md#requirements). The project references `Terraria.exe`, `TerrariaModder.Core.dll`, and the `0Harmony.dll` distributed with Core.
- The XNA Framework assemblies `Microsoft.Xna.Framework.dll` and `Microsoft.Xna.Framework.Game.dll`.
- A [TerrariaWindowKit v1.1.0](https://github.com/EscarvalOokt/TerrariaWindowKit/releases/tag/v1.1.0) checkout in a folder named `TerrariaWindowKit` alongside `SmartAmmoSwitch`.

The project imports `../TerrariaWindowKit/src/TerrariaWindowKit.Source.projitems` relative to the SmartAmmoSwitch repository root. It compiles the toolkit directly into `SmartAmmoSwitch.dll`.

## Local configuration

Copy [SmartAmmoSwitch.Local.props.example](SmartAmmoSwitch.Local.props.example) to `src/SmartAmmoSwitch.Local.props` and set the paths for your installation. Git ignores the local file.

| Property | Purpose |
| --- | --- |
| `TerrariaDirectory` | Path to the folder containing `Terraria.exe` and `TerrariaModder`. |
| `XnaFrameworkDll` | Path to `Microsoft.Xna.Framework.dll` for the WindowKit Core integration. |
| `XnaFrameworkGameDll` | Path to `Microsoft.Xna.Framework.Game.dll` for compiling against Terraria. |
| `HarmonyDll` | Optional override for `0Harmony.dll`. Defaults to `TerrariaModder/core/deps/0Harmony.dll` under `TerrariaDirectory`. |
| `DeployToGame` | Set to `true` to copy the mod into the game after a successful build. Defaults to `false`. |
| `TerrariaModDirectory` | Deployment destination. Defaults to `TerrariaModder/mods/smart-ammo-switch` under `TerrariaDirectory`. |

## Build

From the repository root, build the solution in Release configuration with deployment disabled.

```powershell
dotnet build src/SmartAmmoSwitch.sln -c Release -p:DeployToGame=false
```

To deploy to your local game, close Terraria and run the same command with `-p:DeployToGame=true`. After a successful build, the project copies `SmartAmmoSwitch.dll` and `mod/manifest.json` to `TerrariaModDirectory`.

## Tests

From the repository root, run the NUnit test project. This command also builds the mod with deployment disabled, even if deployment is enabled in your local configuration.

```powershell
dotnet test tests/SmartAmmoSwitch.Tests/SmartAmmoSwitch.Tests.csproj -c Release -p:DeployToGame=false
```

The [testing policy](testing.md) describes what the NUnit suite covers and what to check in game.