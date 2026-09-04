using Terraria;

namespace SmartAmmoSwitch
{
    internal interface IAmmoFilter
    {
        bool Allows(Item ammo, Item weapon);
    }
}