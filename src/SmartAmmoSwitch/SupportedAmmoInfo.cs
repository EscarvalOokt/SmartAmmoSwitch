namespace SmartAmmoSwitch
{
    internal sealed class SupportedAmmoInfo(int itemId, string itemName, int ammoType, SupportedAmmoCategory category)
    {
        public int ItemId { get; } = itemId;
        public string ItemName { get; } = itemName;
        public int AmmoType { get; } = ammoType;
        public SupportedAmmoCategory Category { get; } = category;
    }
}