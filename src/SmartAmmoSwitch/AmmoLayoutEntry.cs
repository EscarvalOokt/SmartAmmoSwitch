namespace SmartAmmoSwitch
{
    internal readonly struct AmmoLayoutEntry(int slotIndex, int itemType)
    {
        public int SlotIndex { get; } = slotIndex;
        public int ItemType { get; } = itemType;
    }
}