using Terraria;

namespace SmartAmmoSwitch
{
    internal sealed class ItemTypeAmmoFilter : IAmmoFilter
    {
        public bool Allows(Item ammo, Item weapon)
        {
            if (ammo == null || ammo.IsAir)
                return false;

            return AmmoFilterSettings.IsItemAllowed(ammo.type);
        }
    }
}