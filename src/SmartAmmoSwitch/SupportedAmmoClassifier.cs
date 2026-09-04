using Terraria;
using Terraria.ID;

namespace SmartAmmoSwitch
{
    internal static class SupportedAmmoClassifier
    {
        public static SupportedAmmoCategory Classify(Item item)
        {
            if (item == null || item.IsAir || item.ammo <= 0)
                return SupportedAmmoCategory.Other;

            if (item.ammo == AmmoID.Arrow)
                return SupportedAmmoCategory.Arrows;

            if (item.ammo == AmmoID.Bullet)
                return SupportedAmmoCategory.Bullets;

            if (item.ammo == AmmoID.Rocket)
                return SupportedAmmoCategory.Rockets;

            if (item.ammo == AmmoID.Dart)
                return SupportedAmmoCategory.Darts;

            if (item.ammo == AmmoID.Coin)
                return SupportedAmmoCategory.Coins;

            if (item.ammo == AmmoID.Flare)
                return SupportedAmmoCategory.Flares;

            if (item.ammo == AmmoID.Sand)
                return SupportedAmmoCategory.Sand;

            if (item.ammo == AmmoID.Solution)
                return SupportedAmmoCategory.Solution;

            return SupportedAmmoCategory.Other;
        }
    }
}