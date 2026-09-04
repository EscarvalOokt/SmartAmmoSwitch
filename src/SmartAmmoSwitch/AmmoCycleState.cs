using System.Collections.Generic;

namespace SmartAmmoSwitch
{
    internal sealed class AmmoCycleState
    {
        public int CurrentIndex = -1;

        public int InventorySignature;

        public List<AmmoLayoutEntry> LastCandidateLayout = new List<AmmoLayoutEntry>();

        public List<int> OrderedTypes = new List<int>();
        public int UseAmmoType;
        public int WeaponType;
    }
}