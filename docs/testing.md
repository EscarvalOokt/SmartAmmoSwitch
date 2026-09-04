# SmartAmmoSwitch testing policy

This document defines the automated testing and runtime acceptance boundaries for SmartAmmoSwitch.

## Testing principle

Automated tests should protect meaningful project-owned contracts and realistic regressions. Test count and broad method-level coverage are not goals by themselves.

## Automated test boundary

The NUnit-based `SmartAmmoSwitch.Tests` project contains deterministic tests for behavior owned by SmartAmmoSwitch.

The initial smoke test validates only that the test project is discoverable and executable. It is infrastructure validation, not product behavior coverage.

Good candidates for deterministic tests include project-owned parsing, serialization, filtering, ordering, and cycle-state rules when they can be exercised without reproducing Terraria runtime semantics.

## Runtime acceptance boundary

Behavior that depends on the complete Terraria, TerrariaModder, or XNA runtime should be validated separately in game rather than reproduced through fragile simulations.

Examples include:

- whether the mod is discovered and loaded by the target TerrariaModder version;
- lifecycle and configuration integration;
- keybind registration and input behavior;
- inventory mutation in the live game;
- TerrariaModder.Core in-game UI rendering and input handling;
- gameplay compatibility with a particular Terraria release.

## Adding tests

Add tests together with project-owned behavior when they protect a meaningful contract. Keep runtime acceptance scenarios separate when the relevant semantics are owned by Terraria or TerrariaModder rather than this project.