using Terraria;

namespace SmartAmmoSwitch
{
    internal sealed class AmmoCandidate(int slotIndex, Item item, bool isInAmmoSlot)
    {
        public int SlotIndex { get; } = slotIndex;
        public Item Item { get; } = item;
        public int ItemType => Item.type;
        public int AmmoType => Item.ammo;
        public bool IsInAmmoSlot { get; } = isInAmmoSlot;
    }
}