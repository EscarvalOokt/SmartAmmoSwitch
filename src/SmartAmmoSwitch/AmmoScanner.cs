using System.Collections.Generic;
using Terraria;

namespace SmartAmmoSwitch
{
    internal sealed class AmmoScanner
    {
        public List<AmmoCandidate> Scan(Player player, AmmoSearchMode mode)
        {
            var result = new List<AmmoCandidate>();
            Item[] inv = player.inventory;

            ScanAmmoSlots(inv, result);

            if (mode == AmmoSearchMode.WholeInventory)
                ScanMainInventory(inv, result);

            return result;
        }

        private static void ScanAmmoSlots(Item[] inv, List<AmmoCandidate> result)
        {
            for (var i = 54; i <= 57; i++)
            {
                Item item = inv[i];
                if (!AmmoUtils.IsValidAmmo(item))
                    continue;

                result.Add(new AmmoCandidate(i, item, true));
            }
        }

        private static void ScanMainInventory(Item[] inv, List<AmmoCandidate> result)
        {
            for (var i = 0; i <= 53; i++)
            {
                Item item = inv[i];
                if (!AmmoUtils.IsValidAmmo(item))
                    continue;

                result.Add(new AmmoCandidate(i, item, false));
            }
        }
    }
}