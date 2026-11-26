# Smart Ammo Switch testing policy

This document defines the automated testing and runtime acceptance boundaries for Smart Ammo Switch.

## Testing principle

Check mod behavior with explicit inputs in automated tests. Verify interactions with Terraria and TerrariaModder in the installed game.

## Automated test boundary

The `SmartAmmoSwitch.Tests` NUnit project covers the following behavior.

- **Slot priority.** Ordinary ammo uses dedicated ammo slots before the main inventory. Coin Gun ammo uses coin slots first. Tests check which slots qualify for each order.
- **Cycle order.** Forward and backward switches enter the allowed cycle from opposite ends when the active type is blocked. Adding or losing a duplicate stack preserves the order. New types are appended, depleted types drop out, and manually moving stacks resets the cycle to the current layout. Clearing the cycle lets a new character start from their own slots.
- **Filter mode parsing.** Defined names and numeric values select the corresponding mode. Missing, unknown or out-of-range values fall back to `Disabled`.
- **Filter Editor categories.** Clearing the selected arrows preserves selected bullets. A search that matches only bullets leaves the Arrows view empty until the category changes.
- **Search focus.** The focus rule recognizes a new click even if another widget consumed the click signal. Hovering over a higher-priority panel or holding the mouse button there does not trigger another focus change.

A separate smoke test checks test discovery and execution. See the [development guide](development.md#tests) for the command to run the suite.

## Runtime acceptance boundary

Use a bow that fires the equipped arrow type. Start with filtering disabled and no ammo-cycling accessory. **Next Ammo** and **Prev Ammo** default to **X** and **Z**. Use the assigned keys if they have been rebound.

1. **Switch in both directions.** Place a stack of at least two arrows of a different type in each of the first three ammo slots. Start with the first type active. Press **Next Ammo** and fire once, then press **Prev Ammo** and fire again. The first shot should use the second type and the next shot should use the original type.
2. **Filtered active type.** Restore the original stack order and blacklist the active arrow type. With two types still allowed, **Next Ammo** should select the first allowed type in slot order. Repeat from the same starting layout with **Prev Ammo**. It should select the last allowed type. With an empty whitelist, either key should leave all stacks in place.
3. **Coin Gun.** Put two coin denominations in the coin slots and hold the Coin Gun with filtering disabled. In ammo-slots-only mode, the switch keys should leave the coins in place. Change to whole-inventory search and switch. The other denomination should become active.
4. **Ammo-cycling accessory.** Equip the accessory with at least two compatible types in the dedicated ammo slots and another stack in the main inventory. Let Terraria select the main-inventory stack. In ammo-slots-only mode, press each switch key. The stacks should stay in place. Switch to whole-inventory search. The keys should now cycle between compatible types.
5. **Clear one category.** Open the Filter Editor and select at least one arrow and one bullet. Choose **Selected → Arrows** and click **Clear results**. Arrows should remain the active category, the button should become disabled, and the bullet should still appear under **Selected → Bullets**.
6. **Search focus across panels.** Open another Core panel with a text field, focus the Filter Editor search, then hover over the other field. Search should keep focus. Click the other field and type. The receiving panel should handle the click and display the typed text.

## Adding tests

Add an automated test when explicit inputs can demonstrate a new rule or reproduce a bug. For behavior that depends on the game, document the setup, action, and expected result in the relevant in-game scenario.