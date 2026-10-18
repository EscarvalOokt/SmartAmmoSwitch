using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace SmartAmmoSwitch
{
    internal static class AmmoUtils
    {
        public static bool IsValidAmmo(Item item)
        {
            return item is { IsAir: false, stack: > 0, ammo: > 0 };
        }

        public static int BuildInventorySignature(List<AmmoCandidate> candidates)
        {
            unchecked
            {
                var hash = 17;

                foreach (int itemType in candidates.Select(c => c.ItemType).Distinct().OrderBy(type => type))
                {
                    hash = hash * 31 + itemType;
                }

                return hash;
            }
        }

        public static List<AmmoLayoutEntry> BuildCandidateLayout(List<AmmoCandidate> candidates)
        {
            return candidates.OrderByDescending(c => c.IsInAmmoSlot)
                .ThenBy(c => c.SlotIndex)
                .Select(c => new AmmoLayoutEntry(c.SlotIndex, c.ItemType))
                .ToList();
        }
    }
}