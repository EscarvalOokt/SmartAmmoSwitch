using System.Collections.Generic;
using Terraria;

namespace SmartAmmoSwitch
{
    internal sealed class AmmoFilterPipeline
    {
        private readonly List<IAmmoFilter> _filters = new List<IAmmoFilter>();

        public void Add(IAmmoFilter filter)
        {
            if (filter != null)
                _filters.Add(filter);
        }

        public bool Allows(Item ammo, Item weapon)
        {
            for (var i = 0; i < _filters.Count; i++)
            {
                if (!_filters[i].Allows(ammo, weapon))
                    return false;
            }

            return true;
        }
    }
}